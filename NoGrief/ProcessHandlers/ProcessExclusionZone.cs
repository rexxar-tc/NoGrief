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
    public class ProcessExclusionZone : ProcessHandlerBase
    {
        private static bool _init = false;
        public static SortedList<long, HashSet<long>> WarnList = new SortedList<long, HashSet<long>>( );

        public override int GetUpdateResolution( )
        {
            return 1000;
        }

        public override void Handle( )
        {
            if ( PluginSettings.Instance.ExclusionEnabled )
            {
                Dictionary<long, BoundingSphereD> bulletProtect = new Dictionary<long, BoundingSphereD>( );

                if ( !_init )
                    Init( );

                foreach ( ExclusionItem item in PluginSettings.Instance.ExclusionItems )
                {
                    if ( item.Enabled )
                    {
                        if ( item.AllowedEntities.Count < 1 )
                            InitItem( item );
                        
                        HashSet<long> ItemWarnList;
                        if ( !WarnList.TryGetValue( item.EntityId, out ItemWarnList ) )
                        {
                            ItemWarnList = new HashSet<long>( );
                            WarnList.Add( item.EntityId, ItemWarnList );
                        }

                        FactionsManager _factions = FactionsManager.Instance;
                        IMyFaction itemFaction = _factions.BackingObject.TryGetFactionByTag( item.FactionTag );

                        IMyEntity itemEntity;
                        if ( !MyAPIGateway.Entities.TryGetEntityById( item.EntityId, out itemEntity ) )
                        {
                            if ( PluginSettings.Instance.ExclusionLogging )
                                NoGrief.Log.Info( "Error processing exclusion zone on entity {0}, could not get entity.", item.EntityId );
                            item.Enabled = false;
                            continue;
                        }
                        if ( itemEntity.Physics.LinearVelocity != Vector3D.Zero || itemEntity.Physics.AngularVelocity != Vector3D.Zero )
                        {
                            if ( PluginSettings.Instance.ExclusionLogging )
                                NoGrief.Log.Debug( "Not processing exclusion zone on entity {0} -> {1} because it is moving.", item.EntityId, itemEntity.DisplayName );
                            continue;
                        }

                        //create a second sphere 100m larger, this is our boundary zone
                        BoundingSphereD protectSphere = new BoundingSphereD( itemEntity.GetPosition( ), item.ExclusionRadius + 100 );
                        List<MyEntity> excludedEntities = MyEntities.GetTopMostEntitiesInSphere( ref protectSphere );

                        //create the actual protection zone
                        protectSphere = new BoundingSphereD( itemEntity.GetPosition( ), item.ExclusionRadius );
                        List<MyEntity> protectEntities = MyEntities.GetTopMostEntitiesInSphere( ref protectSphere );

                        //check entities in our warning list
                        foreach ( long warnId in ItemWarnList )
                        {
                            IMyEntity warnEntity;
                            if ( !MyAPIGateway.Entities.TryGetEntityById( warnId, out warnEntity ) )
                            {
                                //entity no longer exists, remove it from the list
                                ItemWarnList.Remove( warnId );
                                continue;
                            }

                            if ( PluginSettings.Instance.ExclusionLogging )
                                NoGrief.Log.Debug( "Processing WarnList. Entity type: " + warnEntity.GetType( ).ToString( ) );
                            if ( !excludedEntities.Contains( (MyEntity)warnEntity ) )
                            {
                                //entity has left the exclusion zone
                                ItemWarnList.Remove( warnId );
                                if ( warnEntity is MyCharacter )
                                {
                                    if ( PluginSettings.Instance.ExclusionLogging )
                                        NoGrief.Log.Debug( "Player left exclusion zone" );
                                    MyCharacter player = (MyCharacter)warnEntity;
                                    ulong steamID = PlayerMap.Instance.GetSteamId( warnId );
                                    Communication.Notification( steamID, MyFontEnum.Green, 3, "Left exclusion zone" );
                                }
                                else if ( warnEntity is MyCubeGrid )
                                {
                                    if ( PluginSettings.Instance.ExclusionLogging )
                                        NoGrief.Log.Debug( "Ship left exclusion zone" );
                                    ItemWarnList.Remove( warnId );
                                }
                            }
                            if ( protectEntities.Contains( (MyEntity)warnEntity ) )
                            {
                                //object has moved from boundary into protection zone
                                if ( warnEntity is MyCharacter )
                                {
                                    if ( PluginSettings.Instance.ExclusionLogging )
                                        NoGrief.Log.Debug( "Found player in protection zone" );
                                    MyCharacter player = (MyCharacter)warnEntity;
                                    ItemWarnList.Remove( warnId );
                                    ulong steamID = PlayerMap.Instance.GetSteamId( warnId );
                                    if ( player.IsUsing is IMyCubeBlock )
                                    {
                                        //player is in a ship, they'll get moved along with it, simply notify them here
                                        Communication.Notification( steamID, MyFontEnum.Red, 3, "You have been removed from the protection zone." );
                                        continue;
                                    }
                                    Vector3D? tryMove = MathUtility.TraceVector( warnEntity.GetPosition( ), warnEntity.Physics.LinearVelocity, -100 );
                                    if ( tryMove == null )
                                    {

                                        if ( PluginSettings.Instance.ExclusionLogging )
                                            NoGrief.Log.Debug( "Failed to move player" );
                                    }

                                    if ( PluginSettings.Instance.ExclusionLogging )
                                        NoGrief.Log.Debug( "Moving player" );
                                    Communication.MoveMessage( steamID, "normal", (Vector3D)tryMove );
                                    Communication.Notification( steamID, MyFontEnum.Red, 3, "You have been removed from the protection zone." );
                                    //stop the object
                                    SandboxGameAssemblyWrapper.Instance.GameAction( ( ) =>
                                    {
                                        warnEntity.Physics.LinearVelocity = Vector3D.Zero;
                                        warnEntity.Physics.AngularVelocity = Vector3D.Zero;
                                    } );
                                }

                                if ( warnEntity is IMyMissileGunObject )
                                {
                                    SandboxGameAssemblyWrapper.Instance.GameAction( ( ) =>
                                    {
                                        warnEntity.Close( );
                                        MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( warnEntity ) );
                                        //this replication shouldn't be necessary, but let's force a sync just to be safe.
                                    } );
                                    ItemWarnList.Remove( warnId );
                                    continue;
                                }

                                if ( warnEntity is MyCubeGrid )
                                {
                                    ItemWarnList.Remove( warnId );
                                    Vector3D? tryMove = MathUtility.TraceVector( warnEntity.GetPosition( ), warnEntity.Physics.LinearVelocity, -100 );
                                    if ( tryMove == null )
                                    {
                                        //do something
                                    }
                                    Communication.MoveMessage( 0, "normal", (Vector3D)tryMove, warnEntity.GetTopMostParent( ).EntityId );
                                    //stop the object
                                    SandboxGameAssemblyWrapper.Instance.GameAction( ( ) =>
                                    {
                                        warnEntity.Physics.LinearVelocity = Vector3D.Zero;
                                        warnEntity.Physics.AngularVelocity = Vector3D.Zero;
                                    } );
                                    continue;
                                }

                                if ( warnEntity is MyFloatingObject )
                                {
                                    SandboxGameAssemblyWrapper.Instance.GameAction( ( ) =>
                                    {
                                        warnEntity.Close( );
                                        MyMultiplayer.ReplicateImmediatelly( MyExternalReplicable.FindByObject( warnEntity ) );
                                        //this replication shouldn't be necessary, but let's force a sync just to be safe.
                                    } );
                                    ItemWarnList.Remove( warnId );
                                    continue;
                                }
                            }
                        }

                        if ( excludedEntities.Count == protectEntities.Count )
                        {
                            //nothing to do
                            protectEntities.Clear( );
                            excludedEntities.Clear( );
                            WarnList.Remove( item.EntityId );
                            WarnList.Add( item.EntityId, ItemWarnList );
                            continue;
                        }


                        foreach ( MyEntity entity in excludedEntities )
                        {
                            if ( PluginSettings.Instance.ExclusionLogging && entity != null )
                            {
                                if ( PluginSettings.Instance.ExclusionLogging )
                                    NoGrief.Log.Info( entity.GetType( ).ToString( ) );
                            }

                            //ignore items in the protected sphere so we only process the boundary zone
                            if ( protectEntities.Contains( entity ) )
                            {
                                //protect items inside the sphere from bullets
                                if ( entity is MyCubeGrid || entity is MyCharacter )
                                    bulletProtect.Add( entity.EntityId, protectSphere );
                                continue;
                            }

                            if ( entity == null )
                                continue;

                            if ( entity is MyCubeGrid )
                            {
                                if ( PluginSettings.Instance.ExclusionLogging )
                                    NoGrief.Log.Debug( "Found ship in exclusion zone" );
                                MyCubeGrid grid = (MyCubeGrid)entity;
                                MyObjectBuilder_CubeGrid gridBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder( );
                                HashSet<ulong> playersOnboard = new HashSet<ulong>( );

                                if ( item.AllowedEntities.Contains( entity.EntityId ) && !item.TransportAdd )
                                    continue;

                                foreach ( MyObjectBuilder_CubeBlock block in gridBuilder.CubeBlocks )
                                {
                                    if ( block.TypeId == typeof( MyObjectBuilder_Cockpit ) )
                                    {
                                        MyObjectBuilder_Cockpit cockpit = (MyObjectBuilder_Cockpit)block;
                                        if ( cockpit.Pilot == null )
                                            continue;

                                        ulong steamId = PlayerMap.Instance.GetSteamId( cockpit.Pilot.EntityId );
                                        if ( !playersOnboard.Contains( steamId ) )
                                            playersOnboard.Add( steamId );

                                        if ( !ItemWarnList.Contains( cockpit.Pilot.EntityId ) )
                                            ItemWarnList.Add( cockpit.Pilot.EntityId );
                                    }
                                }

                                //there's probably a faster way to do this, but the number of items is usually pretty low, so whatever
                                bool found = false;
                                foreach ( ulong steamId in playersOnboard )
                                {
                                    

                                    if ( item.AllowedPlayers.Contains( steamId ) )
                                        found = true;
                                    else if ( itemFaction != null && itemFaction.IsMember( PlayerMap.Instance.GetFastPlayerIdFromSteamId( steamId ) ) )
                                        found = true;
                                    else if ( item.AllowAdmins && PlayerManager.Instance.IsUserAdmin( steamId ) )
                                        found = true;
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
                                    if ( ItemWarnList.Contains( entity.EntityId ) )
                                        continue;
                                    ItemWarnList.Add( entity.EntityId );

                                    foreach ( ulong steamId in playersOnboard )
                                    {
                                        if ( PluginSettings.Instance.ExclusionLogging )
                                            NoGrief.Log.Debug( "Warning player: " + PlayerMap.Instance.GetFastPlayerNameFromSteamId( steamId ) );
                                        Communication.Notification( steamId, MyFontEnum.Red, 3, "Warning: Approaching exclusion zone" );
                                    }
                                }
                            }

                            if ( entity is MyFloatingObject )
                            {
                                if ( ItemWarnList.Contains( entity.EntityId ) )
                                    continue;
                                ItemWarnList.Add( entity.EntityId );

                                if ( PluginSettings.Instance.ExclusionLogging )
                                    NoGrief.Log.Debug( "Found floating object in exclusion zone" );
                                continue;
                            }

                            if ( entity is MyCharacter )
                            {
                                if ( PluginSettings.Instance.ExclusionLogging )
                                    NoGrief.Log.Debug( "Found player in exclusion zone" );
                                MyCharacter player = (MyCharacter)entity;
                                ulong steamID = PlayerMap.Instance.GetSteamId( entity.EntityId );
                                if ( item.AllowedPlayers.Contains( steamID ) )
                                    continue;
                                else if ( itemFaction != null && itemFaction.IsMember( PlayerMap.Instance.GetFastPlayerIdFromSteamId( steamID ) ) )
                                    continue;
                                else if ( item.AllowAdmins && PlayerManager.Instance.IsUserAdmin( steamID ) )
                                    continue;
                                else
                                {
                                    if ( ItemWarnList.Contains( entity.EntityId ) )
                                        continue;
                                    ItemWarnList.Add( entity.EntityId );
                                    if ( PluginSettings.Instance.ExclusionLogging )
                                        NoGrief.Log.Debug( "Warning player: " + PlayerMap.Instance.GetFastPlayerNameFromSteamId( steamID ) );
                                    Communication.Notification( steamID, MyFontEnum.Red, 3, "Warning: Approaching exclusion zone" );
                                }
                            }

                        }

                        protectEntities.Clear( );
                        excludedEntities.Clear( );
                        WarnList.Remove( item.EntityId );
                        WarnList.Add( item.EntityId, ItemWarnList );
                    }
                }
                //update the real list of protected entities only after we're done processing them
                //it's easier to clear it like this than track when entities leave the protection zone
                DamageHandler.BulletProtect.Clear( );
                if(bulletProtect!= null )
                DamageHandler.BulletProtect = bulletProtect;
                base.Handle( );
            }
        }

        private void Init( )
        {
            _init = true;
            foreach ( ExclusionItem item in PluginSettings.Instance.ExclusionItems )
            {
                InitItem( item );
                //initialize our list of warned items
                WarnList.Add( item.EntityId, new HashSet<long>( ) );
            }
        }

        public static void InitItem( ExclusionItem item )
        {
            if ( !PluginSettings.Instance.ExclusionEnabled )
                return;

            IMyEntity itemEntity;
            if ( PluginSettings.Instance.ExclusionLogging )
                NoGrief.Log.Debug( "Initializing exclusion zone on entity {0}", item.EntityId );

            if ( !MyAPIGateway.Entities.TryGetEntityById( item.EntityId, out itemEntity ) )
            {
                if ( PluginSettings.Instance.ExclusionLogging )
                    NoGrief.Log.Info( "Couldn't initialize exclusion zone on entity {0}", item.EntityId );
                item.Enabled = false;
                return;
            }
            if ( itemEntity.Physics.LinearVelocity != Vector3D.Zero || itemEntity.Physics.AngularVelocity != Vector3D.Zero )
            {
                if ( PluginSettings.Instance.ExclusionLogging )
                    NoGrief.Log.Info( "Couldn't initialize exclusion zone on entity {0} -> {1}, entity is moving.", item.EntityId, itemEntity.DisplayName );
                return;
            }

            BoundingSphereD protectSphere = new BoundingSphereD( itemEntity.GetPosition( ), item.ExclusionRadius );
            List<MyEntity> protectEntities = MyEntities.GetTopMostEntitiesInSphere( ref protectSphere );
            foreach ( MyEntity entity in protectEntities )
            {
                if ( entity is MyCubeGrid )
                {
                    long entityId = entity.EntityId;
                    if ( !item.AllowedEntities.Contains( entityId ) )
                        item.AllowedEntities.Add( entityId );
                    List<long> ownerList = new List<long>( );

                    try
                    {
                        MyCubeGrid tmpGrid = (MyCubeGrid)entity;
                        ownerList = tmpGrid.BigOwners;
                    }
                    catch ( Exception ex )
                    {
                        if ( PluginSettings.Instance.ExclusionLogging )
                            NoGrief.Log.Info( ex, "Couldn't get owner list for entity " + entityId.ToString( ) );
                        continue;
                    }
                    foreach ( long playerID in ownerList )
                    {
                        ulong steamID;

                        //there's probably a better way to do this, but here's a bandaid for now
                        try
                        {
                            steamID = PlayerMap.Instance.GetSteamIdFromPlayerId( playerID );
                        }
                        catch
                        {
                            //owner is an NPC
                            continue;
                        }

                        if ( !item.AllowedPlayers.Contains( steamID ) )
                            item.AllowedPlayers.Add( steamID );
                    }
                }

                if ( entity is MyCharacter )
                {
                    ulong steamId = PlayerMap.Instance.GetSteamId( entity.EntityId );

                    if ( !item.AllowedPlayers.Contains( steamId ) )
                        item.AllowedPlayers.Add( steamId );
                }
            }
        }
    }
}
