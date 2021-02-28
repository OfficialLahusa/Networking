using LahusaPackets;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class PacketHandler
    {
        public Queue<BufferedPacket> packetBuffer;
        private UdpClient server;
        public bool StatusUpdateNeeded = false;

        public PacketHandler(UdpClient server)
        {
            this.server = server;
            packetBuffer = new Queue<BufferedPacket>();
        }

        public void HandleImmediatePackets(Dictionary<Guid, PlayerEntity> players, Stopwatch runtimeTimer)
        {
            while (server.Available > 0)
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
                        //Console.WriteLine($"Received ping packet with ID {packetID} of size {receivedPacket.GetSize()} from {endpoint.Address}:{endpoint.Port}");
                        Packet pingResponsePacket = new Packet();
                        pingResponsePacket.Append((short)PacketID.Ping_Response).Append(runtimeTimer.ElapsedMilliseconds);
                        //Console.WriteLine("ElapsedMilliseconds: " + runtimeTimer.ElapsedMilliseconds);

                        server.Send(pingResponsePacket.GetData(), pingResponsePacket.GetSize(), endpoint);
                        //Console.WriteLine($"Sent ping response packet with ID {(short)PacketID.Ping_Response} of size {pingResponsePacket.GetSize()} to {endpoint.Address}:{endpoint.Port}");
                        break;
                }
            }
        }

        public void HandleTickPackets(Dictionary<Guid, PlayerEntity> players)
        {
            while (packetBuffer.Count > 0)
            {
                BufferedPacket bufferedPacket = packetBuffer.Dequeue();

                short packetID = (short)PacketID.Invalid;
                bufferedPacket.packet.ResetReadPos().Read(ref packetID).ResetReadPos();

                switch (packetID)
                {
                    // Unhandled Packet
                    default:
                        Console.WriteLine($"Received unhandled packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                        break;
                    // Received server info request packet
                    case (short)PacketID.Server_Info_Request:
                        // Send server info packet
                        //Console.WriteLine($"Received server info request packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                        Packet serverInfoPacket = new Packet();
                        serverInfoPacket.Append((short)PacketID.Server_Info).Append(Config.data.name).Append(Config.data.tickrate).Append(players.Count).Append(Config.data.slotCount).Append(players.Count >= Config.data.slotCount);
                        server.Send(serverInfoPacket.GetData(), serverInfoPacket.GetSize(), bufferedPacket.endpoint);
                        //Console.WriteLine($"Sent server info packet with ID {(short)PacketID.Server_Info} of size {serverInfoPacket.GetSize()} to {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                        break;
                    // Received player join packet
                    case (short)PacketID.Player_Join:
                        // Read player info
                        string name = string.Empty;
                        int playerHue = 0;
                        int nametagHue = 0;
                        bufferedPacket.packet.Read(ref packetID).Read(ref name).Read(ref playerHue).Read(ref nametagHue);

                        Console.WriteLine($"Received player join packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                        Console.WriteLine($"Player \"{name}\" joined with hue {playerHue} and nametag hue {nametagHue}");

                        if (players.Count + 1 > Config.data.slotCount)
                        {
                            // Send player join response packet (rejected)
                            Packet joinResponsePacket = new Packet();
                            joinResponsePacket.Append((short)PacketID.Player_Join_Response).Append(false);
                            server.Send(joinResponsePacket.GetData(), joinResponsePacket.GetSize(), bufferedPacket.endpoint);
                            Console.WriteLine($"Sent player join response packet (accepted: {false}) with ID {(short)PacketID.Player_Join_Response} of size {joinResponsePacket.GetSize()} to {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                        }
                        else
                        {
                            // Create player Guid and token
                            Random random = new Random();
                            Guid playerGuid = Guid.NewGuid();
                            Guid playerToken = Guid.NewGuid();

                            // Create player join response packet (accepted)
                            Packet joinResponsePacket = new Packet();
                            Vector2f position = new Vector2f(random.Next(100, 701), random.Next(100, 701));
                            joinResponsePacket.Append((short)PacketID.Player_Join_Response).Append(true).Append(playerGuid).Append(playerToken).Append(position.X).Append(position.Y);

                            // Add all player entities' states to join response packet
                            joinResponsePacket.Append(players.Count);
                            foreach (var player in players)
                            {
                                joinResponsePacket.Append(player.Key).Append(player.Value.name).Append(player.Value.playerHue).Append(player.Value.nametagHue).Append(player.Value.position.X).Append(player.Value.position.Y).Append(player.Value.rotation);
                            }

                            // Send join notification packet to every player
                            Packet joinNotificationPacket = new Packet();
                            joinNotificationPacket.Append((short)PacketID.Player_Join_Notification).Append(playerGuid).Append(name).Append(playerHue).Append(nametagHue).Append(position.X).Append(position.Y);
                            foreach (var player in players)
                            {
                                server.Send(joinNotificationPacket.GetData(), joinNotificationPacket.GetSize(), player.Value.endpoint);
                                //Console.WriteLine($"Sent player join notification packet with ID {(short)PacketID.Player_Join_Notification} of size {joinNotificationPacket.GetSize()} to {player.Value.endpoint.Address}:{player.Value.endpoint.Port}");
                            }

                            // Send player join response packet (accepted)
                            server.Send(joinResponsePacket.GetData(), joinResponsePacket.GetSize(), bufferedPacket.endpoint);
                            Console.WriteLine($"Sent player join response packet (accepted: {true}) with ID {(short)PacketID.Player_Join_Response} of size {joinResponsePacket.GetSize()} to {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");

                            // Add player entity to dictionary
                            players.Add(playerGuid, new PlayerEntity(bufferedPacket.endpoint, playerToken, name, position, null, playerHue, nametagHue));
                            Console.WriteLine($"Player {playerGuid} has been assigned the token {playerToken}, PlayerCount: {players.Count}");
                        }
                        break;
                    case (short)PacketID.Player_Move:
                        Guid movePlayerGuid = Guid.Empty;
                        Guid movePlayerToken = Guid.Empty;
                        float dx = 0, dy = 0;

                        bufferedPacket.packet.Read(ref packetID).Read(ref movePlayerGuid).Read(ref movePlayerToken).Read(ref dx).Read(ref dy);
                        //Console.WriteLine($"Received player move packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");

                        if (players.ContainsKey(movePlayerGuid))
                        {
                            if (players[movePlayerGuid].token == movePlayerToken)
                            {
                                players[movePlayerGuid].position += new Vector2f(dx, dy);
                                StatusUpdateNeeded = true;
                            }
                            else
                            {
                                Console.WriteLine($"Given player token does not match: {movePlayerToken} != {players[movePlayerGuid].token}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Player guid not in player list: {movePlayerGuid}");
                        }
                        break;
                    case (short)PacketID.Player_Rotate:
                        Guid rotatePlayerGuid = Guid.Empty;
                        Guid rotatePlayerToken = Guid.Empty;
                        float rot = 0;

                        bufferedPacket.packet.Read(ref packetID).Read(ref rotatePlayerGuid).Read(ref rotatePlayerToken).Read(ref rot);
                        //Console.WriteLine($"Received player rotation packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");

                        if (players.ContainsKey(rotatePlayerGuid))
                        {
                            if (players[rotatePlayerGuid].token == rotatePlayerToken)
                            {
                                players[rotatePlayerGuid].rotation = rot;
                                StatusUpdateNeeded = true;
                            }
                        }
                        break;
                    case (short)PacketID.Player_Leave:
                        Console.WriteLine($"Received player leave packet with ID {packetID} of size {bufferedPacket.packet.GetSize()} from {bufferedPacket.endpoint.Address}:{bufferedPacket.endpoint.Port}");
                        Guid leavingPlayerGuid = Guid.Empty;
                        Guid leavingPlayerToken = Guid.Empty;

                        bufferedPacket.packet.Read(ref packetID).Read(ref leavingPlayerGuid).Read(ref leavingPlayerToken);

                        if (players.ContainsKey(leavingPlayerGuid))
                        {
                            if (players[leavingPlayerGuid].token == leavingPlayerToken)
                            {
                                players.Remove(leavingPlayerGuid);

                                Packet playerLeaveNotificationPacket = new Packet();
                                playerLeaveNotificationPacket.Append((short)PacketID.Player_Leave_Notification).Append(leavingPlayerGuid);

                                // Send player leave notification to all players
                                foreach (var player in players)
                                {
                                    server.Send(playerLeaveNotificationPacket.GetData(), playerLeaveNotificationPacket.GetSize(), player.Value.endpoint);
                                    //Console.WriteLine($"Sent player leave notification packet with ID {(short)PacketID.Player_Leave_Notification} of size {playerLeaveNotificationPacket.GetSize()} to {player.Value.endpoint.Address}:{player.Value.endpoint.Port}");
                                }

                                Console.WriteLine($"Player {leavingPlayerGuid} left, PlayerCount: {players.Count}");
                            }
                        }
                        break;
                }
            }
        }

        public void SendStatusUpdate(Dictionary<Guid, PlayerEntity> players)
        {
            if (StatusUpdateNeeded)
            {
                Packet statusPacket = new Packet();
                statusPacket.Append((short)PacketID.Status).Append(players.Count);
                foreach (var player in players)
                {
                    statusPacket.Append(player.Key).Append(player.Value.position.X).Append(player.Value.position.Y).Append(player.Value.rotation);
                }

                foreach (var player in players)
                {
                    server.Send(statusPacket.GetData(), statusPacket.GetSize(), player.Value.endpoint);
                }

                StatusUpdateNeeded = false;
            }
        }
    }
}
