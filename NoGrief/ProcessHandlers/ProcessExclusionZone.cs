using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NoGriefPlugin.Settings;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace NoGriefPlugin.ProcessHandlers
{
    public class ProcessExclusionZone : ProcessHandlerBase
    {


        public override int GetUpdateResolution()
        {
            return 200;
        }

        private readonly Dictionary<long, HashSet<long>> _redirectedEntites = new Dictionary<long, HashSet<long>>();


        public override void Handle()
        {
            if (!PluginSettings.Instance.ExclusionZonesEnabled)
                return;
            
            MyEntity[] entities = MyEntities.GetEntities().ToArray();
            if (entities.Length == 0)
            {
                NoGrief.Log.Info("Failed to get entity list in exclusion zone update. Skipping update.");
                return;
            }

            foreach (SettingsExclusionItem item in PluginSettings.Instance.ExclusionItems)
            {
                if (!item.Enabled)
                    continue;

                if (!_redirectedEntites.ContainsKey(item.EntityId))
                    _redirectedEntites.Add(item.EntityId, new HashSet<long>());

                MyEntity grid;
                if (!MyEntities.TryGetEntityById(item.EntityId, out grid))
                    continue;

                bool init = item.ContainsEntities.Count == 0;

                //zones don't work on moving entities
                if (!Vector3.IsZero(grid.Physics.LinearVelocity) || !Vector3.IsZero(grid.Physics.AngularVelocity))
                    continue;

                var sphere = new BoundingSphereD(grid.Center(), item.Radius);

                if (init)
                {
                    foreach (MyEntity entity in entities)
                    {
                        if (sphere.Contains(entity.PositionComp.WorldVolume) == ContainmentType.Contains)
                            item.ContainsEntities.Add(entity.EntityId);
                    }
                    if (item.TransportAdd)
                        item.AllowedEntities = item.ContainsEntities.ToList();
                }

                var outerSphere = new BoundingSphereD(sphere.Center, sphere.Radius + 50);

                var approaching = new HashSet<long>();
                foreach (MyEntity entity in entities)
                {
                    if (entity?.Physics == null)
                        continue;

                    if (entity.Closed || entity.MarkedForClose)
                        continue;

                    if (entity is MyVoxelBase)
                        continue;

                    if (entity is MyAmmoBase)
                        continue;

                    //entity is trying to enter the exclusion zone. push them away
                    if (outerSphere.Contains(entity.Center()) != ContainmentType.Disjoint && !item.ContainsEntities.Contains(entity.EntityId))
                    {
                        approaching.Add(entity.EntityId);
                            MyPlayer controller = MySession.Static.Players.GetControllingPlayer(entity);
                        try
                        {
                            if (controller?.Client != null)
                            {
                                if (!string.IsNullOrEmpty(item.FactionTag) && MySession.Static.Factions.GetPlayerFaction(controller.Identity.IdentityId).Tag == item.FactionTag)
                                {
                                    ExcludeEntities(entity, item);
                                    if (item.TransportAdd && !item.AllowedPlayers.Contains(controller.Client.SteamUserId))
                                        item.AllowedPlayers.Add(controller.Client.SteamUserId);
                                    continue;
                                }

                                if (item.AllowAdmins && controller.IsAdmin)
                                {
                                    ExcludeEntities(entity, item);
                                    continue;
                                }

                                if (item.AllowedPlayers.Contains(controller.Client.SteamUserId))
                                {
                                    ExcludeEntities(entity, item);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NoGrief.Log.Error(ex);
                        }

                        if (item.AllowedEntities.Contains(entity.EntityId))
                        {
                            ExcludeEntities(entity, item);
                            continue;
                        }

                        Vector3D direction = Vector3D.Normalize(entity.Center() - sphere.Center);
                        Vector3D velocity = entity.Physics.LinearVelocity;
                        if (Vector3D.IsZero(velocity))
                            velocity += direction;

                        if (!_redirectedEntites[item.EntityId].Contains(entity.EntityId))
                        {
                            _redirectedEntites[item.EntityId].Add(entity.EntityId);

                            Vector3D forceDir = Vector3D.Reflect(Vector3D.Normalize(velocity), direction);
                            //Vector3D force = forceDir * (velocity.Length()+10) * entity.Physics.Mass * 5 + -entity.Physics.LinearAcceleration * entity.Physics.Mass;
                            //if (!force.IsValid())
                            //{
                            //    NoGrief.Log.Error("Invalid Force");
                            //    continue;
                            //}
                            if (!(entity is MyCharacter))
                            {
                                if (controller?.Client != null && !string.IsNullOrEmpty(item.ExclusionMessage))
                                    Communication.Notification(controller.Client.SteamUserId, MyFontEnum.White, 10000, item.ExclusionMessage);
                                    
                                Wrapper.BeginGameAction(() =>
                                                        {
                                                            try
                                                            {
                                                                //entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, entity.Physics.CenterOfMassWorld, Vector3.Zero);
                                                                //AddForce didn't work well enough. This will give us a hard bounce
                                                                entity.Physics.SetSpeeds(velocity.Length() * forceDir, entity.Physics.AngularVelocity);
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                NoGrief.Log.Error(ex);
                                                            }
                                                        });
                            }
                            else
                            {
                                //characres require a different method
                                var character = (MyCharacter)entity;
                                Vector3D bodyDirection = -Vector3D.Normalize(Vector3D.Transform(direction, character.PositionComp.WorldMatrixInvScaled));
                                Vector3D bodyForce = bodyDirection * character.Physics.LinearVelocity.Length() * character.Physics.Mass;
                                Wrapper.BeginGameAction(() => character.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, bodyForce, character.Center(), Vector3.Zero));
                            }
                        }
                        if (sphere.Contains(entity.Center()) != ContainmentType.Disjoint)
                        {
                            if (!(entity is MyCharacter))
                            {
                                Vector3D force = direction * (velocity.Length() + 100) * entity.Physics.Mass * 5 * entity.Physics.LinearAcceleration.Length();
                                Wrapper.BeginGameAction(() => entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, entity.Physics.CenterOfMassWorld, Vector3.Zero));
                            }
                        }
                    }
                }

                var toRemove = new List<long>();
                //TODO: We're searching the dictionary way too much. Do something about it later
                foreach (long id in _redirectedEntites[item.EntityId])
                {
                    if (!approaching.Contains(id))
                        toRemove.Add(id);
                }
                foreach (long rem in toRemove)
                    _redirectedEntites[item.EntityId].Remove(rem);
            }
        }

        private void ExcludeEntities(MyEntity entity, SettingsExclusionItem item)
        {
            if (!(entity is MyCubeGrid))
            {
                item.ContainsEntities.Add(entity.EntityId);
                return;
            }
            var grid = (MyCubeGrid)entity;
            var nodes = MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Physical).GetGroupNodes(grid);
            foreach (var node in nodes)
            {
                item.ContainsEntities.Add(node.EntityId);
                if (item.TransportAdd)
                {
                    item.AllowedEntities.Add(entity.EntityId);
                    foreach (var id in node.GetPilotSteamIds())
                    {
                        if (!item.AllowedPlayers.Contains(id))
                            item.AllowedPlayers.Add(id);
                    }
                }
            }
        }
    }
}
