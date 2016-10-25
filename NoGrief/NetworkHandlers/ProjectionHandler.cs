using System.Reflection;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities.Blocks;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class ProjectionHandler : NetworkHandlerBase
    {
        private bool? _unitTestResult;
        public override bool CanHandle(CallSite site)
        {
            if (!site.MethodInfo.Name.Equals("OnNewBlueprintSuccess"))
                return false;

            if (_unitTestResult.HasValue)
                return _unitTestResult.Value;

            var parameters = site.MethodInfo.GetParameters();
            if (parameters.Length != 1)
            {
                _unitTestResult = false;
                return false;
            }
            if (parameters[0].ParameterType != typeof(MyObjectBuilder_CubeGrid))
            {
                _unitTestResult = false;
                return false;
            }

            _unitTestResult = true;
            return true;
        }

        public override bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj)
        {
            if (!PluginSettings.Instance.LimitProjectionSize)
                return false;

            MyProjectorBase proj = obj as MyProjectorBase;
            if (proj == null)
            {
                NoGrief.Log.Error("Null projector in ProjectionHandler");
                return false;
            }

            MyObjectBuilder_CubeGrid grid = null;

            base.Serialize(site.MethodInfo, stream, ref grid);

            if (grid.CubeBlocks.Count <= PluginSettings.Instance.ProjectionBlockCount)
                return false;

            if (PluginSettings.Instance.AdminProjectionExempt)
            {
                if (PlayerManager.Instance.IsUserAdmin(remoteUserId))
                    return false;

                ulong ownerSteam = PlayerMap.Instance.GetSteamIdFromPlayerId(proj.OwnerId);
                if (PlayerManager.Instance.IsUserAdmin(ownerSteam))
                    return false;
            }

            if(!string.IsNullOrEmpty(PluginSettings.Instance.ProjectionLimitMessage))
                Communication.Notification(remoteUserId, MyFontEnum.White, 10000, PluginSettings.Instance.ProjectionLimitMessage);

            NoGrief.Log.Info($"Intercepted projection change request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId}. Projection size: {grid.CubeBlocks.Count}");
            
            //this junk is to work around the fact that clints set the projected grid locally before sending the network event
            //so we tell them to remove the projection and replace it with what the server has
            if (proj.Clipboard.PreviewGrids.Count != 0)
            {
                //as much as I hate to do reflection here, it's necessary :(
                var projGrid = typeof(MyProjectorBase).GetField("m_originalGridBuilder", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(proj) as MyObjectBuilder_CubeGrid;
                //order of operations is important!
                //proj.SendRemoveProjection();
                //typeof(MyProjectorBase).GetMethod("SendNewBlueprint", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(proj, new object[] { projGrid });
                //don't waste bandwidth resending the projection to all clients, just send it back to the requesting client
                var remMethod = typeof(MyProjectorBase).GetMethod("OnRemoveProjectionRequest", BindingFlags.NonPublic | BindingFlags.Instance);
                ServerNetworkManager.Instance.RaiseEvent(remMethod, proj, remoteUserId);
                var newMethod = typeof(MyProjectorBase).GetMethod("OnNewBlueprintSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
                ServerNetworkManager.Instance.RaiseEvent(newMethod, proj, remoteUserId, projGrid);
            }
            else
            {
                //if there wasn't already a projection, just tell clients to clear
                proj.SendRemoveProjection();
            }

            return true;
        }
    }
}
