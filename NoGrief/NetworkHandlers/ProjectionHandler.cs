using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class ProjectionHandler : NetworkHandlerBase
    {
        private bool? _unitTestResult = null;
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

            if (PluginSettings.Instance.AdminProjectionExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                return false;

            ulong ownerSteam = PlayerMap.Instance.GetSteamIdFromPlayerId(proj.OwnerId);
            if (PluginSettings.Instance.AdminProjectionExempt && PlayerManager.Instance.IsUserAdmin(ownerSteam))
                return false;

            if(!string.IsNullOrEmpty(PluginSettings.Instance.ProjectionLimitMessage))
                Communication.Notification(remoteUserId, MyFontEnum.White, 10000, PluginSettings.Instance.ProjectionLimitMessage);

            NoGrief.Log.Info($"Intercepted projection change request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId}. Projection size: {grid.CubeBlocks.Count}");

            proj.SendRemoveProjection();
            //this junk was to try and work around the fact that the client updates the projected grid locally before sending the network event
            //I tried to re-send the current grid but it didn't work
            //if (proj.Clipboard.PreviewGrids.Count != 0)
            //{
            //    //var offset = proj.ProjectionOffset;
            //    //var rot = proj.ProjectionRotation;
            //    //bool visible = proj.GetValueBool("ShowOnlyBuildable");
            //    //as much as I hate to do reflection here, it's necessary :(
            //    var projGrid = typeof(MyProjectorBase).GetField("m_originalGridBuilder", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(proj) as MyObjectBuilder_CubeGrid;
            //    typeof(MyProjectorBase).GetMethod("SendNewBlueprint", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(proj, new object[] {projGrid});
            //    //proj.SendNewOffset(offset, rot, visible);
            //}
            //else
            //{
            //    proj.SendRemoveProjection();
            //}

            return true;
        }
    }
}
