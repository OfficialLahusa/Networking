using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahusaPackets
{
    public enum PacketLogMode
    {
        Neutral = 0,
        Sent = 1,
        Received = 1 << 1
    }
}
