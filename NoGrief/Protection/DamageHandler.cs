using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace NoGriefPlugin.Protection
{
    public class DamageHandler
    {
        private static bool _init;
        private static DateTime _lastLog;
        
        public static void Init()
        {
            if (_init)
                return;

            _init = true;

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, ProcessDamage);
        }

        public static void ProcessDamage(object target, ref MyDamageInformation info)
        {
            bool found = false;
            MyEntity ent;
            if (target is MyEntity)
                ent = (MyEntity)target;
            else if (target is MySlimBlock)
                ent = ((MySlimBlock)target).CubeGrid;
            else
            {
                NoGrief.Log.Error(target.GetType().FullName);
                throw new Exception();
            }
            MyEntity attacker;
            if (!MyEntities.TryGetEntityById(info.AttackerId, out attacker))
                return;

            if (PluginSettings.Instance.ProtectionZonesEnabled)
            {
                foreach (var item in PluginSettings.Instance.ProtectionItems)
                {
                    if (!item.Enabled || !item.StopDamage)
                        continue;

                    if (!item.ContainsEntities.Contains(ent.EntityId))
                        continue;

                    if (ent is MyCharacter && !item.StopPlayerDamage)
                        continue;
                    
                    if (info.Type == MyDamageType.Grind && item.StopGrinding)
                    {
                        //why the hell is Owner protected
                        var ownerField = typeof(MyEngineerToolBase).GetField("Owner", BindingFlags.NonPublic | BindingFlags.Instance);
                        var character = ownerField?.GetValue(attacker) as MyCharacter;
                        if (character?.ControllerInfo?.Controller?.Player == null)
                        {
                            continue;
                        }

                        if (item.AdminExempt && character.ControllerInfo.Controller.Player.IsAdmin)
                            return;

                        var grid = ent as MyCubeGrid;
                        if (grid == null)
                            continue;

                        if (item.OwnerExempt && grid.BigOwners.Contains(character.ControllerInfo.Controller.Player.Identity.IdentityId))
                            return;
                    }

                    info.Amount = 0;
                    found = true;
                    break;
                }
            }
            if (found && DateTime.Now - _lastLog > TimeSpan.FromSeconds(1))
            {
                _lastLog = DateTime.Now;
                NoGrief.Log.Info($"Protected entity \"{ent.GetTopMostParent().DisplayName ?? ent.GetTopMostParent().EntityId.ToString()}\"");
            }
        }
    }
}