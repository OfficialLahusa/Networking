using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML_Engine;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Net.Sockets;
using System.Net;
using LahusaPackets;
using System.Diagnostics;

namespace Client.States
{
    public class GameState : State
    {
        PlayerEntity player;
        UdpClient client;
        IPEndPoint server;
        long serverTimestampOffset = 0;

        // Player Rotation (Deg)
        private float playerRotation = 0.0f;

        public GameState(Game game) : base(game)
        {
            Console.WriteLine("Networking Client - (C) Lasse Huber-Saffer, " + DateTime.UtcNow.Year);
            client = new UdpClient();

            Console.WriteLine($"Connecting to {Config.data.hostname}:{Config.data.port}");

            // Determine IP
            IPAddress address;
            if (!IPAddress.TryParse(Config.data.hostname, out address))
            {
                IPAddress[] registeredAddresses = Dns.GetHostEntry(Config.data.hostname).AddressList;
                address = registeredAddresses[0];
                foreach (IPAddress addr in registeredAddresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = addr;
                        break;
                    }
                }
            }
           
            server = new IPEndPoint(address, Config.data.port);

            #region Manual hostname & port input in console (disabled)
            // Manually connect
            /*
            else
            {
                // Address and port of the server
                IPAddress address = IPAddress.None;
                short port = -1;

                // Read and validate hostname/IPv4 address input
                do
                {
                    Console.Write("Enter IPv4 Address or hostname\n>");
                    string hostname = Console.ReadLine();

                    IPHostEntry hostEntry = null;
                    try
                    {
                        hostEntry = Dns.GetHostEntry(hostname);
                    } catch (SocketException e)
                    {
                        Console.WriteLine("Address not found");
                        continue;
                    } catch (Exception e)
                    {
                        Console.WriteLine("Invalid input");
                        continue;
                    }

                    IPAddress[] registeredAddresses = hostEntry.AddressList;

                    // Check if there are any registered IPv4 addresses for the hostname
                    if (registeredAddresses.Length > 0)
                    {
                        foreach (IPAddress addr in registeredAddresses)
                        {
                            if (addr.AddressFamily == AddressFamily.InterNetwork)
                            {
                                address = addr;
                                break;
                            }
                        }
                    }
                } while (address == IPAddress.None);

                // Read and validate port input
                do
                {
                    Console.Write("Enter port [0-65535]\n>");
                    if (!Int16.TryParse(Console.ReadLine(), out port))
                    {
                        Console.WriteLine("Invalid port format");
                        port = -1;
                    }
                } while (port < 0);

                Console.WriteLine($"Connecting to {address}:{port}");

                server = new IPEndPoint(address, port);
            }*/
            #endregion

            // Connect to server
            client.Connect(server);

            #region Ping
            // Get ping
            var pingResult = GetPing();
            Console.WriteLine($"Ping: {pingResult.ping}ms, ServerTimestampOffset: {pingResult.timestampOffset}");
            serverTimestampOffset = pingResult.timestampOffset;
            #endregion

            #region ServerInfo
            // Send server info request packet
            Packet serverInfoRequestPacket = new Packet();
            serverInfoRequestPacket.Append((short)Server.PacketID.Server_Info_Request);
            client.Send(serverInfoRequestPacket.GetData(), serverInfoRequestPacket.GetSize());
            Console.WriteLine($"Sent server info request packet with ID {(short)Server.PacketID.Server_Info_Request} of size {serverInfoRequestPacket.GetSize()} to {server.Address}:{server.Port}");
            
            // Receive server info packet
            short serverInfoPacketID = -1;
            Packet serverInfoPacket;
            IPEndPoint serverInfoEndpoint = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                serverInfoEndpoint = new IPEndPoint(IPAddress.Any, 0);
                serverInfoPacket = new Packet(client.Receive(ref serverInfoEndpoint));
                serverInfoPacketID = -1;
                serverInfoPacket.Read(ref serverInfoPacketID);
            } while (serverInfoPacketID != (short)Server.PacketID.Server_Info);

            string serverName = String.Empty;
            short tickrate = -1;
            int playerCount = -1, slotCount = -1;
            bool isFull = false;

            serverInfoPacket.Read(ref serverName).Read(ref tickrate).Read(ref playerCount).Read(ref slotCount).Read(ref isFull);
            Console.WriteLine($"Received server info packet with ID {serverInfoPacketID} of size {serverInfoPacket.GetSize()} from {serverInfoEndpoint.Address}:{serverInfoEndpoint.Port}");
            Console.WriteLine($"Server Name: \"{serverName}\", tickrate: {tickrate}, slots: ({playerCount}/{slotCount}), full: {isFull}");
            #endregion

            #region Joining
            // Send join packet
            Packet joinPacket = new Packet();
            joinPacket.Append((short)Server.PacketID.Player_Join).Append(Config.data.name).Append(Config.data.playerHue).Append(Config.data.nametagHue);
            client.Send(joinPacket.GetData(), joinPacket.GetSize());
            Console.WriteLine($"Sent player join packet with ID {(short)Server.PacketID.Player_Join} of size {joinPacket.GetSize()} to {server.Address}:{server.Port}");

            // Receive join response packet
            short joinResponsePacketID = -1;
            Packet joinResponsePacket;
            IPEndPoint joinResponseEndpoint = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                joinResponseEndpoint = new IPEndPoint(IPAddress.Any, 0);
                joinResponsePacket = new Packet(client.Receive(ref joinResponseEndpoint));
                joinResponsePacketID = -1;
                joinResponsePacket.Read(ref joinResponsePacketID).ResetReadPos();
            } while (joinResponsePacketID != (short)Server.PacketID.Player_Join_Response);

            bool didServerAcceptJoin = false;
            joinResponsePacket.Read(ref joinResponsePacketID).Read(ref didServerAcceptJoin);

            if(!didServerAcceptJoin)
            {
                Console.WriteLine($"Received join response packet (accepted: {didServerAcceptJoin}) with ID {joinResponsePacketID} of size {joinResponsePacket.GetSize()} from {joinResponseEndpoint.Address}:{joinResponseEndpoint.Port}");
                Console.WriteLine("Join packet rejected by server");
                Console.ReadLine();
                return;
            }

            float x = 0, y = 0;
            joinResponsePacket.Read(ref x).Read(ref y);
            Console.WriteLine($"Received join response packet (accepted: {didServerAcceptJoin}) with ID {joinResponsePacketID} of size {joinResponsePacket.GetSize()} from {joinResponseEndpoint.Address}:{joinResponseEndpoint.Port}");
            Console.WriteLine($"Position: (x: {x}|y: {y})");
            #endregion

            // Initialize PlayerEntity locally
            player = new PlayerEntity(Config.data.name, new Vector2f(x, y), Config.data.playerHue, Config.data.nametagHue);
        }

        public override bool IsOpaque
        {
            get
            {
                return true;
            }
        }

        public override void Draw(float deltaTime)
        {
            game.window.Clear(new Color(50, 200, 65));

            game.window.Draw(player);
        }

        public override void HandleInput(float deltaTime)
        {
            if (game.window.HasFocus() && game.cd == 0)
            {
                if(Keyboard.IsKeyPressed(Keyboard.Key.Escape))
                {
                    game.stateMachine.RemoveCurrent();
                }

                Vector2f moveVector = new Vector2f(0, 0);

                if (Keyboard.IsKeyPressed(Keyboard.Key.W))
                {
                    moveVector.Y -= 1;
                }
                if (Keyboard.IsKeyPressed(Keyboard.Key.S))
                {
                    moveVector.Y += 1;
                }
                if (Keyboard.IsKeyPressed(Keyboard.Key.A))
                {
                    moveVector.X -= 1;
                }
                if (Keyboard.IsKeyPressed(Keyboard.Key.D))
                {
                    moveVector.X += 1;
                }

                float length = (float)Math.Sqrt(moveVector.X * moveVector.X + moveVector.Y * moveVector.Y);
                if(length != 0)
                {
                    moveVector /= length;
                }
                moveVector *= deltaTime;
                player.Move(moveVector);
            }

            Vector2f mousePos = new Vector2f(Mouse.GetPosition(game.window).X, Mouse.GetPosition(game.window).Y);
            playerRotation = (float)Math.Atan2(mousePos.Y - player.Position.Y, mousePos.X - player.Position.X) / (float)Math.PI * 180.0f + 180.0f;

        }

        public override void Update(float deltaTime)
        {
            Console.Write($"\rFPS: {Math.Round(1.0/deltaTime)}, Rotation: {Math.Round(playerRotation, 2)}°                        ");

            player.UpdateRotation(playerRotation);
            player.UpdatePosition(player.Position);
        }

        private (int ping, int timestampOffset) GetPing()
        {
            if(!client.Client.Connected)
            {
                throw new Exception("Tried to get ping on an unconnected socket");
            }

            //Ping measurement
            Stopwatch pingTimer = new Stopwatch();
            IPEndPoint endpointOfPingResponse = new IPEndPoint(IPAddress.Any, 0);
            byte[] rawPingResponsePacket = null;

            // Send ping packet to server
            Packet pingPacket = new Packet();
            pingPacket.Append((short)Server.PacketID.Ping);

            pingTimer.Start();
            client.Send(pingPacket.GetData(), pingPacket.GetSize());
            Console.WriteLine($"Sent ping packet with ID {(short)Server.PacketID.Ping} of size {pingPacket.GetSize()} to {server.Address}:{server.Port}");
            do
            {
                endpointOfPingResponse = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    rawPingResponsePacket = client.Receive(ref endpointOfPingResponse);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            } while (!endpointOfPingResponse.Address.Equals(server.Address) || endpointOfPingResponse.Port != server.Port);



            int ping = (int)pingTimer.ElapsedMilliseconds;
            pingTimer.Stop();

            // Receive ping response packet
            Packet pingResponsePacket = new Packet(rawPingResponsePacket);
            short pingResponsePacketID = -1;
            int timestampOffset = -1;
            pingResponsePacket.Read(ref pingResponsePacketID).Read(ref timestampOffset);
            Console.WriteLine($"Received ping response packet with ID {pingResponsePacketID} of size {pingResponsePacket.GetSize()} from {endpointOfPingResponse.Address}:{endpointOfPingResponse.Port}");

            return (ping, timestampOffset);
        }
    }
}
