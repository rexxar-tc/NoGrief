using System.Timers;
using SEModAPIInternal.API.Server;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class ConvertPhyiscsHandler : NetworkHandlerBase
    {
        private const string ConvertShipName = "OnConvertedToShipRequest";
        private const string ConvertStationName = "OnConvertedToStationRequest";
        public override bool CanHandle( CallSite site )
        {
            return site.MethodInfo.Name == ConvertShipName || site.MethodInfo.Name == ConvertStationName;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            return true;
        }
    }
}
