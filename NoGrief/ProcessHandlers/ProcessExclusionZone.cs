using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Generics;
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

        private Type myProjectilesType = null;
        private int updateCount = 0;

        public override void Handle()
        {
            if (!PluginSettings.Instance.ExclusionZonesEnabled)
                return;

            if (myProjectilesType == null)
            {
                myProjectilesType = Utilities.FindTypeInAllAssemblies("Sandbox.Game.Weapons.MyProjectiles");
                if (myProjectilesType == null)
                    throw new TypeAccessException("Can't find type for MyProjectiles!");
            }

            //this mess of reflection gets the internal pool of projectiles so we can stop them later
            var projPool = myProjectilesType.GetField("m_projectiles", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            if(projPool==null)
                throw new FieldAccessException("Can't get m_projectile");
            //this field is a HashSetReader, we need this instead of the MyObjectsPool
            var projActive = projPool.GetType().GetProperty("Active", BindingFlags.Instance | BindingFlags.Public);
            if(projActive == null)
                throw new FieldAccessException("Can't get Active");
            //the type is MyHashSetReader<MyProjectile>, but MyProjectile is private
            //cast to generic IEnumberable so we can get around this
            var projectiles = projActive.GetValue(projPool) as IEnumerable;

            if (projectiles == null)
                throw new FieldAccessException("Can't get value for m_projectiles!");

            HashSet<MyEntity> entities = MyEntities.GetEntities();
            
            foreach (var item in PluginSettings.Instance.ExclusionItems)
            {
                if (!item.Enabled)
                    continue;

                MyEntity grid;
                if (!MyEntities.TryGetEntityById(item.EntityId, out grid))
                    continue;

                bool init = item.ContainsEntities.Count == 0;

                //zones don't work on moving entities
                if (!Vector3.IsZero(grid.Physics.LinearVelocity) || !Vector3.IsZero(grid.Physics.AngularVelocity))
                    continue;

                var sphere = new BoundingSphereD(grid.Center(), item.ExclusionRadius);

                if (init)
                {
                    foreach (var entity in entities)
                    {
                        if(sphere.Contains(entity.PositionComp.WorldVolume) == ContainmentType.Contains)
                            item.ContainsEntities.Add(entity.EntityId);
                    }
                    if (item.TransportAdd)
                        item.AllowedEntities = item.ContainsEntities.ToList();
                }

                var outerSphere = new BoundingSphereD(sphere.Center, sphere.Radius + 50);

                foreach (var entity in entities)
                {
                    if (entity?.Physics == null)
                        continue;

                    if (entity.Closed || entity.MarkedForClose)
                        continue;

                    if (entity is MyVoxelBase)
                        continue;

                    
                    //entity is trying to enter the exclusion zone. push them away
                    if (outerSphere.Contains(entity.Center()) != ContainmentType.Disjoint && !item.ContainsEntities.Contains(entity.EntityId))
                    {
                        if (entity is MyAmmoBase)
                        {
                            ((IMyDestroyableObject)entity).DoDamage(100f, MyStringHash.GetOrCompute("Explosion"), true);
                            continue;
                        }
                        var direction = Vector3D.Normalize(sphere.Center - entity.Center());
                        Vector3D velocity = entity.Physics.LinearVelocity;
                        if (Vector3D.IsZero(velocity))
                            velocity += direction;
                        Vector3D forceDir = Vector3D.Reflect(Vector3D.Normalize(velocity), direction);
                        Vector3D force = forceDir * velocity.Length() * entity.Physics.Mass * 5 + -entity.Physics.LinearAcceleration * entity.Physics.Mass;
                        if (!force.IsValid())
                        {
                            NoGrief.Log.Error("Invalid Force");
                            continue;
                        }
                        if (!(entity is MyCharacter))
                        {
                            Wrapper.BeginGameAction(() =>
                                                    {
                                                        try
                                                        {
                                                            entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, entity.Physics.CenterOfMassWorld, Vector3.Zero);
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
                            //var character = (MyCharacter)entity;
                            //Vector3D bodyDirection = Vector3D.Transform(direction, character.PositionComp.WorldMatrixInvScaled);
                            //Vector3D bodyForce = bodyDirection * character.Physics.LinearVelocity.Length() * character.Physics.Mass;//Vector3D.Transform(worldForce, character.PositionComp.WorldMatrixNormalizedInv);
                            //Wrapper.BeginGameAction(() => character.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, bodyForce, character.Center(), Vector3.Zero));
                        }
                    }
                }

                //delete any bullets that are entering the exclusion zone
                foreach (object obj in projectiles)
                {
                    Vector3D pos = (Vector3D)obj.GetType().GetField("m_position",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
                    if (outerSphere.Contains(pos) != ContainmentType.Disjoint && sphere.Contains(pos) == ContainmentType.Disjoint)
                    {
                        //obj.GetType().GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(obj, null);
                        var state = obj.GetType().GetField("m_state",BindingFlags.NonPublic|BindingFlags.Instance);
                        
                        if(state==null)
                            throw new FieldAccessException("m_state");

                        //set state to KILLED_AND_DRAWN to trick the game into removing the bullet on the next updat
                        //this doesn't sync :(
                        state.SetValue(obj, (byte)2);
                    }
                }
            }
        }
    }
}
