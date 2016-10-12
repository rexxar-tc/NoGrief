using System.Net;
using System.Timers;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace NoGriefPlugin.NetworkHandlers
{
    public class ColorBlockHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;
        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "ColorBlockRequest" )
            {
                return false;
            }

            if ( _unitTestResult == null )
            {
                //make sure Keen hasn't changed the method somehow
                //private void ColorBlockRequest(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
                var parameters = site.MethodInfo.GetParameters();
                if ( parameters.Length != 4 )
                {
                    _unitTestResult = false;
                    return false;
                }

                if ( parameters[0].ParameterType != typeof(Vector3I)
                     || parameters[1].ParameterType != typeof(Vector3I)
                     || parameters[2].ParameterType != typeof(Vector3)
                     || parameters[3].ParameterType != typeof(bool) )
                {
                    _unitTestResult = false;
                    return false;
                }

                _unitTestResult = true;
            }

            return _unitTestResult.Value;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if (!PluginSettings.Instance.ProtectionZonesEnabled)
                return false;

            var grid = obj as MyCubeGrid;
            if (grid == null)
            {
               NoGrief.Log.Debug("Null grid in ColorBlockHandler");
                return false;
            }

            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (!item.Enabled || !item.StopPaint)
                    continue;

                if (!item.ContainsEntities.Contains(grid.EntityId))
                    continue;

                if (item.AdminExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                    continue;

                if (item.OwnerExempt && grid.BigOwners.Contains(PlayerMap.Instance.GetFastPlayerIdFromSteamId(remoteUserId)))
                    continue;

                NoGrief.Log.Info($"Intercepted block color request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for grid {grid.DisplayName??"ID"}:{grid.EntityId}");
                Communication.SendPrivateInformation(remoteUserId, "You cannot paint blocks in this protected area!");
                return true;
            }
            return false;
        }
    }
}
