using System;
using System.Text;
using NLog;
using Sandbox;
using Sandbox.ModAPI;
using SEModAPIExtensions.API;
using SEModAPIInternal.API.Common;
using VRage.Game;

namespace NoGriefPlugin.Utility
{
    //copied from Essentials
    public static class Communication
    {
        public enum DataMessageType : long
        {
            Test = 5000,
            VoxelHeader,
            VoxelPart,
            Message,
            RemoveStubs,
            ChangeServer,
            ServerSpeed,
            Credits,

            //skipped a few addresses to avoid conflict
            //just in case
            Dialog = 5020,
            Move,
            Notification,
            MaxSpeed,
            ServerInfo,
            Waypoint,
            GMenu
        }

        private static readonly Logger Log = LogManager.GetLogger("PluginLog");
        private static readonly Random _random = new Random();

        public static void SendPublicInformation(string infoText)
        {
            if (infoText == "")
                return;

            var MessageItem = new ServerMessageItem();
            MessageItem.From = PluginSettings.Instance.ServerChatName;
            MessageItem.Message = infoText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML(MessageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            if (ChatManager.EnableData)
            {
                BroadcastDataMessage(DataMessageType.Message, data);
            }
            else
                ChatManager.Instance.SendPublicChatMessage(infoText);

            ChatManager.Instance.AddChatHistory(new ChatManager.ChatEvent(DateTime.Now, 0, infoText));
        }

        public static void SendPrivateInformation(ulong playerId, string infoText, string from = null)
        {
            if (infoText == "")
                return;

            var MessageItem = new ServerMessageItem();

            MessageItem.From = PluginSettings.Instance.ServerChatName;

            MessageItem.Message = infoText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML(MessageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            if (ChatManager.EnableData)
            {
                SendDataMessage(playerId, DataMessageType.Message, data);
            }
            else
                ChatManager.Instance.SendPrivateChatMessage(playerId, infoText);

            var chatItem = new ChatManager.ChatEvent();
            chatItem.Timestamp = DateTime.Now;
            chatItem.RemoteUserId = from == null ? 0 : PlayerMap.Instance.GetSteamIdFromPlayerName(from);
            chatItem.Message = infoText;
            ChatManager.Instance.AddChatHistory(chatItem);
        }

        public static void Notification(ulong steamId, MyFontEnum color, int timeInSeconds, string message)
        {
            var messageItem = new ServerNotificationItem
                              {
                                  color = color,
                                  time = timeInSeconds,
                                  message = message
                              };

            string messageString = MyAPIGateway.Utilities.SerializeToXML(messageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            if (steamId != 0)
                SendDataMessage(steamId, DataMessageType.Notification, data);
            else
                BroadcastDataMessage(DataMessageType.Notification, data);
        }

        public static void DisplayDialog(ulong steamId, string header, string subheader, string content, string buttonText = "OK")
        {
            var messageItem = new ServerDialogItem
                              {
                                  title = header,
                                  header = subheader,
                                  content = content,
                                  buttonText = buttonText
                              };

            string messageString = MyAPIGateway.Utilities.SerializeToXML(messageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            SendDataMessage(steamId, DataMessageType.Dialog, data);
        }

        public static void DisplayDialog(ulong steamId, ServerDialogItem messageItem)
        {
            string messageString = MyAPIGateway.Utilities.SerializeToXML(messageItem);
            byte[] data = Encoding.UTF8.GetBytes(messageString);

            SendDataMessage(steamId, DataMessageType.Dialog, data);
        }

        public static void SendDataMessage(ulong steamId, DataMessageType messageType, byte[] data)
        {
            /*
            long msgId = (long)messageType;

            string msgIdString = msgId.ToString( );
            byte[ ] newData = new byte[data.Length + msgIdString.Length + 1];
            newData[0] = (byte)msgIdString.Length;
            for ( int r = 0; r < msgIdString.Length; r++ )
                newData[r + 1] = (byte)msgIdString[r];

            Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );
            */

            //hash a random long with the current time to make a decent quality guid for each message
            var randLong = new byte[sizeof(long)];
            _random.NextBytes(randLong);
            long uniqueId = 23;
            uniqueId = uniqueId * 37 + BitConverter.ToInt64(randLong, 0);
            uniqueId = uniqueId * 37 + DateTime.Now.GetHashCode();

            //this is a much more elegant and lightweight method
            var newData = new byte[sizeof(long) * 2 + data.Length];
            BitConverter.GetBytes(uniqueId).CopyTo(newData, 0);
            BitConverter.GetBytes((long)messageType).CopyTo(newData, sizeof(long));
            data.CopyTo(newData, sizeof(long) * 2);

            //Wrapper.GameAction( ( ) =>
            MySandboxGame.Static.Invoke(() => { MyAPIGateway.Multiplayer.SendMessageTo(9000, newData, steamId); });
        }

        public static void BroadcastDataMessage(DataMessageType messageType, byte[] data)
        {
            /*
            long msgId = (long)messageType;

            string msgIdString = msgId.ToString( );
            byte[ ] newData = new byte[data.Length + msgIdString.Length + 1];
            newData[0] = (byte)msgIdString.Length;
            for ( int r = 0; r < msgIdString.Length; r++ )
                newData[r + 1] = (byte)msgIdString[r];

            Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );
            */
            var randLong = new byte[sizeof(long)];
            _random.NextBytes(randLong);
            long uniqueId = 23;
            uniqueId = uniqueId * 37 + BitConverter.ToInt64(randLong, 0);
            uniqueId = uniqueId * 37 + DateTime.Now.GetHashCode();

            var newData = new byte[sizeof(long) * 2 + data.Length];
            BitConverter.GetBytes(uniqueId).CopyTo(newData, 0);
            BitConverter.GetBytes((long)messageType).CopyTo(newData, sizeof(long));
            data.CopyTo(newData, sizeof(long) * 2);

            //Wrapper.GameAction( ( ) =>
            MySandboxGame.Static.Invoke(() => { MyAPIGateway.Multiplayer.SendMessageToOthers(9000, newData); });
        }

        public class ServerMessageItem
        {
            public string From { get; set; }

            public string Message { get; set; }
        }

        public class ServerDialogItem
        {
            public string title { get; set; }

            public string header { get; set; }

            public string content { get; set; }

            public string buttonText { get; set; }
        }

        public class ServerNotificationItem
        {
            public MyFontEnum color { get; set; }

            public int time { get; set; }

            public string message { get; set; }
        }
    }
}