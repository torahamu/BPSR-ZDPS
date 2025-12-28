using Zproto;

namespace BPSR_ZDPS.DataTypes.Chat
{
    public class User(BasicShowInfo info)
    {
        public BasicShowInfo Info = info;
        public int NumSentMessages = 0;
    }
}