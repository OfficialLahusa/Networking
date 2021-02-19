using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public enum PacketID : short
    {
        Invalid = -1,
        Ping = 0,
        Ping_Response = 1,
        Server_Info_Request = 2,
        Server_Info = 3,
        Player_Join = 4,
        Player_Join_Response = 5,
        Player_Move = 6,
        Player_Leave = 7,
        Status = 8,
    }
}