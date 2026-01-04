namespace BPSR_ZDPS.DataTypes.Chat
{
    public class ChatTab(ChatTabConfig config)
    {
        public ChatTabConfig Config = config;
        public List<long> MessageIds = [];
    }
}