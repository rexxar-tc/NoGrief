using System.Linq;
using NLog;
using NoGriefPlugin.Utility;
using SEModAPIInternal.API.Common;

namespace NoGriefPlugin.ChatHandlers
{
    public abstract class ChatHandlerBase
    {
        protected static readonly Logger Log = LogManager.GetLogger("PluginLog");

        public ChatHandlerBase()
        {
            Log.Debug("Added chat handler: {0}", GetCommandText());
        }

        public virtual bool CanHandle(ulong steamId, string[] words, ref int commandCount)
        {
            // Administrator Command
            if (IsAdminCommand())
            {
                if (!PlayerManager.Instance.IsUserAdmin(steamId) && steamId != 0)
                    return false;
            }

            // Check if this command has multiple commands that do the same thing
            if (GetMultipleCommandText().Length < 1)
            {
                commandCount = GetCommandText().Split(' ').Count();
                if (words.Length > commandCount - 1)
                    return string.Join(" ", words).ToLower().StartsWith(GetCommandText());
            }
            else
            {
                bool found = false;
                foreach (string command in GetMultipleCommandText())
                {
                    commandCount = command.Split(' ').Count();
                    if (words.Length > commandCount - 1)
                    {
                        found = string.Join(" ", words).ToLower().StartsWith(command);
                        if (found)
                            break;
                    }
                }

                return found;
            }

            return false;
        }

        public abstract string GetHelp();

        public abstract Communication.ServerDialogItem GetHelpDialog();

        public virtual string GetCommandText()
        {
            return "";
        }

        public virtual string[] GetMultipleCommandText()
        {
            return new string[] {};
        }

        public virtual bool IsAdminCommand()
        {
            return false;
        }

        public virtual bool AllowedInConsole()
        {
            return false;
        }

        public virtual bool IsClientOnly()
        {
            return false;
        }

        public virtual bool HandleCommand(ulong userId, string[] words)
        {
            return false;
        }
    }
}