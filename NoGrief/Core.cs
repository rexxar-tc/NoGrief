using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;
using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Server;
using SEModAPIInternal.Support;
using NLog;
using SEModAPI.API;

using VRageMath;
using VRage.Common.Utils;


using NoGriefPlugin.Settings;
using NoGrief.Settings;
using NoGriefPlugin;
using NoGriefPlugin.UtilityClasses;

namespace NoGriefPlugin
{
        
    public class NoGrief
    {

        public static Logger Log;
        private static string _pluginPath;

        #region "Properties"

        public static string PluginPath
        {
            get
            {
                return _pluginPath;
            }
            set
            {
                _pluginPath = value;
            }
        }

        [Browsable( true )]
        [ReadOnly( true )]
        public string Location
        {
            get
            {
                return System.IO.Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) + "\\";
            }

        }

        [Category( "Admin Protection" )]
        [Description( "Only admins can get near these grids" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public MTObservableCollection<AdminItem> AdminItems
        {
            get
            {
                return PluginSettings.Instance.AdminItems;
            }
            set
            {
                PluginSettings.Instance.AdminItems = value;
            }
        }

        [Category( "Admin Protection" )]
        [Description( "Radius, in meters, of admin protection zones" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public int AdminProtectRadius
        {
            get
            {
                return PluginSettings.Instance.AdminProtectRadius;
            }
            set
            {
                PluginSettings.Instance.AdminProtectRadius = value;
            }
        }

        [Category( "Admin Protection" )]
        [Description( "Warning dialog sent to player on approaching an admin zone" )]
        [Browsable( true )]
        [ReadOnly( false )]
        [TypeConverter( typeof( ExpandableObjectConverter ) )]
        public SettingsDialogItem AdminWarningItem
        {
            get
            {
                return PluginSettings.Instance.AdminWarningItem;
            }
            set
            {
                PluginSettings.Instance.AdminWarningItem = value;
            }
        }

        [Category( "Player Protection" )]
        [Description( "Amount of time, in minutes, after a player logs off before their station is protected" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public int ProtectionTime
        {
            get
            {
                return PluginSettings.Instance.ProtectionTime;
            }
            set
            {
                PluginSettings.Instance.ProtectionTime = value;
            }
        }

        [Category( "Player Protection" )]
        [Description( "Grids with more than this number of blocks total will not be protected" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public int MaxBlockCount
        {
            get
            {
                return PluginSettings.Instance.MaxBlockCount;
            }
            set
            {
                PluginSettings.Instance.MaxBlockCount = value;
            }
        }

        [Category( "Player Protection" )]
        [Description( "Maximum number of grids that can be included in the protection zone" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public int MaxSubgridCount
        {
            get
            {
                return PluginSettings.Instance.MaxSubgridCount;
            }
            set
            {
                PluginSettings.Instance.MaxSubgridCount = value;
            }
        }

        [Category( "Player Protection" )]
        [Description( "Radius, in meters, of the protection zone" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public int ProtectionRadius
        {
            get
            {
                return PluginSettings.Instance.ProtectionRadius;
            }
            set
            {
                PluginSettings.Instance.ProtectionRadius = value;
            }
        }

        #endregion

        #region "Methods"

        
        public void OnCubeBlockCreated( CubeBlockEntity obj )
        {
           
            return;
        }

        public void OnCubeBlockDeleted( CubeBlockEntity obj )
        {
            return;
        }

        public void OnChatReceived( ChatManager.ChatEvent obj )
        {
            if ( obj.SourceUserId == 0 )
                return;


            if ( obj.Message[0] == '/' )
            {
                bool isadmin = false;// = SandboxGameAssemblyWrapper.Instance.IsUserAdmin(obj.SourceUserId);
                string[ ] words = obj.Message.Split( ' ' );
                //string rem = "";
                //proccess

                
            }
            return;
        }
        public void OnChatSent( ChatManager.ChatEvent obj )
        {
            //do nothing
            return;
        }
        #endregion

    }
}
