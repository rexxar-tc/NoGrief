namespace NoGriefPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Xml.Serialization;
    using NoGriefPlugin.Settings;
    using NoGriefPlugin.UtilityClasses;

    [Serializable]
    public class PluginSettings
    {
        #region Private Fields

        private int _maxBlockCount;
        private int _maxSubgridCount;
        private int _protectionZoneTime;
        private int _protectionZoneRadius;
        private MTObservableCollection<ExclusionItem> _exclusionItems;
        private bool _exclusionEnabled;
        private bool _exclusionLogging;
        private string _serverChatName;
        private bool _protectionZoneEnabled;
        private MTObservableCollection<ProtectionItem> _protectionZoneItems;

        private List<long> _protectedEntities;
        private bool _creativeProtection;
        private bool _maxCreate;
        private int _maxCreateSize;
        private bool _createNotify;

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

        public string ServerChatName
        {
            get
            {
                return _serverChatName;
            }
            set
            {
                _serverChatName = value;
                Save( );
            }
        }

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

        public int ProtectionZoneTime
        {
            get
            {
                return _protectionZoneTime;
            }
            set
            {
                _protectionZoneTime = value;
                Save( );
            }
        }

        public int ProtectionZoneRadius
        {
            get
            {
                return _protectionZoneRadius;
            }
            set
            {
                _protectionZoneRadius = value;
                Save( );
            }
        }

        public bool ProtectionZoneEnabled
        {
            get
            {
                return _protectionZoneEnabled;
            }
            set
            {
                _protectionZoneEnabled = value;
                Save( );
            }
        }

        public MTObservableCollection<ProtectionItem> ProtectionZoneItems
        {
            get
            {
                return _protectionZoneItems;
            }
            set
            {
                _protectionZoneItems = value;
                Save( );
            }
        }
        public List<long> ProtectedEntities
        {
            get
            {
                return _protectedEntities;
            }
            set
            {
                _protectedEntities = value;
                Save( );
            }
        }

        public bool CreativeProtection
        {
            get
            {
                return _creativeProtection;
            }
            set
            {
                _creativeProtection = value;
                Save( );
            }
        }

        public bool MaxCreate
        {
            get
            {
                return _maxCreate;
            }
            set
            {
                _maxCreate = value;
                Save( );
            }
        } 

        public int MaxCreateSize
        {
            get
            {
                return _maxCreateSize;
            }
            set
            {
                _maxCreateSize = value;
                Save( );
            }
        }

        public bool CreateNotify
        {
            get
            {
                return _createNotify;
            }
            set
            {
                _createNotify = value;
                Save( );
            }
        }

        public MTObservableCollection<ExclusionItem> ExclusionItems
        {
            get
            {
                return _exclusionItems;
            }
            set
            {
                _exclusionItems = value;
                Save( );
            }
        }

        public bool ExclusionEnabled
        {
            get
            {
                return _exclusionEnabled;
            }
            set
            {
                _exclusionEnabled = value;
                Save( );
            }
        }

        public bool ExclusionLogging
        {
            get
            {
                return _exclusionLogging;
            }
            set
            {
                _exclusionLogging = value;
                Save( );
            }
        }

        #endregion



        #region Constructor
        public PluginSettings( )
        {
            // Default is 12 hours
            _start = DateTime.Now;

            _exclusionEnabled = false;
            _exclusionLogging = false;
            _exclusionItems = new MTObservableCollection<ExclusionItem>( );
            _exclusionItems.CollectionChanged += ItemsCollectionChanged;
            _maxBlockCount = 100;
            _maxSubgridCount = 5;
            _protectionZoneRadius = 5000;
            _protectionZoneTime = 10;
            _protectionZoneEnabled = false;
            _protectionZoneItems = new MTObservableCollection<ProtectionItem>( );
            _protectionZoneItems.CollectionChanged += ItemsCollectionChanged;
            _serverChatName = "Server";

            _protectedEntities = new List<long>( );
            _creativeProtection = false;
            _maxCreate = false;
            _maxCreateSize = 1000;
            _createNotify = true;

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
                    String fileName = NoGrief.PluginPath + "NoGrief-Settings.xml";
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
                    String fileName = NoGrief.PluginPath + "NoGrief-Settings.xml";
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
