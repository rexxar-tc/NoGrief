using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using NoGriefPlugin.Utility;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class ActionLogger : NetworkHandlerBase
    {
        private ConcurrentQueue<string> _logBuffer = new ConcurrentQueue<string>();
        private Timer _logTimer = new Timer(10);

        public ActionLogger()
        {
            _logTimer.Elapsed += (sender, args) => ProcessLog();
            _logTimer.Start();
        }

        public override bool CanHandle(CallSite site)
        {
            //we want everything except these two message types because they're sent many times per second and we don't want to decode them
            return !site.MethodInfo.Name.Contains("SyncPropertyChanged") && !site.MethodInfo.Name.Contains("OnSimulationInfo");
        }

        public override bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj)
        {
            var ent = obj as MyEntity;
            if(ent==null) 
                _logBuffer.Enqueue($"Player {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} sent event {site.MethodInfo.Name}");
            else
                _logBuffer.Enqueue($"Player {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}:{remoteUserId} sent event {site.MethodInfo.Name} near {ent.Center()}");

            return false;
        }

        private void ProcessLog()
        {
            string line;
            while (_logBuffer.TryDequeue(out line))
            {
                NoGrief.Log.Error(line);
            }
        }
    }
}
