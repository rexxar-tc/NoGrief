using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NoGrief.Settings;
using NoGriefPlugin.Settings;
using NoGriefPlugin.UtilityClasses;

namespace NoGriefPlugin
{
    [Serializable]
    public class PluginSettings
    {
        #region Private Fields

        private int _maxBlockCount;
        private int _maxSubgridCount;
        private int _protectionTime;
        private int _protectionRadius;
        private MTObservableCollection<AdminItem> _adminItems;
        private int _adminProtectRadius;
        private SettingsDialogItem _adminWarningItem;


        private static PluginSettings _instance;
        private static bool _loading = false;
        private static DateTime _start;

        #endregion

        #region Static Properties
        public static DateTime RestartStartTime
        {
            get
            {
                return _start;
            }
        }

        public static PluginSettings Instance
        {
            get
            {
                return _instance ?? (_instance = new PluginSettings( ));
            }
        }
        #endregion

        #region Properties

        public int MaxBlockCount
        {
            get
            {
                return _maxBlockCount;
            }
            set
            {
                _maxBlockCount = value;
                Save( );
            }
        }

        public int MaxSubgridCount
        {
            get
            {
                return _maxSubgridCount;
            }
            set
            {
                _maxSubgridCount = value;
                Save( );
            }
        }

        public int ProtectionTime
        {
            get
            {
                return _protectionTime;
            }
            set
            {
                _protectionTime = value;
                Save( );
            }
        }

        public int ProtectionRadius
        {
            get
            {
                return _protectionRadius;
            }
            set
            {
                _protectionRadius = value;
                Save( );
            }
        }

        public MTObservableCollection<AdminItem> AdminItems
        {
            get
            {
                return _adminItems;
            }
            set
            {
                _adminItems = value;
                Save( );
            }
        }

        public int AdminProtectRadius
        {
            get
            {
                return _adminProtectRadius;
            }
            set
            {
                _adminProtectRadius = value;
                Save( );
            }
        }

        public SettingsDialogItem AdminWarningItem
        {
            get
            {
                return _adminWarningItem;
            }
            set
            {
                _adminWarningItem = value;
                Save( );
            }
        }

        #endregion



        #region Constructor
        public PluginSettings( )
        {
            // Default is 12 hours
            _start = DateTime.Now;

            _adminProtectRadius = 10000;
            _adminItems = new MTObservableCollection<AdminItem>();
            _adminItems.CollectionChanged += ItemsCollectionChanged;
            _maxBlockCount = 100;
            _maxSubgridCount = 5;
            _protectionRadius = 5000;
            _protectionTime = 10;

            
        }


        #endregion

        #region Loading and Saving

        /// <summary>
        /// Loads our settings
        /// </summary>
        public void Load( )
        {
            _loading = true;

            try
            {
                lock ( this )
                {
                    String fileName = NoGrief.PluginPath + "Essential-Settings.xml";
                    if ( File.Exists( fileName ) )
                    {
                        using ( StreamReader reader = new StreamReader( fileName ) )
                        {
                            XmlSerializer x = new XmlSerializer( typeof( PluginSettings ) );
                            PluginSettings settings = (PluginSettings)x.Deserialize( reader );
                            reader.Close( );

                            _instance = settings;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                NoGrief.Log.Error( ex );
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        /// Saves our settings
        /// </summary>
        public void Save( )
        {
            if ( _loading )
                return;

            try
            {
                lock ( this )
                {
                    String fileName = NoGrief.PluginPath + "Essential-Settings.xml";
                    using ( StreamWriter writer = new StreamWriter( fileName ) )
                    {
                        XmlSerializer x = new XmlSerializer( typeof( PluginSettings ) );
                        x.Serialize( writer, _instance );
                        writer.Close( );
                    }
                }
            }
            catch ( Exception ex )
            {
                NoGrief.Log.Error( ex );
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Triggered when items changes.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemsCollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            Save( );
        }

        private void OnPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            Console.WriteLine( "PropertyChanged()" );
            Save( );
        }

        #endregion
    }
}
