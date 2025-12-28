using BPSR_DeepsLib;
using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.Chat;
using System.Collections.Concurrent;
using static Zproto.ChitChatNtf.Types;

namespace BPSR_ZDPS.Managers
{
    public class ChatManager
    {
        public static ConcurrentDictionary<long, ChatMessage> Messages = [];
        public static ConcurrentDictionary<long, User> Senders = [];
        //public List<ChatTab> ChatTabs = [];

        public static event Action<User, ChatMessage, NotifyNewestChitChatMsgs> OnChatMessage;

        private static ConcurrentQueue<long> ChatMessageIds = new();

        public ChatManager()
        {
            //LoadChatTabs();
            //ApplyNewSettings(Settings.Instance.Chat);
        }

        public static void ProcessChatMessage(ReadOnlySpan<byte> span, ExtraPacketData extraData)
        {
            if (!AppState.IsChatEnabled)
            {
                return;
            }

            var msg = NotifyNewestChitChatMsgs.Parser.ParseFrom(span);
            if (msg != null)
            {
                var chatMsg = new ChatMessage(msg.VRequest.ChatMsg.MsgInfo, msg.VRequest.ChannelType, msg.VRequest.ChatMsg.SendCharInfo.CharId, msg.VRequest.ChatMsg.Timestamp);
                User chatUser;

                if (Senders.TryGetValue(msg.VRequest.ChatMsg.SendCharInfo.CharId, out var sender))
                {
                    sender.Info = msg.VRequest.ChatMsg.SendCharInfo;
                    sender.NumSentMessages++;
                    chatUser = sender;
                }
                else
                {
                    var chatSender = new User(msg.VRequest.ChatMsg.SendCharInfo);
                    Senders.TryAdd(msg.VRequest.ChatMsg.SendCharInfo.CharId, chatSender);
                    chatUser = chatSender;
                }

                if (Messages.TryAdd(msg.VRequest.ChatMsg.MsgId, chatMsg))
                {
                    ChatMessageIds.Enqueue(msg.VRequest.ChatMsg.MsgId);
                    RemoveMessagesOverCap(Settings.Instance.Chat.MaxChatHistory);
                }

                OnChatMessage?.Invoke(chatUser, chatMsg, msg);
            }
        }

        public static void RemoveMessagesOverCap(int cap)
        {
            while (ChatMessageIds.Count() > cap)
            {
                if (ChatMessageIds.TryDequeue(out var msgId))
                {
                    Messages.Remove(msgId, out var _);
                }
            }
        }
    }
}
