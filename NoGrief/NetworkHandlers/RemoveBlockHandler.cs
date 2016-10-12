using System.Collections.Generic;
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
    public class RemoveBlockHandler : NetworkHandlerBase
    {
        private static readonly Dictionary<string, bool> UnitTestResults = new Dictionary<string, bool>();
        private const string RazeBlocksName = "RazeBlocksRequest";
        private const string RazeAreaName = "RazeBlocksAreaRequest";

        public override bool CanHandle( CallSite site )
        {
            if ( site.MethodInfo.Name == RazeBlocksName )
            {
                bool result;
                if ( !UnitTestResults.TryGetValue( RazeBlocksName, out result ) )
                {
                    //public void RazeBlocks(List<Vector3I> locations)
                    var parameters = site.MethodInfo.GetParameters();
                    if ( parameters.Length != 1 )
                    {
                        UnitTestResults[RazeBlocksName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(List<Vector3I>) )
                    {
                        UnitTestResults[RazeBlocksName] = false;
                        return false;
                    }
                    
                    UnitTestResults[RazeBlocksName] = true;
                    return true;
                }

                return result;
            }
            else if ( site.MethodInfo.Name == RazeAreaName )
            {
                bool result;
                if ( !UnitTestResults.TryGetValue( RazeAreaName, out result ) )
                {
                    //void RazeBlocksAreaRequest(Vector3I pos, Vector3UByte size)
                    var parameters = site.MethodInfo.GetParameters();
                    if ( parameters.Length != 2 )
                    {
                        UnitTestResults[RazeAreaName] = false;
                        return false;
                    }

                    if ( parameters[0].ParameterType != typeof(Vector3I)
                         || parameters[1].ParameterType != typeof(Vector3UByte) )
                    {
                        UnitTestResults[RazeAreaName] = false;
                        return false;
                    }

                    UnitTestResults[RazeAreaName] = true;
                    return true;
                }

                return result;
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
                NoGrief.Log.Debug("Null grid in RemoveBlockHandler");
                return false;
            }

            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (!item.Enabled || !item.StopRemoveBlock)
                    continue;

                if (!item.ContainsEntities.Contains(grid.EntityId))
                    continue;

                if (item.AdminExempt && PlayerManager.Instance.IsUserAdmin(remoteUserId))
                    continue;

                if (item.OwnerExempt && grid.BigOwners.Contains(PlayerMap.Instance.GetFastPlayerIdFromSteamId(remoteUserId)))
                    continue;

                NoGrief.Log.Info($"Intercepted block remove request from {PlayerMap.Instance.GetFastPlayerNameFromSteamId(remoteUserId)} for grid {grid.DisplayName ?? "ID"}:{grid.EntityId}");
                Communication.SendPrivateInformation(remoteUserId, "You cannot remove blocks in this protected area!");
                return true;
            }
            return false;
        }
    }
}
