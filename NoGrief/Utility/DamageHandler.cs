namespace NoGriefPlugin.Utility
{

    using System;
    using NoGriefPlugin;
    using Sandbox.ModAPI;
    using SEModAPIInternal.API.Common;

    public class DamageHandler
    {
        private static bool _init = false;
        //private static DateTime _lastLog;

        public static void Init( )
        {
            if ( _init )
                return;

            _init = true;

            //find an elegant way to do this

            //if ( EssentialsPlugin.PluginSettings.Instance.ProtectedEnabled )
            //{
            //    NoGriefPlugin.NoGrief.Log.Warn( "Entity protection is enabled in Essentials. Please disable it and restart the server before attempting to use protection in NoGrief." );
            //    return;
            //}

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler( 0, ProcessDamage );
        }

        public static void ProcessDamage( object target, ref MyDamageInformation info )
        {
            /*
            if ( !PluginSettings.Instance.ProtectedEnabled )
                return;

            IMySlimBlock block = target as IMySlimBlock;
            if ( block == null )
                return;

            IMyCubeGrid grid = block.CubeGrid;
            ulong steamId = PlayerMap.Instance.GetSteamId( info.AttackerId );

            foreach ( ProtectedItem item in PluginSettings.Instance.ProtectedItems )
            {
                if ( item.Enabled && item.EntityId == grid.EntityId )
                {
                    if ( info.Type == MyDamageType.Grind || info.Type == MyDamageType.Weld )
                    {
                        if ( PlayerManager.Instance.IsUserAdmin( steamId ) || grid.BigOwners.Contains( info.AttackerId ) )
                            return;
                    }
                    //grid owners and admins can grind or weld protected grids

                    else
                    {
                        info.Amount = 0;
                        if ( DateTime.Now - _lastLog > TimeSpan.FromSeconds( 1 ) )
                        {
                            _lastLog = DateTime.Now;
                            Essentials.Log.Info( "Protected entity \"{0}\" from player \"{1}\".", grid.DisplayName, PlayerMap.Instance.GetFastPlayerNameFromSteamId( steamId ) );
                        }
                    }
                }
            }*/
        }
    }
}
