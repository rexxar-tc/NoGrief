using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using VRage.Game.Entity;

namespace NoGriefPlugin.Protection
{
    public static class Protection
    {
        private static bool _init = false;

        public static void Init( )
        {
            if ( _init )
                return;

            _init = true;
            NoGrief.Log.Info( "OnEntityCreate registered" );
            MyEntities.OnEntityAdd += OnEntityAdd;
        }

        private static void OnEntityAdd( MyEntity obj )
        {
            if ( !PluginSettings.Instance.MaxCreate )
                return;

            if ( !(obj is MyCubeGrid) )
                return;

            var grid = (MyCubeGrid)obj;
           
            if ( grid.BlocksCount > PluginSettings.Instance.MaxCreateSize )
            {
                grid.Close( );
                if(PluginSettings.Instance.CreateNotify )
                    Communication.SendPublicInformation( string.Format( "Grids larger than {0} blocks cannot be spawned in this world.", PluginSettings.Instance.MaxCreateSize ) );

                NoGrief.Log.Info( "Entity creation stopped" );
            } 
        }
    }
}
