using System.Net;

namespace LahusaPackets
{
    public class BufferedPacket
    {
        public Packet packet;
        public IPEndPoint endpoint;

        public BufferedPacket(Packet packet, IPEndPoint endpoint)
        {
            this.packet = packet;
            this.endpoint = endpoint;
        }
    }
}
