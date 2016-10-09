using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace NoGriefPlugin.Protection
{
    public class DamageHandler
    {
        private static bool _init;
        private static DateTime _lastLog;

        public static HashSet<long> ProtectedId { get; set; } = new HashSet<long>();

        public static Dictionary<long, BoundingSphereD> BulletProtect { get; set; } = new Dictionary<long, BoundingSphereD>();

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
            try
            {
                //check plugin settings
                IMySlimBlock block = target as IMySlimBlock;
                if (block == null)
                    return;

                long gridId = block.CubeGrid.EntityId;
                IMyEntity attacker;
                if (!MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attacker))
                    return;

                if (ProtectedId.Contains(gridId))
                {
                    info.Amount = 0;
                    found = true;
                }

                else if (BulletProtect.ContainsKey(gridId) && info.Type == MyDamageType.Bullet)
                {
                    BoundingSphereD sphere;
                    if (!BulletProtect.TryGetValue(gridId, out sphere))
                    {
                        NoGrief.Log.Info("Failed to get bounding sphere on " + gridId);
                        return;
                    }
                    if (Vector3D.Distance(attacker.GetPosition(), sphere.Center) >= sphere.Radius)
                    {
                        info.Amount = 0;
                        found = true;
                    }
                }
                
                if (found && DateTime.Now - _lastLog > TimeSpan.FromSeconds(1))
                {
                    _lastLog = DateTime.Now;
                    NoGrief.Log.Info("Protected entity \"{0}\"", block.CubeGrid.DisplayName);
                }
            }
            catch (Exception ex)
            {
                NoGrief.Log.Error(ex, "fail damage handler");
            }
        }
    }
}