using System;
using NoGriefPlugin.NetworkHandlers;
using SEModAPIInternal.API.Server;

namespace NoGriefPlugin.Protection
{
    public class ProtectionMain
    {
        private static ProtectionMain _instance;
        public static ProtectionMain Instance => _instance ?? (_instance = new ProtectionMain());
        private static InitEnum _initFlags;
        
        //only register the handlers we need so we don't bog down the network processing
        public void Init()
        {
            foreach (var item in PluginSettings.Instance.ProtectionItems)
            {
                if (item.StopRemoveBlock && !_initFlags.HasFlag(InitEnum.BlockRemove))
                {
                    _initFlags |= InitEnum.BlockRemove;
                    ServerNetworkManager.Instance.RegisterNetworkHandler(new RemoveBlockHandler());
                    NoGrief.Log.Info("Init RemoveBlock");
                }
                if (item.StopBuild && !_initFlags.HasFlag(InitEnum.BlockAdd))
                {
                    _initFlags |= InitEnum.BlockAdd;
                    ServerNetworkManager.Instance.RegisterNetworkHandler(new BuildBlockHandler());
                    NoGrief.Log.Info("Init BuildBlock");
                }
                if (item.StopPaint && !_initFlags.HasFlag(InitEnum.BlockPaint))
                {
                    _initFlags |= InitEnum.BlockPaint;
                    ServerNetworkManager.Instance.RegisterNetworkHandler(new ColorBlockHandler());
                    NoGrief.Log.Info("Init ColorBlock");
                }
                if (item.StopDeleteGrid && !_initFlags.HasFlag(InitEnum.GridDelete))
                {
                    _initFlags |= InitEnum.GridDelete;
                    ServerNetworkManager.Instance.RegisterNetworkHandler(new GridDeleteHandler());
                    NoGrief.Log.Info("Init GridDelete");
                }
            }

            if (PluginSettings.Instance.LimitPasteSize && !_initFlags.HasFlag(InitEnum.PasteLimit))
            {
                _initFlags |= InitEnum.PasteLimit;
                ServerNetworkManager.Instance.RegisterNetworkHandler(new GridPasteHandler());
                NoGrief.Log.Info("Init GridPaste");
            }
        }

        [Flags]
        private enum InitEnum
        {
            BlockRemove = 1,
            BlockAdd = 1 << 1,
            BlockPaint = 1 << 2,
            GridDelete = 1 << 3,
            PasteLimit = 1 << 4,
        }
    }
}
