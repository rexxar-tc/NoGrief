using NoGriefPlugin.Utility;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class VoxelOperationHandler : NetworkHandlerBase
    {
        public override bool CanHandle(CallSite site)
        {
            return site.MethodInfo.Name.StartsWith("VoxelOperation");
        }

        public override bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj)
        {
            if (!PluginSettings.Instance.StopVoxelHands)
                return false;

            if (PlayerManager.Instance.IsUserAdmin(remoteUserId))
                return false;

            if (PluginSettings.Instance.VoxelPasteSpaceMaster && PlayerManager.Instance.IsUserPromoted(remoteUserId))
                return false;

            Communication.Notification(remoteUserId, MyFontEnum.White, 5000, "Only administrators can use voxel hands!");

            NoGrief.Log.Info($"Intercepted voxel hand operation from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId}");
            return true;
        }
    }
}
