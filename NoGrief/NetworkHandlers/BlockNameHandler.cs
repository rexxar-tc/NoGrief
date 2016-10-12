using System.Timers;
using SEModAPIInternal.API.Server;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class BlockNameHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;
        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "SetCustomNameEvent")
            {
                if ( _unitTestResult==null )
                {
                    var parameters = site.MethodInfo.GetParameters();
                    if ( parameters.Length != 1 )
                    {
                        _unitTestResult = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(string) )
                    {
                        _unitTestResult = false;
                        return false;
                    }
                    _unitTestResult = true;
                }

                return _unitTestResult.Value;
            }

            return false;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            return true;
        }
    }
}
