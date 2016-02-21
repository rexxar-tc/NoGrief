using System;
using System.Linq;
using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using VRage.Game.Entity;
using VRage.Game;
using NoGriefPlugin.CustomPacketHandlers;
using NoGriefPlugin.Utility;
using SEModAPIInternal.API.Common;

namespace NoGriefPlugin.CustomPacketHandlers
{
    public class SyncGridHandler : SyncHandlerBase
    {
        private static Type mySyncGridType = null;
        public SyncGridHandler()
        {
            mySyncGridType = SandboxGameAssemblyWrapper.Instance.GetAssemblyType("Sandbox.Game.Multiplayer","MySyncGrid");
            MethodInfo onConvertedToShipRequest = this.GetType().GetMethod("OnConvertedToShipRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            RegisterCustomPacketHandler(mySyncGridType, "ConvertToShipMsg", PacketRegistrationType.Instance, MyTransportMessageEnum.Request, onConvertedToShipRequest);

            MethodInfo onColorBlocksRequest = this.GetType().GetMethod("OnColorBlocksRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            RegisterCustomPacketHandler(mySyncGridType, "ColorBlocksMsg", PacketRegistrationType.Instance, MyTransportMessageEnum.Request, onColorBlocksRequest);

            MethodInfo onBuildBlocksRequest = this.GetType().GetMethod("OnBuildBlocksRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            RegisterCustomPacketHandler(mySyncGridType, "BuildBlocksMsg", PacketRegistrationType.Instance, MyTransportMessageEnum.Request, onBuildBlocksRequest);
        }

        protected static void OnBuildBlocksRequest<T>(Object instance, ref T packet, Object masterNetManager) where T : struct
        {
            MyNetworkClient client = (MyNetworkClient)masterNetManager;
            PropertyInfo entityProp = instance.GetType().GetProperties().Single(p => p.Name == "Entity" && p.PropertyType == typeof(MyCubeGrid));
            MyCubeGrid grid = (MyCubeGrid)entityProp.GetValue(instance);

            if (SafeZoneManager.IsEntityInSafeZone(grid))
            {
                Communication.DisplayDialog(client.SteamUserId, "Grid Building", "Building in a safe zone", "Building in a safe zone has been disabled");
            }
            else if(grid.PositionComp.WorldAABB.Size.Max() > 600)
            {
                Communication.DisplayDialog(client.SteamUserId, "Grid Building", "Invalid Ship Length", "Your ship/station is over 600m in one direction.  A ship can not be larger than 600m.  Please correct the length before building can continue.");
                grid.GetType().GetMethod("RecalcBounds", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(grid, new object[] { });
            }
            else
            {
                mySyncGridType.GetMethod("OnBuildBlocksRequest", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { instance, packet, masterNetManager });
            }
        }

        protected static void OnColorBlocksRequest<T>(Object instance, ref T packet, Object masterNetManager) where T : struct
        {
            MyNetworkClient client = (MyNetworkClient)masterNetManager;
            PropertyInfo entityProp = instance.GetType().GetProperties().Single(p => p.Name == "Entity" && p.PropertyType == typeof(MyCubeGrid));
            MyCubeGrid grid = (MyCubeGrid)entityProp.GetValue(instance);

            if (SafeZoneManager.IsEntityInSafeZone(grid))
            {
                Communication.DisplayDialog(client.SteamUserId, "Grid Painting", "Painting a Grid in a Safe Zone", "Painting grids in a safe zone is disabled");
            }
            else
            {
                mySyncGridType.GetMethod("OnColorBlocksRequest", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { instance, packet, masterNetManager});
            }
        }

        protected static void OnConvertedToShipRequest<T>(Object instance, ref T packet, Object masterNetManager) where T : struct
        {
            try
            {
                MyNetworkClient client = (MyNetworkClient)masterNetManager;
                PropertyInfo entityProp = instance.GetType().GetProperties().Single(p => p.Name == "Entity" && p.PropertyType == typeof(MyCubeGrid));
                MyCubeGrid grid = (MyCubeGrid)entityProp.GetValue(instance);

                FieldInfo info = packet.GetType().GetField("GridEntityId", BindingFlags.Public | BindingFlags.Instance);
                long entityId = (long)info.GetValue(packet);
                
                MyEntity entity;
                if (MyEntities.TryGetEntityById(entityId, out entity))
                {
                    Logging.WriteLineAndConsole(string.Format("Player '{0}' converting grid '{1}' to ship ({2})", client.DisplayName, grid.DisplayName, grid.GridSizeEnum.ToString()));

                    MyCubeGrid checkGrid = (MyCubeGrid)entity;

                    bool found = false;
                    foreach (long owner in checkGrid.BigOwners)
                    {
                        MyRelationsBetweenPlayerAndBlock relation = MyPlayer.GetRelationBetweenPlayers(owner, PlayerMap.Instance.GetFastPlayerIdFromSteamId(client.SteamUserId));
                        if(relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Owner)
                            found = true;
                    }

                    if(!found)
                    {
                        Communication.DisplayDialog(client.SteamUserId, "Grid Conversion", "Convert Station To Ship", "You can not convert this station into a grid due to the fact that you do not own enough of the ship to convert it.  You must hack or change the ownership of more blocks on the station to be able to convert it.");
                    }
                    else
                    {
                        MethodInfo oldOnConvertedToShipRequest = instance.GetType().GetMethod("OnConvertedToShipRequest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        oldOnConvertedToShipRequest.Invoke(instance, new object[] { instance, packet, masterNetManager });

                        Communication.DisplayDialog(client.SteamUserId, "Grid Conversion", "Convert Station To Ship ", "Station has been successfully converted to a ship.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLineAndConsole(string.Format("OnConvertedToShipRequest Problem: {0}", ex.ToString()));
            }
        }
    }
}