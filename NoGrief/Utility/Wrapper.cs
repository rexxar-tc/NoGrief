using System;
using SEModAPIInternal.API.Common;

namespace NoGriefPlugin.Utility
{
    public static class Wrapper
    {
        public static void GameAction(Action action)
        {
            SandboxGameAssemblyWrapper.Instance.GameAction(action);
        }

        public static void BeginGameAction(Action action, SandboxGameAssemblyWrapper.GameActionCallback callback = null, object state = null)
        {
            SandboxGameAssemblyWrapper.Instance.BeginGameAction(action, callback, state);
        }
    }
}