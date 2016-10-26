using System.Reflection;
using System.Timers;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace NoGriefPlugin.NetworkHandlers
{
    public class SpawnGridHandler : NetworkHandlerBase
    {
        private static bool? _unitTestResult;
        public override bool CanHandle( CallSite site )
        {
            //static void RequestGridSpawn(Author author, DefinitionIdBlit definition, BuildData position, bool instantBuild, bool forceStatic, uint colorMaskHsv)
            if (!site.MethodInfo.Name.Equals("RequestGridSpawn"))
                return false;
            
            if (_unitTestResult.HasValue)
                return _unitTestResult.Value;

            var parameters = site.MethodInfo.GetParameters();
            if (parameters.Length != 6)
            {
                _unitTestResult = false;
                return false;
            }

            if (parameters[0].ParameterType != typeof(MyCubeBuilder).GetNestedType("Author", BindingFlags.NonPublic)
                || parameters[1].ParameterType != typeof(DefinitionIdBlit)
                || parameters[2].ParameterType != typeof(MyCubeBuilder).GetNestedType("BuildData", BindingFlags.NonPublic)
                || parameters[3].ParameterType != typeof(bool)
                || parameters[4].ParameterType != typeof(bool)
                || parameters[5].ParameterType != typeof(uint))
            {
                NoGrief.Log.Error("UnitTestResult");
                _unitTestResult = false;
                return false;
            }

            _unitTestResult = true;
            return true;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if (!PluginSettings.Instance.ProtectionZonesEnabled)
                return false;
            
            Author author = new Author();
            DefinitionIdBlit blit = new DefinitionIdBlit();
            BuildData data = new BuildData();
            bool instant = false;
            bool forceStatic = false;
            uint hsf = 0;

            base.Serialize(site.MethodInfo, stream, ref author, ref blit, ref data, ref instant, ref forceStatic, ref hsf);

            bool found = false;
            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (!item.Enabled || !item.StopBuild)
                    continue;

                MyCubeGrid grid;
                if (!MyEntities.TryGetEntityById(item.EntityId, out grid))
                    continue;

                var sphere = new BoundingSphereD(grid.Center(), item.Radius);

                if (sphere.Contains(data.Position) == ContainmentType.Disjoint)
                    continue;

                if (item.AdminExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                    continue;
                
                found = true;
                break;
            }

            if (!found)
                return false;

            NoGrief.Log.Info($"Intercepted grid spawn request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)}");
            Communication.SendPrivateInformation(remoteUserId, "You cannot build blocks in this protected area!");

            //send the fail message to make the client play the paste fail sound
            //just because we can
            var inf = typeof(MyCubeBuilder).GetMethod("SpawnGridReply", BindingFlags.NonPublic | BindingFlags.Static);
            ServerNetworkManager.Instance.RaiseStaticEvent(inf, remoteUserId, false);

            return true;
        }

#pragma warning disable CS0649
        //copy/pasta'd because they're marked private in the game source
        //these aren't used outside of this class, but they must stay here exactly as is
       struct BuildData
        {
            public Vector3D Position;
            public Vector3 Forward;
            public Vector3 Up;
        }

        struct Author
        {
            public long EntityId;
            public long IdentityId;
            public Author(long entityId, long identityId)
            {
                EntityId = entityId;
                IdentityId = identityId;
            }
        }
#pragma warning restore CS0649
    }
}
