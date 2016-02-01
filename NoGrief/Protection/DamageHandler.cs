namespace NoGriefPlugin.Utility
{

    using System;
    using System.Collections.Generic;
    using NoGriefPlugin;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;
    using VRageMath;
    using Sandbox.Game.Weapons;
    using VRage.ModAPI;
    using Settings;
    public class DamageHandler
    {
        

        private static bool _init = false;
        private static DateTime _lastLog;
        private static HashSet<long> _protectedId = new HashSet<long>( );
        private static Dictionary<long, BoundingSphereD> _bulletProtect = new Dictionary<long, BoundingSphereD>( );

        public static HashSet<long> ProtectedId
        {
            get
            {
                return _protectedId;
            }
            set
            {
                _protectedId = value;
            }
        }

        public static Dictionary<long, BoundingSphereD> BulletProtect
        {
            get
            {
                return _bulletProtect;
            }
            set
            {
                _bulletProtect = value;
            }
        }

        public static void Init( )
        {
            if ( _init )
                return;

            _init = true;
            //register damage handler with priority 1 to avoid conflict with Essentials (priority 0)
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler( 1, ProcessDamage );
        }

        public static void ProcessDamage( object target, ref MyDamageInformation info )
        {
        bool found = false;
            try
            {
                ///check plugin settings
                IMySlimBlock block = target as IMySlimBlock;
                if ( block == null )
                    return;

                long gridId = block.CubeGrid.EntityId;
                IMyEntity attacker;
                if ( !MyAPIGateway.Entities.TryGetEntityById( info.AttackerId, out attacker ) )
                    return;
                /*
                if ( PluginSettings.Instance.ProtectionEnabled )
                {
                    foreach ( ProtectionItem item in PluginSettings.Instance.ProtectionItems )
                    {
                        if ( !item.Enabled )
                            continue;

                        IMyEntity entity;
                        if ( !MyAPIGateway.Entities.TryGetEntityById( item.EntityId, out entity ) )
                            continue;

                        //see if our entity is within the protection radius
                        if ( Vector3D.Distance( block.CubeGrid.GetPosition( ), entity.GetPosition( ) ) <= item.ProtectionRadius )
                        {
                            info.Amount = 0;
                            found = true;
                        }
                    }
                }*/

                if ( _protectedId.Contains( gridId ) )
                {
                    info.Amount = 0;
                    found = true;
                }
                else if ( _bulletProtect.ContainsKey( gridId ) && info.Type == MyDamageType.Bullet )
                {
                    BoundingSphereD sphere = new BoundingSphereD( );
                    if ( !_bulletProtect.TryGetValue( gridId, out sphere ) )
                    {
                        NoGrief.Log.Info( "Failed to get bounding sphere on " + gridId.ToString( ) );
                        return;
                    }
                    if ( Vector3D.Distance( attacker.GetPosition( ), sphere.Center ) >= sphere.Radius )
                    {
                        info.Amount = 0;
                        found = true;
                    }
                }

                if ( PluginSettings.Instance.ProtectedEntities.Contains( gridId ) )
                {
                    info.Amount = 0;
                    found = true;
                }

                if ( found && DateTime.Now - _lastLog > TimeSpan.FromSeconds( 1 ) )
                {
                    _lastLog = DateTime.Now;
                    NoGrief.Log.Info( "Protected entity \"{0}\"", block.CubeGrid.DisplayName );
                }
            }
            catch ( Exception ex )
            {
                NoGrief.Log.Error( ex, "fail damage handler" );
            }
        }
    }
}
