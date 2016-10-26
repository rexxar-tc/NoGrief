using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;
using NoGriefPlugin.ChatHandlers;
using NoGriefPlugin.ProcessHandlers;
using NoGriefPlugin.Protection;
using NoGriefPlugin.Settings;
using NoGriefPlugin.UtilityClasses;
using Sandbox.Game.Entities;
using SEModAPI.API.Utility;
using SEModAPIExtensions.API;
using SEModAPIExtensions.API.Plugin.Events;
using VRage.Game.Entity;
using VRage.Plugins;
using IPlugin = SEModAPIExtensions.API.Plugin.IPlugin;

namespace NoGriefPlugin
{
    public class NoGrief : IPlugin, IChatEventHandler, IPlayerEventHandler
    {
        public static Logger Log;

        #region Methods

        public void OnChatReceived(ChatManager.ChatEvent obj)
        {
            if (obj.SourceUserId == 0)
                return;


            if (obj.Message[0] == '/')
            {
            }
        }

        #endregion

        #region Constructors and Initializers

        private void DoInit(string path)
        {
            Instance = this;
            //controlForm.Text = "Testing";
            PluginPath = path;

            // Load our settings
            PluginSettings.Instance.Load();

            // Setup process handlers
            _processHandlers = new List<ProcessHandlerBase>()
                               {
                                   new ProcessProtectionZone(),
                                   new ProcessZoneBoundaries(),
                                   new ProcessExclusionZone(),
                               };

            // Setup chat handlers
            _chatHandlers = new List<ChatHandlerBase>();

            _processThreads = new List<Thread>();
            _processThread = new Thread(PluginProcessing);
            _processThread.Start();

            MyEntities.OnEntityAdd -= OnEntityAdd;
            MyEntities.OnEntityAdd += OnEntityAdd;
            MyEntities.OnEntityRemove -= OnEntityRemove;
            MyEntities.OnEntityRemove += OnEntityRemove;

            DamageHandler.Init();
            ProtectionMain.Instance.Init();
            Log.Info("Plugin '{0}' initialized. (Version: {1}  ID: {2})", Name, Version, Id);
        }

        #endregion

        #region Processing Loop

        private void PluginProcessing()
        {
            try
            {
                foreach (ProcessHandlerBase handler in _processHandlers)
                {
                    ProcessHandlerBase currentHandler = handler;
                    var thread = new Thread(() =>
                                            {
                                                while (_running)
                                                {
                                                    if (currentHandler.CanProcess())
                                                    {
                                                        try
                                                        {
                                                            currentHandler.Handle();
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Warn("Handler Problems: {0} - {1}", currentHandler.GetUpdateResolution(), ex);
                                                        }

                                                        // Let's make sure LastUpdate is set to now otherwise we may start processing too quickly
                                                        currentHandler.LastUpdate = DateTime.Now;
                                                    }

                                                    Thread.Sleep(100);
                                                }
                                            });

                    _processThreads.Add(thread);
                    thread.Start();
                }

                foreach (Thread thread in _processThreads)
                    thread.Join();
            }
            catch (ThreadAbortException ex)
            {
                Log.Trace(ex);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                MyEntities.OnEntityAdd -= OnEntityAdd;
                MyEntities.OnEntityRemove -= OnEntityRemove;
            }
        }

        #endregion

        public void OnEntityAdd(MyEntity obj)
        {
            ThreadPool.QueueUserWorkItem(state =>
                                         {
                                             foreach (ProcessHandlerBase handler in _processHandlers)
                                                 handler.OnEntityAdd(obj);
                                         });
        }

        public void OnEntityRemove(MyEntity obj)
        {
            ThreadPool.QueueUserWorkItem(state =>
                                         {
                                             foreach (ProcessHandlerBase handler in _processHandlers)
                                                 handler.OnEntityRemove(obj);
                                         });
        }

        public void OnSectorSaved(object state)
        {
            foreach (ProcessHandlerBase handler in _processHandlers)
                handler.OnSectorSaved();
        }

        #region Private Fields

        internal static NoGrief Instance;
        private Thread _processThread;
        private List<Thread> _processThreads;
        private List<ProcessHandlerBase> _processHandlers;
        private List<ChatHandlerBase> _chatHandlers;
        private readonly bool _running = true;

        #endregion

        #region Properties

        public static string PluginPath { get; set; }

        [Category("Chat Settings")]
        [Description("Chat messages sent from the server will show this name. \r\nNote: This is set separately from SESE and Essentials.")]
        public string ServerChatName
        {
            get { return PluginSettings.Instance.ServerChatName; }
            set { PluginSettings.Instance.ServerChatName = value; }
        }

        [Category("Protection Zone")]
        public bool ProtectionZoneEnabled
        {
            get { return PluginSettings.Instance.ProtectionZonesEnabled; }
            set { PluginSettings.Instance.ProtectionZonesEnabled = value; }
        }

        [Category("Protection Zone")]
        [Description("All grids inside the protection zone are protected by the rules in the protection item")]
        public MTObservableCollection<SettingsProtectionItem> ProtectionItems
        {
            get { return PluginSettings.Instance.ProtectionItems; }
            set { PluginSettings.Instance.ProtectionItems = value; }
        }

        [Category("Exclusion Zone")]
        public bool ExclusionZoneEnabled
        {
            get { return PluginSettings.Instance.ExclusionZonesEnabled; }
            set { PluginSettings.Instance.ExclusionZonesEnabled = value; }
        }

        [Category("Exclusion Zone")]
        public MTObservableCollection<SettingsExclusionItem> ExclusionItems
        {
            get { return PluginSettings.Instance.ExclusionItems; }
            set { PluginSettings.Instance.ExclusionItems = value; }
        }

        [Category("Paste Limitations")]
        [Description("Enables the paste size limit")]
        public bool LimitPasteSize
        {
            get { return PluginSettings.Instance.LimitPasteSize; }
            set { PluginSettings.Instance.LimitPasteSize = value; }
        }

        [Category("Paste Limitations")]
        [Description("The maximum number of blocks a player can paste at once.")]
        public int PasteBlockCount
        {
            get { return PluginSettings.Instance.PasteBlockCount; }
            set { PluginSettings.Instance.PasteBlockCount = value; }
        }

        [Category("Paste Limitations")]
        [Description("This message is shown only to the player who violates the limit.")]
        public string PasteLimitMessagePrivate
        {
            get { return PluginSettings.Instance.PasteLimitMessagePrivate; }
            set { PluginSettings.Instance.PasteLimitMessagePrivate = value; }
        }

        [Category("Paste Limitations")]
        [Description("This message is sent globally. %player% is replaced with the offending player's name, and %count% is replaced with the number of blocks they tried to paste.")]
        public string PasteLimitMessagePublic
        {
            get { return PluginSettings.Instance.PasteLimitMessagePublic; }
            set { PluginSettings.Instance.PasteLimitMessagePublic = value; }
        }

        [Category("Paste Limitations")]
        [Description("Players will be kicked automatically 30 seconds after breaking the rule")]
        public bool PasteLimitKick
        {
            get { return PluginSettings.Instance.PasteLimitKick; }
            set { PluginSettings.Instance.PasteLimitKick = value; }
        }

        [Category("Paste Limitations")]
        [Description("Players will be banned automatically 30 seconds after breaking the rule")]
        public bool PasteLimitBan
        {
            get { return PluginSettings.Instance.PasteLimitBan; }
            set { PluginSettings.Instance.PasteLimitBan = value; }
        }

        [Category("Paste Limitations")]
        public bool SpaceMasterPasteExempt
        {
            get { return PluginSettings.Instance.SpaceMasterPasteExempt; }
            set { PluginSettings.Instance.SpaceMasterPasteExempt = value; }
        }

        [Category("Paste Limitations")]
        public bool AdminPasteExempt
        {
            get { return PluginSettings.Instance.AdminPasteExempt; }
            set { PluginSettings.Instance.AdminPasteExempt = value; }
        }

        [Category("Projection Limitations")]
        [Description("Puts a limit on the size of ships players can load into a projector")]
        public bool LimitProjectionSize
        {
            get { return PluginSettings.Instance.LimitProjectionSize; }
            set { PluginSettings.Instance.LimitProjectionSize = value; }
        }

        [Category("Projection Limitations")]
        public int ProjectionBlockCount
        {
            get { return PluginSettings.Instance.ProjectionBlockCount; }
            set { PluginSettings.Instance.ProjectionBlockCount = value; }
        }

        [Category("Projection Limitations")]
        [Description("Admins or projectors owned by admins are allowed to bypass the limit")]
        public bool AdminProjectionExempt
        {
            get { return PluginSettings.Instance.AdminProjectionExempt; }
            set { PluginSettings.Instance.AdminProjectionExempt = value; }
        }

        [Category("Projection Limitations")]
        [Description("This message will be shown to players who try to load a projection over the limit")]
        public string ProjectionLimitMessage
        {
            get { return PluginSettings.Instance.ProjectionLimitMessage; }
            set { PluginSettings.Instance.ProjectionLimitMessage = value; }
        }

        [Category("Voxel Paste")]
        [Description("Prevents users pasting planets")]
        public bool StopPlanetPaste
        {
            get { return PluginSettings.Instance.StopPlanetPaste; }
            set { PluginSettings.Instance.StopPlanetPaste = value; }
        }

        [Category("Voxel Paste")]
        [Description("Prevents users pasting asteroids")]
        public bool StopAsteroidPaste
        {
            get { return PluginSettings.Instance.StopAsteroidPaste; }
            set { PluginSettings.Instance.StopAsteroidPaste = value; }
        }

        [Category("Voxel Paste")]
        [Description("Users with Space Master rights can paste voxels")]
        public bool VoxelPasteSpaceMaster
        {
            get { return PluginSettings.Instance.VoxelPasteSpaceMaster; }
            set { PluginSettings.Instance.VoxelPasteSpaceMaster = value; }
        }

        [Category("Voxel Paste")]
        public bool VoxelPasteKick
        {
            get { return PluginSettings.Instance.VoxelPasteKick; }
            set { PluginSettings.Instance.VoxelPasteKick = value; }
        }

        [Category("Voxel Paste")]
        public bool VoxelPasteBan
        {
            get { return PluginSettings.Instance.VoxelPasteBan; }
            set { PluginSettings.Instance.VoxelPasteBan = value; }
        }

        [Category("Voxel Paste")]
        public bool StopVoxelHands
        {
            get { return PluginSettings.Instance.StopVoxelHands; }
            set { PluginSettings.Instance.StopVoxelHands = value; }
        }
        #endregion

        #region IPlugin Members

        public void Init()
        {
            Log.Debug("Initializing NoGrief plugin at path {0}\\", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            DoInit(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\");
        }

        public void InitWithPath(string modPath)
        {
            Log.Debug("Initializing NoGrief plugin at path {0}\\", Path.GetDirectoryName(modPath));
            DoInit(Path.GetDirectoryName(modPath) + "\\");
        }

        public void Shutdown()
        {
            Log.Info("Shutting down plugin: {0} - {1}", Name, Version);

            foreach (Thread thread in _processThreads)
                thread.Abort();

            _processThread.Abort();
        }

        public void Update()
        {
        }

#endregion

#region IChatEventHandler Members

        public void OnMessageReceived()
        {
        }


        public void HandleChatMessage(ulong steamId, string message)
        {
            // Parse chat message
            ulong remoteUserId = steamId;
            List<string> commandParts = CommandParser.GetCommandParts(message);

            // See if we have any valid handlers
            foreach (ChatHandlerBase chatHandler in _chatHandlers)
            {
                int commandCount = 0;
                if (remoteUserId == 0 && !chatHandler.AllowedInConsole())
                    continue;

                if (chatHandler.CanHandle(remoteUserId, commandParts.ToArray(), ref commandCount))
                {
                    try
                    {
                        chatHandler.HandleCommand(remoteUserId, commandParts.Skip(commandCount).ToArray());
                    }
                    catch (Exception ex)
                    {
                        Log.Info(string.Format("ChatHandler Error: {0}", ex));
                    }
                }
            }
        }
        

        public void OnChatSent(ChatManager.ChatEvent obj)
        {
        }

#endregion

#region IPlayerEventHandler Members

        public void OnPlayerJoined(ulong remoteUserId)
        {
            foreach (ProcessHandlerBase handler in _processHandlers)
                handler.OnPlayerJoined(remoteUserId);
        }

        public void OnPlayerLeft(ulong remoteUserId)
        {
            foreach (ProcessHandlerBase handler in _processHandlers)
                handler.OnPlayerLeft(remoteUserId);
        }

        public void OnPlayerWorldSent(ulong remoteUserId)
        {
        }

#endregion

#region IPlugin Members

        public Guid Id
        {
            get
            {
                var guidAttr = (GuidAttribute)typeof(NoGrief).Assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
                return new Guid(guidAttr.Value);
            }
        }

        public string Name
        {
            get { return "NoGrief Plugin"; }
        }

        public Version Version
        {
            get { return typeof(NoGrief).Assembly.GetName().Version; }
        }

#endregion
    }
}