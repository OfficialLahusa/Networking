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
        public PacketDirection PacketDirectionFilter = PacketDirection.Neutral;
        public int? Filter = null;

        public PacketLogger() {

        }

        [Conditional("DEBUG")]
        public void Log(Packet packet, IPEndPoint endpoint, PacketDirection direction, PacketIDType packetType)
        {
            int id = (int)Convert.ChangeType(packetType, typeof(int));

            // Check whitelist condition
            if (FilterMode == FilterMode.Whitelist)
            {
                // If the filter is null, discard all packets
                if(Filter == null)
                {
                    return;
                }

                // Id is not contained in whitelist
                if ((Filter & id) != id)
                {
                    return;
                }
            } 
            // Check blacklist condition
            else if(FilterMode == FilterMode.Blacklist)
            {
                // Check condition only if a filter is set
                if(Filter != null)
                {
                    // Id is contained in blacklist
                    if ((Filter & id) == id)
                    {
                        return;
                    }
                }
            }

            // Check if direction filter is specified
            if(PacketDirectionFilter != PacketDirection.Neutral)
            {
                // Reject packets from other directions
                if(direction != PacketDirectionFilter)
                {
                    return;
                }
            }

            string verb = string.Empty;

            if (direction == PacketDirection.Neutral) verb = "Logged";
            else if (direction == PacketDirection.Received) verb = "Received";
            else if (direction == PacketDirection.Sent) verb = "Sent";

            Console.WriteLine($"{verb} {packetType} Packet ({packet.GetSize()} bytes) with id {packetType.ToInt32(null)} {((direction == PacketDirection.Received) ? "from" : "to")} {endpoint.Address}:{endpoint.Port}");
        }
    }
}