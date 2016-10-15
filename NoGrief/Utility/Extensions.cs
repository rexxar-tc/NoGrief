using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;
using VRageMath;

namespace NoGriefPlugin.Utility
{
    public static class Extensions
    {
        #region MyCubeGrid

        public static List<ulong> GetPilotSteamIds(this MyCubeGrid grid)
        {
            var steamIds = new List<ulong>();
            foreach (MySlimBlock block in grid.CubeBlocks)
            {
                var cockpit = block.FatBlock as MyCockpit;

                if (cockpit?.Pilot?.ControllerInfo?.Controller?.Player == null)
                    continue;
                
                steamIds.Add(cockpit.Pilot.ControllerInfo.Controller.Player.Client.SteamUserId);
            }
            return steamIds;
        }

        #endregion
    
        #region MyEntity

        public static void Stop(this MyEntity entity)
        {
            if (entity?.Physics == null)
                return;

            Wrapper.BeginGameAction(() =>
                               {
                                   entity.Physics.SetSpeeds(Vector3.Zero, Vector3.Zero);
                               });
        }

        public static Vector3D Center(this MyEntity entity)
        {
            return entity.PositionComp.WorldAABB.Center;
        }
        #endregion
    }
}