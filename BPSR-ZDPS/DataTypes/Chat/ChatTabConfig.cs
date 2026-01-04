using Zproto;

namespace BPSR_ZDPS.DataTypes.Chat
{
    public class ChatTabConfig : ICloneable
    {
        public string Name = "";
        public long Id = DateTime.Now.Ticks;
        public List<ChitChatChannelType> Channels { get; set; } = [];
        public int OverLevel { get; set; } = 50;
        public string Contains { get; set; } = "";
        public string DoesNotContain { get; set; } = "";

        public object Clone()
        {
            return new ChatTabConfig
            {
                Name = Name,
                Id = Id,
                OverLevel = OverLevel,
                Contains = Contains,
                DoesNotContain = DoesNotContain,
                Channels = new(Channels)
            };
        }
    }
}