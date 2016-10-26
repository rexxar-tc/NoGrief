using System;
using NoGriefPlugin.NetworkHandlers;
using SEModAPIInternal.API.Server;

namespace NoGriefPlugin.Protection
{
    public class ProtectionMain
    {
        private static ProtectionMain _instance;
        public static ProtectionMain Instance => _instance ?? (_instance = new ProtectionMain());
        private static InitFlags _initFlags;
        
        //only register the handlers we need so we don't bog down the network processing
        public void Init()
        {
            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (item.StopRemoveBlock && !_initFlags.HasFlag(InitFlags.BlockRemove))
                {
                    _initFlags |= InitFlags.BlockRemove;
                    ServerNetworkManager.Instance.RegisterNetworkHandlers(new RemoveBlockHandler());
                    NoGrief.Log.Info("Init RemoveBlock");
                }
                if (item.StopBuild && !_initFlags.HasFlag(InitFlags.BlockAdd))
                {
                    _initFlags |= InitFlags.BlockAdd;
                    ServerNetworkManager.Instance.RegisterNetworkHandlers(new BuildBlockHandler(), new SpawnGridHandler());
                    NoGrief.Log.Info("Init BuildBlock");
                }
                if (item.StopPaint && !_initFlags.HasFlag(InitFlags.BlockPaint))
                {
                    _initFlags |= InitFlags.BlockPaint;
                    ServerNetworkManager.Instance.RegisterNetworkHandlers(new ColorBlockHandler());
                    NoGrief.Log.Info("Init ColorBlock");
                }
                if (item.StopDeleteGrid && !_initFlags.HasFlag(InitFlags.GridDelete))
                {
                    _initFlags |= InitFlags.GridDelete;
                    ServerNetworkManager.Instance.RegisterNetworkHandlers(new GridDeleteHandler());
                    NoGrief.Log.Info("Init GridDelete");
                }
            }

            if (PluginSettings.Instance.LimitPasteSize && !_initFlags.HasFlag(InitFlags.PasteLimit))
            {
                _initFlags |= InitFlags.PasteLimit;
                ServerNetworkManager.Instance.RegisterNetworkHandlers(new GridPasteHandler());
                NoGrief.Log.Info("Init GridPaste");
            }

            if (PluginSettings.Instance.LimitProjectionSize && !_initFlags.HasFlag(InitFlags.ProjectionLimit))
            {
                _initFlags |= InitFlags.ProjectionLimit;
                ServerNetworkManager.Instance.RegisterNetworkHandlers(new ProjectionHandler());
                NoGrief.Log.Info("Init ProjectionHandler");
            }

            if (PluginSettings.Instance.StopPlanetPaste && !_initFlags.HasFlag(InitFlags.PlanetPaste))
            {
                _initFlags |= InitFlags.PlanetPaste;
                ServerNetworkManager.Instance.RegisterNetworkHandlers(new SpawnPlanetHandler());
                NoGrief.Log.Info("Init Planet");
            }

            if(PluginSettings.Instance.StopAsteroidPaste && !_initFlags.HasFlag(InitFlags.AsteroidPaste))
            {
                _initFlags |= InitFlags.AsteroidPaste;
                ServerNetworkManager.Instance.RegisterNetworkHandlers(new SpawnAsteroidHandler());
                NoGrief.Log.Info("Init Asteroid");
            }

            if (PluginSettings.Instance.StopVoxelHands && !_initFlags.HasFlag(InitFlags.VoxelHand))
            {
                _initFlags |= InitFlags.VoxelHand;
                ServerNetworkManager.Instance.RegisterNetworkHandlers(new VoxelOperationHandler());
                NoGrief.Log.Info("Init VoxelOperation");
            }
        }

        [Flags]
        private enum InitFlags 
        {
            BlockRemove = 1,
            BlockAdd = 1 << 1,
            BlockPaint = 1 << 2,
            GridDelete = 1 << 3,
            PasteLimit = 1 << 4,
            ProjectionLimit = 1 << 5,
            PlanetPaste = 1 << 6,
            AsteroidPaste = 1 << 7,
            VoxelHand = 1 << 8,
        }
    }
}
