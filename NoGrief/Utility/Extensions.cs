using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;
using VRage.ModAPI;
using VRageMath;

namespace NoGriefPlugin.Utility
{
    public static class Extensions
    {
        #region "MyCubeGrid"

        public static List<ulong> GetPilotSteamIds( this MyCubeGrid grid )
        {
            List<ulong> steamIds = new List<ulong>( );
            foreach ( MySlimBlock block in grid.CubeBlocks )
            {
                var cockpit = block.FatBlock as MyCockpit;
                if ( cockpit == null )
                    continue;

                if ( cockpit.Pilot == null )
                    continue;

                MyCharacter pilot = cockpit.Pilot;

                steamIds.Add( pilot.ControllerInfo.Controller.Player.Client.SteamUserId );
            }
            return steamIds;
        }
        #endregion

        #region "IMyEntity"

        public static void Stop( this IMyEntity entity )
        {
            if ( entity == null || entity.Physics == null )
                return;

            Wrapper.GameAction( ( ) =>
             {
                 entity.Physics.LinearVelocity = Vector3D.Zero;
                 entity.Physics.AngularVelocity = Vector3D.Zero;
             } );
        }

        #endregion
    }
}
