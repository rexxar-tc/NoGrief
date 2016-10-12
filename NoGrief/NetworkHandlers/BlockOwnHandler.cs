using System.Collections.Generic;
using System.Timers;
using Sandbox.Game.Entities;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class BlockOwnHandler : NetworkHandlerBase
    {
        private static Dictionary<string, bool> _unitTestResults = new Dictionary<string, bool>();
        private const string ChangeOwnerName = "OnChangeOwnerRequest";
        private const string ChangeOwnersName = "OnChangeOwnersRequest";

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name == ChangeOwnerName )
            {
                if ( !_unitTestResults.ContainsKey( ChangeOwnerName ) )
                {
                    //void OnChangeOwnerRequest(long blockId, long owner, MyOwnershipShareModeEnum shareMode)
                    var parameters = site.MethodInfo.GetParameters();
                    if ( parameters.Length != 3 )
                    {
                        _unitTestResults[ChangeOwnerName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(long)
                         || parameters[1].ParameterType != typeof(long)
                         || parameters[2].ParameterType != typeof(MyOwnershipShareModeEnum) )
                    {

                        _unitTestResults[ChangeOwnerName] = false;
                        return false;
                    }
                    _unitTestResults[ChangeOwnerName] = true;
                }

                return _unitTestResults[ChangeOwnerName];
            }
            else if ( site.MethodInfo.Name == ChangeOwnersName )
            {
                if ( !_unitTestResults.ContainsKey( ChangeOwnersName ) )
                {
                    //private static void OnChangeOwnersRequest(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests, long requestingPlayer)   
                    var parameters = site.MethodInfo.GetParameters();
                    if ( parameters.Length != 3 )
                    {
                        _unitTestResults[ChangeOwnersName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(MyOwnershipShareModeEnum)
                         || parameters[1].ParameterType != typeof(List<MyCubeGrid.MySingleOwnershipRequest>)
                         || parameters[2].ParameterType != typeof(long) )
                    {
                        _unitTestResults[ChangeOwnersName] = false;
                        return false;
                    }
                    _unitTestResults[ChangeOwnersName] = true;
                }
                return _unitTestResults[ChangeOwnersName];
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
