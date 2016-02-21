using System.Linq;
using System.Threading;

namespace NoGriefPlugin.ProcessHandlers
{
    using NoGriefPlugin.Settings;
    using NoGriefPlugin;
    using VRageMath;
    using VRage.ModAPI;
    using Sandbox.ModAPI;
    using System.Collections.Generic;
    using Sandbox.Game.Entities;
    using SEModAPIInternal.API.Common;
    using NoGriefPlugin.Utility;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common;
    using System;
    using VRage.ObjectBuilders;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Replication;
    using Sandbox.Game.World;
    using Sandbox.Game.Multiplayer;
    using Sandbox.Game.Entities.Character;
    using Sandbox.Definitions;
    using Sandbox.Game.Gui;
    using VRage.Game.Entity;
    public class ProcessProtectionZone : ProcessHandlerBase
    {

        public override int GetUpdateResolution( )
        {
            return 500;
        }

        public override void Handle( )
        {
            if ( PluginSettings.Instance.ProtectionZoneEnabled )
            {
                //create a local list of entites to protect rather than modify the real one
                HashSet<long> protectedId = new HashSet<long>( );

                foreach ( ProtectionItem item in PluginSettings.Instance.ProtectionZoneItems )
                {
                    if ( !item.Enabled )
                        continue;

                    IMyEntity itemEntity;
                    
                    if ( !MyAPIGateway.Entities.TryGetEntityById( item.EntityId, out itemEntity ) )
                    {
                        if ( PluginSettings.Instance.ExclusionLogging )
                            NoGrief.Log.Info( "Error processing protection zone on entity {0}, could not get entity.", item.EntityId );
                        item.Enabled = false;
                        continue;
                    }
                    if ( itemEntity.Physics.LinearVelocity != Vector3D.Zero || itemEntity.Physics.AngularVelocity != Vector3D.Zero )
                    {
                        if ( PluginSettings.Instance.ExclusionLogging )
                            NoGrief.Log.Debug( "Not processing protection zone on entity {0} -> {1} because it is moving.", item.EntityId, itemEntity.DisplayName );
                        continue;
                    }
                    //create the actual protection zone
                    BoundingSphereD protectSphere = new BoundingSphereD( itemEntity.GetPosition( ), item.ProtectionRadius );
                    List<MyEntity> protectEntities = MyEntities.GetTopMostEntitiesInSphere( ref protectSphere );

                    int gridCount = 0;
                    foreach ( MyEntity entity in protectEntities )
                    {
                        if ( entity is MyCubeGrid )
                        {
                            ++gridCount;
                            MyCubeGrid grid = (MyCubeGrid)entity;
                            List<IMySlimBlock> blocks = new List<IMySlimBlock>( );
                            if ( item.MaxGridSize != 0 && grid.BlocksCount > item.MaxGridSize )
                            {
                                List<ulong> steamIds = grid.GetPilotSteamIds();
                                if (steamIds.Any())
                                {
                                    foreach (ulong steamId in steamIds)
                                    {
                                        Communication.Notification(steamId, MyFontEnum.DarkBlue, 5, 
                                            $"This grid has exceeded the max block count of {item.MaxGridSize} and is not protected.");
                                    }
                                }
                                continue;
                            }
                            if ( item.MaxGridCount != 0 && gridCount > item.MaxGridCount )
                            {
                                List<ulong> steamIds = grid.GetPilotSteamIds( );
                                if ( steamIds.Any( ) )
                                {
                                    foreach ( ulong steamId in steamIds )
                                    {
                                        Communication.Notification( steamId, MyFontEnum.DarkBlue, 5, 
                                            "Protected grid count exceeded. This grid is not protected." );
                                    }
                                }
                                continue;
                            }
                            protectedId.Add( entity.EntityId );
                        }
                    }
                    protectEntities.Clear( );
                }
                //update the real list of protected entities only after we're done processing them
                //it's easier to clear it like this than track when entities leave the protection zone
                DamageHandler.ProtectedId.Clear( );
                
                DamageHandler.ProtectedId = protectedId;
                base.Handle( );
            }
        }
    }
}
