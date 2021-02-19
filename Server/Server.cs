using LahusaPackets;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        public void Run()
        {
            Console.WriteLine("Networking Server - (C) Lasse Huber-Saffer, " + DateTime.UtcNow.Year);
            Console.CursorVisible = false;

            Console.WriteLine($"Starting as \"{Config.data.name}\" on port {Config.data.port} with tickrate {Config.data.tickrate}");
            Serialiser.SaveConfigFile(Config.data);

            double tickInterval = 1.0f / Config.data.tickrate;

            UdpClient server = new UdpClient(Config.data.port);
            Queue<BufferedPacket> packetBuffer = new Queue<BufferedPacket>();

            Dictionary<IPEndPoint, PlayerEntity> players = new Dictionary<IPEndPoint, PlayerEntity>();

            Stopwatch runtimeTimer = new Stopwatch();
            Stopwatch lastTickTimer = new Stopwatch();

            runtimeTimer.Start();
            lastTickTimer.Start();

            bool running = true;
            while (running)
            {
                // Receive incoming packets if there are any
                if (server.Available > 0)
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] rawPacket = server.Receive(ref endpoint);
                    Packet receivedPacket = new Packet(rawPacket);

                    short packetID = (short)PacketID.Invalid;
                    receivedPacket.Read(ref packetID).ResetReadPos();

                    // Immediate packet handling
                    switch (packetID)
                    {
                        // Add normal packets to the packet buffer for handling in a server tick
                        default:
                            packetBuffer.Enqueue(new BufferedPacket(receivedPacket, endpoint));
                            break;

                        // Respond to ping packets immediately outside of a normal tick timeframe
                        case (short)PacketID.Ping:
                            Console.WriteLine($"Received ping packet with ID {packetID} of size {receivedPacket.GetSize()} from {endpoint.Address}:{endpoint.Port}");
                            Packet pingResponsePacket = new Packet();
                            pingResponsePacket.Append((short)PacketID.Ping_Response).Append(runtimeTimer.ElapsedMilliseconds);
                            Console.WriteLine("ElapsedMilliseconds: " + runtimeTimer.ElapsedMilliseconds);

                            server.Send(pingResponsePacket.GetData(), pingResponsePacket.GetSize(), endpoint);
                            Console.WriteLine($"Sent ping response packet with ID {(short)PacketID.Ping_Response} of size {pingResponsePacket.GetSize()} to {endpoint.Address}:{endpoint.Port}");
                            break;
                    }
                }

                // Check if a server tick should occur
                if (lastTickTimer.Elapsed.TotalSeconds >= tickInterval)
                {
                    lastTickTimer.Restart();

                    while (packetBuffer.Count > 0)
                    {
                        BufferedPacket bufferedPacket = packetBuffer.Dequeue();

                        short packetID = (short)PacketID.Invalid;
                        bufferedPacket.packet.Read(ref packetID).ResetReadPos();

                        // Handle Tick-Packets
                        switch (packetID)
                        {
                            // Unhandled Packet
                            default:
                                Console.WriteLine($"Received unhandled packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                                break;
                            // Received server info request packet
                            case (short)PacketID.Server_Info_Request:
                                // Send server info packet
                                Console.WriteLine($"Received server info request packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                                Packet serverInfoPacket = new Packet();
                                serverInfoPacket.Append((short)PacketID.Server_Info).Append(Config.data.name).Append(Config.data.tickrate).Append(players.Count).Append(Config.data.slotCount).Append(players.Count >= Config.data.slotCount);
                                server.Send(serverInfoPacket.GetData(), serverInfoPacket.GetSize(), bufferedPacket.endpoint);
                                Console.WriteLine($"Sent server info packet with ID {(short)PacketID.Server_Info} of size {serverInfoPacket.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                                break;
                            // Received player join packet
                            case (short)PacketID.Player_Join:
                                // Read player info
                                string name = string.Empty;
                                int playerHue = 0;
                                int nametagHue = 0;
                                bufferedPacket.packet.Read(ref packetID).Read(ref name).Read(ref playerHue).Read(ref nametagHue);

                                Console.WriteLine($"Received player join packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");

                                if (players.Count + 1 > Config.data.slotCount)
                                {
                                    // Send player join response packet (rejected)
                                    Packet joinResponsePacket = new Packet();
                                    joinResponsePacket.Append((short)PacketID.Player_Join_Response).Append(false);
                                    server.Send(joinResponsePacket.GetData(), joinResponsePacket.GetSize(), bufferedPacket.endpoint);
                                    Console.WriteLine($"Sent player join response packet (accepted: {false}) with ID {(short)PacketID.Player_Join_Response} of size {joinResponsePacket.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                                } else
                                {
                                    // Add player entity to dictionary
                                    Vector2f position = new Vector2f(400, 400);
                                    players.Add(bufferedPacket.endpoint, new PlayerEntity(name, position, playerHue, nametagHue));

                                    // Send player join response packet (accepted)
                                    Packet joinResponsePacket = new Packet();
                                    joinResponsePacket.Append((short)PacketID.Player_Join_Response).Append(true).Append(position.X).Append(position.Y);
                                    server.Send(joinResponsePacket.GetData(), joinResponsePacket.GetSize(), bufferedPacket.endpoint);
                                    Console.WriteLine($"Sent player join response packet (accepted: {true}) with ID {(short)PacketID.Player_Join_Response} of size {joinResponsePacket.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                                }
                                break;
                        }

                    }
                }
            }


            Console.ReadKey();
        }
    }
}
