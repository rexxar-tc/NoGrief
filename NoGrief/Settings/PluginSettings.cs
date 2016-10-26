using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NoGriefPlugin.Protection;
using NoGriefPlugin.Settings;
using NoGriefPlugin.UtilityClasses;

namespace NoGriefPlugin
{
    [Serializable]
    public class PluginSettings
    {
        #region Constructor

        public PluginSettings()
        {
            _serverChatName = "Server";
            _protectionItems = new MTObservableCollection<SettingsProtectionItem>();
            _protectionItems.CollectionChanged += ItemsCollectionChanged;
            _protectionItems.CollectionChanged += (sender, args) => ProtectionMain.Instance.Init();

            _exclusionItems = new MTObservableCollection<SettingsExclusionItem>();
            _exclusionItems.CollectionChanged += ItemsCollectionChanged;

            _pasteLimitMessagePrivate = "";
            _pasteLimitMessagePublic = "";
        }

        #endregion

        #region Private Fields
        
        private string _serverChatName;

        private static PluginSettings _instance;
        private static bool _loading;

        private bool _protectionZonesEnabled;
        private MTObservableCollection<SettingsProtectionItem> _protectionItems;
        private bool _exclusionZonesEnabled;
        private MTObservableCollection<SettingsExclusionItem> _exclusionItems;
        private bool _limitPasteSize;
        private int _pasteBlockCount;
        private string _pasteLimitMessagePrivate;
        private string _pasteLimitMessagePublic;
        private bool _pasteLimitKick;
        private bool _pasteLimitBan;
        private bool _spaceMasterPasteExempt;
        private bool _adminPasteExempt;

        private bool _limitProjectionSize;
        private int _projectionBlockCount;
        private bool _adminProjectionExempt;
        private string _projectionLimitMessage;

        private bool _stopPlanetPaste;
        private bool _stopAsteroidPaste;
        private bool _voxelPasteSpaceMaster;
        private bool _voxelPasteKick;
        private bool _voxlePasteBan;
        private bool _stopVoxelHands;

        #endregion

        #region Static Properties
        
        public static PluginSettings Instance
        {
            get { return _instance ?? (_instance = new PluginSettings()); }
        }

        #endregion

        #region Properties

        public string ServerChatName
        {
            get { return _serverChatName; }
            set
            {
                _serverChatName = value;
                Save();
            }
        }

        public bool ProtectionZonesEnabled
        {
            get { return _protectionZonesEnabled; }
            set
            {
                _protectionZonesEnabled = value;
                Save();
            }
        }

        public MTObservableCollection<SettingsProtectionItem> ProtectionItems
        {
            get { return _protectionItems; }
            set
            {
                _protectionItems = value;
                Save();
            }
        }

        public bool ExclusionZonesEnabled
        {
            get { return _exclusionZonesEnabled; }
            set
            {
                _exclusionZonesEnabled = value; 
                Save();
            }
        }

        public MTObservableCollection<SettingsExclusionItem> ExclusionItems
        {
            get { return _exclusionItems; }
            set
            {
                _exclusionItems = value;
                Save();
            }
        }

        public bool LimitPasteSize
        {
            get { return _limitPasteSize; }
            set
            {
                _limitPasteSize = value;
                Save();
                ProtectionMain.Instance.Init();
            }
        }

        public int PasteBlockCount
        {
            get { return _pasteBlockCount; }
            set
            {
                _pasteBlockCount = value;
                Save();
            }
        }

        public string PasteLimitMessagePrivate
        {
            get { return _pasteLimitMessagePrivate; }
            set
            {
                _pasteLimitMessagePrivate = value;
                Save();
            }
        }

        public string PasteLimitMessagePublic
        {
            get { return _pasteLimitMessagePublic; }
            set
            {
                _pasteLimitMessagePublic = value;
                Save();
            }
        }

        public bool PasteLimitKick
        {
            get { return _pasteLimitKick; }
            set
            {
                _pasteLimitKick = value;
                Save();
            }
        }

        public bool PasteLimitBan
        {
            get { return _pasteLimitBan; }
            set
            {
                _pasteLimitBan = value;
                Save();
            }
        }

        public bool SpaceMasterPasteExempt
        {
            get {return _spaceMasterPasteExempt; }
            set
            {
                _spaceMasterPasteExempt = value;
                Save();
            }
        }

        public bool AdminPasteExempt
        {
            get { return _adminPasteExempt; }
            set
            {
                _adminPasteExempt = value;
                Save();
            }
        }

        public bool LimitProjectionSize
        {
            get { return _limitProjectionSize; }
            set
            {
                _limitProjectionSize = value;
                ProtectionMain.Instance.Init();
                Save();
            }
        }

        public int ProjectionBlockCount
        {
            get { return _projectionBlockCount; }
            set
            {
                _projectionBlockCount = value;
                Save();
            }
        }

        public bool AdminProjectionExempt
        {
            get {return _adminProjectionExempt; }
            set
            {
                _adminProjectionExempt = value;
                Save();
            }
        }

        public string ProjectionLimitMessage
        {
            get { return _projectionLimitMessage; }
            set
            {
                _projectionLimitMessage = value;
                Save();
            }
        }

        public bool StopPlanetPaste
        {
            get { return _stopPlanetPaste; }
            set
            {
                _stopPlanetPaste = value;
                Save();
                ProtectionMain.Instance.Init();
            }
        }

        public bool StopAsteroidPaste
        {
            get { return _stopAsteroidPaste; }
            set
            {
                _stopAsteroidPaste = value;
                Save();
                ProtectionMain.Instance.Init();
            }
        }

        public bool VoxelPasteSpaceMaster
        {
            get { return _voxelPasteSpaceMaster; }
            set
            {
                _voxelPasteSpaceMaster = value; 
                Save();
            }
        }

        public bool VoxelPasteKick
        {
            get { return _voxelPasteKick; }
            set
            {
                _voxelPasteKick = value;
                Save();
            }
        }

        public bool VoxelPasteBan
        {
            get { return _voxlePasteBan; }
            set
            {
                _voxlePasteBan = value;
                Save();
            }
        }

        public bool StopVoxelHands
        {
            get { return _stopVoxelHands; }
            set
            {
                _stopVoxelHands = value;
                Save();
                ProtectionMain.Instance.Init();
            }
        }

        #endregion

        #region Loading and Saving

        /// <summary>
        ///     Loads our settings
        /// </summary>
        public void Load()
        {
            _loading = true;

            try
            {
                lock (this)
                {
                    string fileName = NoGrief.PluginPath + "NoGrief-Settings.xml";
                    if (File.Exists(fileName))
                    {
                        using (var reader = new StreamReader(fileName))
                        {
                            var x = new XmlSerializer(typeof(PluginSettings));
                            var settings = (PluginSettings)x.Deserialize(reader);
                            reader.Close();

                            _instance = settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NoGrief.Log.Error(ex);
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        ///     Saves our settings
        /// </summary>
        public void Save()
        {
            if (_loading)
                return;

            try
            {
                lock (this)
                {
                    string fileName = NoGrief.PluginPath + "NoGrief-Settings.xml";
                    using (var writer = new StreamWriter(fileName))
                    {
                        var x = new XmlSerializer(typeof(PluginSettings));
                        x.Serialize(writer, _instance);
                        writer.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                NoGrief.Log.Error(ex);
            }
        }

        #endregion

        #region Events

        /// <summary>
        ///     Triggered when items changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine("PropertyChanged()");
            Save();
        }

        #endregion
    }
}