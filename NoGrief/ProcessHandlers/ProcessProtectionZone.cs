using System.Collections.Generic;
using System.Linq;
using NoGriefPlugin.Protection;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRageMath;

namespace NoGriefPlugin.ProcessHandlers
{
    public class ProcessProtectionZone : ProcessHandlerBase
    {
        public override int GetUpdateResolution()
        {
            return 500;
        }

        public override void Handle()
        {
            if (!PluginSettings.Instance.ProtectionZonesEnabled)
                return;

            var entities = MyEntities.GetEntities().ToArray();
            if (entities.Length == 0)
            {
                NoGrief.Log.Info("Failed to get entities in protection zone update. Skipping update.");
                return;
            }
            
            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (!item.Enabled)
                    continue;

                MyEntity outEntity;
                if (!MyEntities.TryGetEntityById(item.EntityId, out outEntity))
                    continue;

                item.ContainsEntities.Clear();

                //zones don't work on moving entities
                if (!Vector3.IsZero(outEntity.Physics.LinearVelocity) || !Vector3.IsZero(outEntity.Physics.AngularVelocity))
                    continue;

                var sphere = new BoundingSphereD(outEntity.Center(), item.Radius);

                foreach (var entity in entities)
                {
                    if (sphere.Contains(entity.PositionComp.WorldVolume) == ContainmentType.Contains)
                        item.ContainsEntities.Add(entity.EntityId);
                }
            }
        }
    }
}
