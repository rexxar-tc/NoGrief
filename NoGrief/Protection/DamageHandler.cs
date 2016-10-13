using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game;
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
            IMyEntity ent;
            if (target is IMyEntity)
                ent = (IMyEntity)target;
            else if (target is IMySlimBlock)
                ent = ((IMySlimBlock)target).CubeGrid;
            else
            {
                NoGrief.Log.Error(target.GetType().FullName);
                throw new Exception();
            }
            IMyEntity attacker;
            if (!MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attacker))
                return;

            if (PluginSettings.Instance.ProtectionZonesEnabled)
            {
                foreach (var item in PluginSettings.Instance.ProtectionItems)
                {
                    if (!item.Enabled || !item.StopDamage)
                        continue;

                    if (!item.ContainsEntities.Contains(ent.EntityId))
                        continue;

                    if (ent is IMyCharacter && !item.StopPlayerDamage)
                        continue;

                    if (info.Type == MyStringHash.GetOrCompute("Grind") && !item.StopGrinding)
                        continue;

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