using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace LahusaPackets
{
    // PacketLogger class for any PacketID Enum
    public class PacketLogger<PacketIDType> where PacketIDType : struct, IConvertible
    {
        public FilterMode FilterMode = FilterMode.Blacklist;
        public int Filter = 0;

        public PacketLogger() {

        }

        [Conditional("DEBUG")]
        public void Log(Packet packet, IPEndPoint endpoint, PacketLogMode logType, PacketIDType packetType)
        {
            int id = (int)Convert.ChangeType(packetType, typeof(int));

            // Check whitelist condition
            if (FilterMode == FilterMode.Whitelist)
            {
                // Id is not contained in whitelist
                if ((Filter & id) != id)
                {
                    return;
                }
            } 
            // Check blacklist condition
            else if(FilterMode == FilterMode.Blacklist)
            {
                // Id is contained in blacklist
                if ((Filter & id) == id)
                {
                    return;
                }
            }

            string verb = string.Empty;

            if (logType == PacketLogMode.Neutral) verb = "Logged";
            else if (logType == PacketLogMode.Received) verb = "Received";
            else if (logType == PacketLogMode.Sent) verb = "Sent";

            Console.WriteLine($"{verb} {packetType} Packet ({packet.GetSize()} bytes) with id {packetType.ToInt32(null)} {((logType == PacketLogMode.Received) ? "from" : "to")} {endpoint.Address}:{endpoint.Port}");
        }
    }
}