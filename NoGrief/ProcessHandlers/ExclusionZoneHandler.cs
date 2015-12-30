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
    using System;
    using VRage.ObjectBuilders;
    using Sandbox.Engine.Multiplayer;
    using Sandbox.Game.Replication;
    public class ExclusionZoneHandler : ProcessHandlerBase
    {
        private static bool _init = true;

        public override int GetUpdateResolution( )
        {
            return 1000;
        }

        public override void Handle( )
        {
            if ( PluginSettings.Instance.ExclusionEnabled )
            {

                if ( _init )
                    Init( );

                foreach ( ExclusionItem item in PluginSettings.Instance.ExclusionItems )
                {
                    if ( item.Enabled )
                    {
                        IMyEntity itemEntity;
                        if ( !MyAPIGateway.Entities.TryGetEntityById( item.EntityId, out itemEntity ) )
                        {
                            NoGrief.Log.Info( "Error processing protection zone on entity {0}, could not get entity.", item.EntityId );
                            item.Enabled = false;
                            continue;
                        }
                        if ( itemEntity.Physics.LinearVelocity != Vector3D.Zero || itemEntity.Physics.AngularVelocity != Vector3D.Zero )
                        {
                            NoGrief.Log.Debug( "Not processing protection zone on entity {0} -> {1} because it is moving.", item.EntityId, itemEntity.DisplayName );
                            continue;
                        }

                        BoundingSphereD protectSphere = new BoundingSphereD( itemEntity.GetPosition( ), item.ExclusionRadius );
                        List<IMyEntity> protectEntities = MyAPIGateway.Entities.GetEntitiesInSphere( ref protectSphere );

                        foreach ( IMyEntity entity in protectEntities )
                        {
                            if ( entity == null )
                                continue;
                            
                            else if ( entity is IMyCubeGrid )
                            {
                                IMyCubeGrid grid = (IMyCubeGrid)entity;
                                MyObjectBuilder_CubeGrid gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
                                List<ulong> playersOnboard = new List<ulong>( );

                                if ( item.AllowedEntities.Contains( entity.EntityId ) && !item.TransportAdd )
                                    continue;

                                foreach ( MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks )
                                {
                                    if ( block.TypeId == typeof( MyObjectBuilder_Cockpit ) )
                                    {
                                        MyObjectBuilder_Cockpit cockpit = (MyObjectBuilder_Cockpit)block;
                                        if ( cockpit.Pilot == null )
                                            continue;

                                        IMyPlayer pilot = (IMyPlayer)cockpit.Pilot;
                                        if ( !playersOnboard.Contains( pilot.SteamUserId ) )
                                            playersOnboard.Add( pilot.SteamUserId );
                                    }

                                    if ( block.TypeId == typeof( MyObjectBuilder_RemoteControl ) )
                                    {
                                        MyObjectBuilder_RemoteControl cockpit = (MyObjectBuilder_RemoteControl)block;
                                        if ( cockpit.PreviousControlledEntityId == null )
                                            continue;

                                        //we should check to see if the remote control block is currently being used
                                        //this ought to be good enough for now, though
                                        ulong steamId = PlayerMap.Instance.GetSteamIdFromPlayerId( (long)cockpit.PreviousControlledEntityId );
                                        if ( !playersOnboard.Contains( steamId ) )
                                            playersOnboard.Add( steamId );
                                    }
                                }
                                if ( item.AllowedEntities.Contains( entity.EntityId ) && item.TransportAdd )
                                {
                                    foreach ( ulong steamId in playersOnboard )
                                    {
                                        if ( !item.AllowedPlayers.Contains( steamId ) )
                                            item.AllowedPlayers.Add( steamId );
                                    }
                                    continue;
                                }
                                //there's probably a faster way to do this, but the number of items is usually pretty low, so whatever
                                bool found = false;
                                foreach ( ulong steamId in playersOnboard )
                                {
                                    if ( item.AllowedPlayers.Contains( steamId ) )
                                    {
                                        found = true;
                                        break;
                                    }
                                    if ( item.AllowAdmins && PlayerManager.Instance.IsUserAdmin( steamId ) )
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if ( found && item.TransportAdd )
                                {
                                    foreach ( ulong steamId in playersOnboard )
                                    {
                                        if ( !item.AllowedPlayers.Contains( steamId ) )
                                            item.AllowedPlayers.Add( steamId );
                                    }
                                    continue;
                                }
                                else if ( found )
                                    continue;
                                else
                                {
                                    //oh hey look we FINALLY have a ship we need to stop.
                                    //...
                                    ///TODO: Warn user and move ship away from protected zone
                                }
                            }

                            else if ( entity is IMyFloatingObject )
                            {
                                //floating object, has someone hurled a huge rock at us?
                                if ( entity.Physics.Speed > 10f )
                                {
                                    //if the thing is moving faster than 10m/s, assume it's a weapon and delete it
                                    entity.Close( );
                                    MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( entity ) );
                                    //this replication shouldn't be necessary, but let's force a sync just to be safe.
                                }
                            }

                            else if ( entity is IMyPlayer )
                            {
                                IMyPlayer player = (IMyPlayer)entity;
                                if ( item.AllowedPlayers.Contains( player.SteamUserId ) )
                                    continue;
                                else if ( item.AllowAdmins && PlayerManager.Instance.IsUserAdmin( player.SteamUserId ) )
                                    continue;
                                else
                                {
                                    ///TODO: Warn user

                                    Vector3D? tmpVect = MathUtility.TraceVector( entity.GetPosition( ), entity.Physics.LinearVelocity, -100 );
                                    Vector3D moveTo;
                                    if ( tmpVect != null )
                                        moveTo = (Vector3D)tmpVect;
                                    else
                                    {
                                        //couldn't find anywhere to put the player.
                                        //do something about it?
                                        continue;
                                    }

                                    ///TODO: Move user away from protection zone
                                }
                            }
                            
                            else
                            {
                                //there's a thing, but we don't know what it is
                                //what we don't know can't hurt us, right?
                                NoGrief.Log.Debug( "Found entity of type {0} in protection zone on {1}", entity.ToString( ), item.EntityId );
                            }
                        }

                        protectEntities.Clear( );
                    }
                }
                base.Handle( );
            }
        }

        private void Init( )
        {
            _init = false;
            foreach ( ExclusionItem item in PluginSettings.Instance.ExclusionItems )
            {
                InitItem( item );
            }
        }

        public static void InitItem( ExclusionItem item )
        {
            if ( !PluginSettings.Instance.ExclusionEnabled )
                return;

            IMyEntity itemEntity;

            if ( !MyAPIGateway.Entities.TryGetEntityById( item.EntityId, out itemEntity ) )
            {
                NoGrief.Log.Info( "Couldn't initialize protection zone on entity {0}", item.EntityId );
                item.Enabled = false;
                return;
            }
            if ( itemEntity.Physics.LinearVelocity != Vector3D.Zero || itemEntity.Physics.AngularVelocity != Vector3D.Zero )
            {
                NoGrief.Log.Info( "Couldn't initialize protection zone on entity {0} -> {1}, entity is moving.", item.EntityId, itemEntity.DisplayName );
                return;
            }

            BoundingSphereD protectSphere = new BoundingSphereD( itemEntity.GetPosition( ), item.ExclusionRadius );
            List<IMyEntity> protectEntities = MyAPIGateway.Entities.GetEntitiesInSphere( ref protectSphere );
            foreach ( IMyEntity entity in protectEntities )
            {
                if ( entity is IMyCubeGrid )
                {
                    long entityId = entity.EntityId;
                    if ( !item.AllowedEntities.Contains( entityId ) )
                        item.AllowedEntities.Add( entityId );
                    List<long> ownerList = new List<long>( );

                    try
                    {
                        IMyCubeGrid tmpGrid = (IMyCubeGrid)entity;
                        ownerList = tmpGrid.BigOwners;
                    }
                    catch ( Exception ex )
                    {
                        NoGrief.Log.Info( ex, "Couldn't get owner list for entity " + entityId.ToString( ) );
                        continue;
                    }
                    foreach ( long playerID in ownerList )
                    {
                        ulong steamID = PlayerMap.Instance.GetSteamIdFromPlayerId( playerID );

                        if ( !item.AllowedPlayers.Contains( steamID ) )
                            item.AllowedPlayers.Add( steamID );
                    }
                }
            }
        }



    }
}
