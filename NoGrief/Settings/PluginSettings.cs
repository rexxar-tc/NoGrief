using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
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
        }

        #endregion

        #region Private Fields
        
        private string _serverChatName;

        private static PluginSettings _instance;
        private static bool _loading;

        #endregion

        #region Static Properties

        public static DateTime RestartStartTime { get; private set; }

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