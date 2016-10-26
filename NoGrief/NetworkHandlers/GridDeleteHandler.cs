using System.Reflection;
using System.Timers;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class GridDeleteHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;
        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name != "OnEntityClosedRequest" )
                return false;

            if (_unitTestResult != null)
                return _unitTestResult.Value;

            //static void OnEntityClosedRequest(long entityId)
            var parameters = site.MethodInfo.GetParameters();
            if ( parameters.Length != 1 )
            {
                _unitTestResult = false;
                return false;
            }

            if ( parameters[0].ParameterType != typeof(long) )
            {
                _unitTestResult = false;
                return false;
            }

            _unitTestResult = true;

            return _unitTestResult.Value;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if (!PluginSettings.Instance.ProtectionZonesEnabled)
                return false;

            long entityId = 0;

            base.Serialize(site.MethodInfo, stream, ref entityId);

            MyCubeGrid grid;

            if (!MyEntities.TryGetEntityById(entityId, out grid))
                return false;

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

                NoGrief.Log.Info($"Intercepted grid delete request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for grid {grid.DisplayName ?? "ID"}:{grid.EntityId}");
                Communication.SendPrivateInformation(remoteUserId, "You cannot delete grids in this protected area!");

                //send the fail message to make the client play the paste fail sound
                //just because we can
                var inf = typeof(MyCubeBuilder).GetMethod("SpawnGridReply", BindingFlags.NonPublic | BindingFlags.Static);
                ServerNetworkManager.Instance.RaiseStaticEvent(inf, remoteUserId, false);

                return true;
            }

            return false;
        }
    }
}
