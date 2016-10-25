using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class SpawnPlanetHandler : NetworkHandlerBase
    {
        private bool? _unitTestResult = null;
        public override bool CanHandle(CallSite site)
        {
            //static void SpawnPlanet_Server(string planetName, float size, int seed, Vector3D pos)
            if (!site.MethodInfo.Name.Equals("SpawnPlanet_Server"))
                return false;

            if (_unitTestResult.HasValue)
                return _unitTestResult.Value;

            var parameters = site.MethodInfo.GetParameters();
            if (parameters.Length != 4)
            {
                _unitTestResult = false;
                return false;
            }

            if (parameters[0].ParameterType != typeof(string)
                || parameters[1].ParameterType != typeof(float)
                || parameters[2].ParameterType != typeof(int)
                || parameters[3].ParameterType != typeof(Vector3D))
            {
                _unitTestResult = false;
                return false;
            }

            _unitTestResult = true;
            return true;
        }

        private Timer _kickTimer = new Timer(10000);
        public override bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj)
        {
            if (!PluginSettings.Instance.StopAsteroidPaste)
                return false;

            if (PlayerManager.Instance.IsUserAdmin(remoteUserId))
                return false;

            if (PluginSettings.Instance.VoxelPasteSpaceMaster && PlayerManager.Instance.IsUserPromoted(remoteUserId))
                return false;

            Communication.Notification(remoteUserId, MyFontEnum.White, 5000, "Only administrators can paste planets!");
            NoGrief.Log.Info($"Intercepted planet paste request from user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId}");

            if (PluginSettings.Instance.VoxelPasteBan)
            {
                _kickTimer.Elapsed += (sender, args) =>
                                      {
                                          MyMultiplayer.Static.BanClient(remoteUserId, true);
                                          NoGrief.Log.Info($"Banned user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for pasting planets");
                                      };
                _kickTimer.AutoReset = false;
                _kickTimer.Start();
            }
            else if (PluginSettings.Instance.VoxelPasteKick)
            {
                _kickTimer.Elapsed += (sender, args) =>
                                      {
                                          MyMultiplayer.Static.KickClient(remoteUserId);
                                          NoGrief.Log.Info($"Kicked user {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for pasting planets");
                                      };
                _kickTimer.AutoReset = false;
                _kickTimer.Start();
            }

            return true;
        }
    }
}
