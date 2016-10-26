using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Server;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Network;

namespace NoGriefPlugin.NetworkHandlers
{
    public class BuildBlockHandler : NetworkHandlerBase
    {
        private static Dictionary<string, bool> _unitTestResults = new Dictionary<string, bool>();
        private const string BuildBlockName = "BuildBlockRequest";
        private const string BuildBlocksName = "BuildBlocksRequest";
        private const string BuildAreaName = "BuildBlocksAreaRequest";
        public override bool CanHandle( CallSite site )
        {
            //okay, there's three distinct methods that build blocks, we need to handle all of them
            if ( site.MethodInfo.Name == BuildBlockName )
            {
                if ( !_unitTestResults.ContainsKey( BuildBlockName ) )
                {
                    //public void BuildBlockRequest(uint colorMaskHsv, MyBlockLocation location, [DynamicObjectBuilder] MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId, bool instantBuild, long ownerId)
                    var parameters = site.MethodInfo.GetParameters();
                    if ( parameters.Length != 6 )
                    {
                        _unitTestResults[BuildBlockName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(uint)
                         || parameters[1].ParameterType != typeof(MyCubeGrid.MyBlockLocation)
                         || parameters[2].ParameterType != typeof(MyObjectBuilder_CubeBlock)
                         || parameters[3].ParameterType != typeof(long)
                         || parameters[4].ParameterType != typeof(bool)
                         || parameters[5].ParameterType != typeof(long) )
                    {
                        _unitTestResults[BuildBlockName] = false;
                        return false;
                    }

                    _unitTestResults[BuildBlockName] = true;
                }

                return _unitTestResults[BuildBlockName];
            }
            else if (site.MethodInfo.Name == BuildBlocksName)
            {
                if (!_unitTestResults.ContainsKey(BuildBlocksName))
                {
                    //void BuildBlocksRequest(uint colorMaskHsv, HashSet<MyBlockLocation> locations, long builderEntityId, bool instantBuild, long ownerId)
                    var parameters = site.MethodInfo.GetParameters();
                    if (parameters.Length != 5)
                    {
                        _unitTestResults[BuildBlocksName] = false;
                        return false;
                    }

                    if (parameters[0].ParameterType != typeof(uint)
                         || parameters[1].ParameterType != typeof(HashSet<MyCubeGrid.MyBlockLocation>)
                         || parameters[2].ParameterType != typeof(long)
                         || parameters[3].ParameterType != typeof(bool)
                         || parameters[4].ParameterType != typeof(long))
                    {
                        _unitTestResults[BuildBlocksName] = false;
                        return false;
                    }

                    _unitTestResults[BuildBlocksName] = true;
                }

                return _unitTestResults[BuildBlocksName];
            }
            else if (site.MethodInfo.Name == BuildAreaName)
            {
                if (!_unitTestResults.ContainsKey(BuildAreaName))
                {
                    //private void BuildBlocksAreaRequest(MyCubeGrid.MyBlockBuildArea area, long builderEntityId, bool instantBuild, long ownerId)
                    var parameters = site.MethodInfo.GetParameters();
                    if (parameters.Length != 4)
                    {
                        _unitTestResults[BuildAreaName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(MyCubeGrid.MyBlockBuildArea)
                         || parameters[1].ParameterType != typeof(long)
                         || parameters[2].ParameterType != typeof(bool)
                         || parameters[3].ParameterType != typeof(long))
                    {
                        _unitTestResults[BuildAreaName] = false;
                        return false;
                    }

                    _unitTestResults[BuildAreaName] = true;
                }

                return _unitTestResults[BuildAreaName];
            }
            return false;
        }

        Timer _kickTimer = new Timer(30000);
        public override bool Handle( ulong remoteUserId, CallSite site, BitStream stream, object obj )
        {
            if (!PluginSettings.Instance.ProtectionZonesEnabled)
                return false;

            var grid = obj as MyCubeGrid;
            if (grid == null)
            {
                NoGrief.Log.Debug("Null grid in BuildBlockHandler");
                return false;
            }

            bool found = false;
            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (!item.Enabled || !item.StopBuild)
                    continue;

                if (!item.ContainsEntities.Contains(grid.EntityId))
                    continue;

                if (item.AdminExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                    continue;

                if (item.OwnerExempt && grid.BigOwners.Contains(PlayerMap.Instance.GetFastPlayerIdFromSteamId(remoteUserId)))
                    continue;

                found = true;
                break;
            }

            if (!found)
                return false;

            NoGrief.Log.Info($"Intercepted block build request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for grid {grid.DisplayName ?? "ID"}:{grid.EntityId}");
            Communication.SendPrivateInformation(remoteUserId, "You cannot build blocks in this protected area!");

            //send the fail message to make the client play the paste fail sound
            //just because we can
            var inf = typeof(MyCubeBuilder).GetMethod("SpawnGridReply", BindingFlags.NonPublic | BindingFlags.Static);
            ServerNetworkManager.Instance.RaiseStaticEvent(inf, remoteUserId, false);

            return true;
        }
    }
}
