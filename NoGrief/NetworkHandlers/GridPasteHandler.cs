using System.Collections.Generic;
using System.Timers;
using NoGriefPlugin.Utility;
using Sandbox.Engine.Multiplayer;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace NoGriefPlugin.NetworkHandlers
{
    public class GridPasteHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;
        public override bool CanHandle( CallSite site )
        {
            //public static void TryPasteGrid_Implementation(List<MyObjectBuilder_CubeGrid> entities, bool detectDisconnects, 
            //                                              long inventoryEntityId, Vector3 objectVelocity, bool multiBlock, bool instantBuild)
            if (site.MethodInfo.Name != "TryPasteGrid_Implementation")
                return false;

            if (_unitTestResult != null)
                return _unitTestResult.Value;

            var parameters = site.MethodInfo.GetParameters();
            if ( parameters.Length != 6 )
            {
                _unitTestResult = false;
                foreach(var param in parameters)
                    NoGrief.Log.Error(param.ParameterType.ToString);
                return false;
            }

            if (parameters[0].ParameterType != typeof(List<MyObjectBuilder_CubeGrid>)
                || parameters[1].ParameterType != typeof(bool)
                || parameters[2].ParameterType != typeof(long)
                || parameters[3].ParameterType != typeof(Vector3)
                || parameters[4].ParameterType != typeof(bool)
                || parameters[5].ParameterType != typeof(bool))
            {
                _unitTestResult = false;
                return false;
            }
            
            _unitTestResult = true;

            return true;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if (!PluginSettings.Instance.LimitPasteSize)
                return false;

            if (PluginSettings.Instance.AdminPasteExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                return false;

            if (PluginSettings.Instance.SpaceMasterPasteExempt && PlayerManager.Instance.IsUserPromoted(remoteUserId))
                return false;

            var gridsList = new List<MyObjectBuilder_CubeGrid>();
            bool detectDisconects = false;
            long inventoryEntityId = 0;
            Vector3 objectVelocity = Vector3.Zero;
            bool multiBlock = false;
            bool instantBuild = false;

            base.Serialize(site.MethodInfo,stream,ref gridsList, ref detectDisconects, ref inventoryEntityId, ref objectVelocity, ref multiBlock, ref instantBuild);

            int blockCount = 0;
            foreach (var gridOb in gridsList)
            {
                blockCount += gridOb.CubeBlocks.Count;
                break;
            }

            if (blockCount < PluginSettings.Instance.PasteBlockCount)
                return false;

            NoGrief.Log.Info($"Intercepted grid paste request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for {blockCount} blocks.");

            if(!string.IsNullOrEmpty(PluginSettings.Instance.PasteLimitMessagePrivate))
                Communication.Notification(remoteUserId, MyFontEnum.Red, 10000, PluginSettings.Instance.PasteLimitMessagePrivate);

            if(!string.IsNullOrEmpty(PluginSettings.Instance.PasteLimitMessagePublic))
                Communication.SendPublicInformation(PluginSettings.Instance.PasteLimitMessagePublic.Replace("%player%", PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)).Replace("%count%", blockCount.ToString()));

            if (PluginSettings.Instance.PasteLimitBan)
            {
                _kickTimer.Elapsed += (sender, args) =>
                                      {
                                          Wrapper.BeginGameAction(() => MyMultiplayer.Static.BanClient(remoteUserId, true));
                                          NoGrief.Log.Info($"Auto-banned player {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for violating paste limits");
                                      };
                _kickTimer.AutoReset = false;
                _kickTimer.Start();
            }
            else if (PluginSettings.Instance.PasteLimitKick)
            {
                _kickTimer.Elapsed += (sender, args) =>
                                      {
                                          Wrapper.BeginGameAction(() => MyMultiplayer.Static.KickClient(remoteUserId));
                                          NoGrief.Log.Info($"Auto-kicked player {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} for violating paste limits");
                                      };
                _kickTimer.AutoReset=false;
                _kickTimer.Start();
            }

            return true;
        }
    }
}
