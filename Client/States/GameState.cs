﻿using System;
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
using System.Threading;
using MapToolkit;

namespace Client.States
{
    public class GameState : State
    {
        // Rendering
        View view;
        float zoomFac = 1.0f;
        PlayerEntity localPlayer;
        Dictionary<Guid, PlayerEntity> players;

        // Networking
        UdpClient client;
        IPEndPoint server;
        PacketLogger<Server.PacketID> packetLogger;
        long serverTimestampOffset = 0;
        bool hasSentMovementStopPacket = false;

        // Auth
        Guid localPlayerGuid = Guid.Empty;
        Guid localPlayerToken = Guid.Empty;
        
        // Other
        VectorMap map;

        // Player Rotation (Deg)
        private float playerRotation = 0.0f;

        public GameState(Game game) : base(game)
        {
            view = new View((Vector2f)game.window.Size / 2, (Vector2f)game.window.Size);
            packetLogger = new PacketLogger<Server.PacketID>
            {
                FilterMode = FilterMode.Whitelist,
                Filter = null,
                PacketDirectionFilter = PacketDirection.Neutral
            };

            #region Resources
            // Load Font
            if (!game.fonts.ContainsKey("montserrat"))
            {
                game.fonts.Add("montserrat", new Font("res/Montserrat-Bold.ttf"));
            }

            // Load map
            SvgMapLoader mapLoader = new SvgMapLoader();
            map = mapLoader.LoadMap("res/map/de_dust2.svg", game.fonts["montserrat"]);
            #endregion

            Console.WriteLine("Networking Client - (C) Lasse Huber-Saffer, " + DateTime.UtcNow.Year);
            client = new UdpClient();

            Console.WriteLine($"Connecting to {Config.data.hostname}:{Config.data.port}");

            // Determine IP
            if (!IPAddress.TryParse(Config.data.hostname, out IPAddress address))
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
            try
            {
                var (ping, timestampOffset) = GetPing();
                Console.WriteLine($"Ping: {ping}ms, ServerTimestampOffset: {timestampOffset}");
                serverTimestampOffset = timestampOffset;
            } catch (SocketException)
            {
                return;
            }

            #endregion

            #region ServerInfo
            // Send server info request packet
            Packet serverInfoRequestPacket = new Packet();
            serverInfoRequestPacket.Append((short)Server.PacketID.Server_Info_Request);
            client.Send(serverInfoRequestPacket.GetData(), serverInfoRequestPacket.GetSize());
            packetLogger.Log(serverInfoRequestPacket, server, PacketDirection.Sent, Server.PacketID.Server_Info_Request);
            
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
            packetLogger.Log(serverInfoPacket, serverInfoEndpoint, PacketDirection.Received, (Server.PacketID)serverInfoPacketID);
            Console.WriteLine($"Server Name: \"{serverName}\", tickrate: {tickrate}, slots: ({playerCount}/{slotCount}), full: {isFull}");
            #endregion
            
            #region Joining
            // Send join packet
            Packet joinPacket = new Packet();
            joinPacket.Append((short)Server.PacketID.Player_Join).Append(Config.data.name).Append(Config.data.playerHue).Append(Config.data.nametagHue);
            client.Send(joinPacket.GetData(), joinPacket.GetSize());
            packetLogger.Log(joinPacket, server, PacketDirection.Sent, Server.PacketID.Player_Join);

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
                packetLogger.Log(joinResponsePacket, joinResponseEndpoint, PacketDirection.Received, (Server.PacketID)joinResponsePacketID);
                Console.WriteLine("Join packet rejected by server");
                Console.ReadLine();
                return;
            }

            float x = 0, y = 0;
            int joinPlayerCount = -1;
            joinResponsePacket.Read(ref localPlayerGuid).Read(ref localPlayerToken).Read(ref x).Read(ref y).Read(ref joinPlayerCount);

            players = new Dictionary<Guid, PlayerEntity>();

            for(int i = 0; i < joinPlayerCount; i++)
            {
                Guid entityGuid = Guid.Empty;
                string entityName = string.Empty;
                int entityHue = -1, entityNametagHue = -1;
                float entityPosX = 400, entityPosY = 400, entityRotation = 0;
                joinResponsePacket.Read(ref entityGuid).Read(ref entityName).Read(ref entityHue).Read(ref entityNametagHue).Read(ref entityPosX).Read(ref entityPosY).Read(ref entityRotation);

                players.Add(entityGuid, new PlayerEntity(entityName, game.fonts["montserrat"], new Vector2f(entityPosX, entityPosY), entityHue, entityNametagHue));
                players[entityGuid].UpdateRotation(entityRotation);
            }

            packetLogger.Log(joinResponsePacket, joinResponseEndpoint, PacketDirection.Received, (Server.PacketID)joinResponsePacketID);
            Console.WriteLine($"Guid: {localPlayerGuid}, token: {localPlayerToken}, Position: (x: {x}|y: {y}), Initialized {players.Count} preexisting PlayerEntities");
            #endregion

            // Initialize PlayerEntity locally
            localPlayer = new PlayerEntity(Config.data.name, game.fonts["montserrat"], new Vector2f(x, y), Config.data.playerHue, Config.data.nametagHue);

            #region EventHandler registration
            game.window.Closed += OnWindowClosed;
            game.window.Resized += OnWindowResized;
            game.window.MouseWheelScrolled += OnMouseWheelScrolled;
            #endregion
        }

        #region Window event handling
        private void OnWindowResized(object sender, SizeEventArgs e)
        {
            if (!game.stateMachine.IsCurrent(this))
            {
                return;
            }
            else
            {

                view.Size = new Vector2f(e.Width, e.Height);
                return;
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if(!game.stateMachine.IsCurrent(this))
            {
                return;
            } else
            {

                Leave();

                game.window.Close();
                return;
            }
        }

        private void OnMouseWheelScrolled(object sender, MouseWheelScrollEventArgs e)
        {
            zoomFac += 0.1f * (-e.Delta);
        }
        #endregion

        public override bool IsOpaque
        {
            get
            {
                return true;
            }
        }

        public override void Draw(float deltaTime)
        {
            if (client.Client == null) return;
            if (!client.Client.Connected) return;

            // Clear to map background color if there is one, otherwise use default background color
            if(map.BackgroundColor != null)
            {
                game.window.Clear(map.BackgroundColor.Value);
            } else
            {
                game.window.Clear(new Color(50, 200, 65));
            }


            // Set view to player view
            game.window.SetView(view);

            // Draw map
            if(map != null)
            {
                game.window.Draw(map.Triangles);
                game.window.Draw(map.Lines);
                foreach(Text text in map.Text)
                {
                    game.window.Draw(text);
                }
            }

            // Draw all remote entities
            foreach (PlayerEntity player in players.Values)
            {
                game.window.Draw(player);
            }

            // Draw local player
            game.window.Draw(localPlayer);

            // Reset view to default view
            game.window.SetView(game.window.DefaultView);
        }

        public override void HandleInput(float deltaTime)
        {
            if (game.window.HasFocus())
            {
                if(Keyboard.IsKeyPressed(Keyboard.Key.Escape))
                {
                    Leave();
                    game.stateMachine.ReplaceCurrent(new LoginState(game));
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
                    hasSentMovementStopPacket = false;
                }
                //moveVector *= deltaTime;

                // Only send packets if the client is connected to a server
                if(client.Client != null)
                {
                    if(client.Client.Connected)
                    {
                        if (moveVector.X != 0 || moveVector.Y != 0 || !hasSentMovementStopPacket)
                        {
                            Packet movePacket = new Packet();
                            movePacket.Append((short)Server.PacketID.Player_Move).Append(localPlayerGuid).Append(localPlayerToken).Append(300 * moveVector.X).Append(300 * moveVector.Y);
                            client.Send(movePacket.GetData(), movePacket.GetSize());
                            packetLogger.Log(movePacket, server, PacketDirection.Sent, Server.PacketID.Player_Move);

                            if(moveVector.X == 0 && moveVector.Y == 0)
                            {
                                hasSentMovementStopPacket = true;
                            }
                        }
                        //localPlayer.Move(moveVector);

                        float oldRotation = playerRotation;
                        Vector2f mousePos = game.window.MapPixelToCoords(Mouse.GetPosition(game.window), view);
                        playerRotation = (float)Math.Atan2(localPlayer.Position.Y - mousePos.Y, localPlayer.Position.X - mousePos.X) / (float)Math.PI * 180.0f;

                        if (playerRotation != oldRotation)
                        {
                            Packet rotatePacket = new Packet();
                            rotatePacket.Append((short)Server.PacketID.Player_Rotate).Append(localPlayerGuid).Append(localPlayerToken).Append(playerRotation);
                            client.Send(rotatePacket.GetData(), rotatePacket.GetSize());
                            packetLogger.Log(rotatePacket, server, PacketDirection.Sent, Server.PacketID.Player_Rotate);
                        }
                    }
                }
            }
        }

        public override void BackgroundUpdate(float deltaTime)
        {
            return;
        }

        public override void Update(float deltaTime)
        {
            if (client.Client == null || (client.Client != null && !client.Client.Connected))
            {
                game.stateMachine.ReplaceCurrent(new LoginState(game));
                client.Close();
            }
            else
            {
                HandlePackets();
                view.Center += 0.9f * Math.Min(1.0f, 10 * deltaTime) * (localPlayer.Position - view.Center);
                view.Size = new Vector2f(game.window.Size.X * zoomFac, game.window.Size.Y * zoomFac);

                //Console.Write($"\rFPS: {Math.Round(1.0/deltaTime)}, Rotation: {Math.Round(playerRotation, 2)}°                        ");

                //localPlayer.UpdateRotation(playerRotation);
                //localPlayer.UpdatePosition(localPlayer.Position);
            }
        }

        private void HandlePackets()
        {
            if (client.Available > 0)
            {
                //Console.Write('\r');

                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] rawPacket = client.Receive(ref endpoint);
                Packet receivedPacket = new Packet(rawPacket);

                short packetID = (short)Server.PacketID.Invalid;
                receivedPacket.Read(ref packetID).ResetReadPos();

                // Immediate packet handling
                switch (packetID)
                {
                    // Unhandled packet
                    default:
                        Console.WriteLine($"Received unhandled packet with ID {packetID} of size {receivedPacket.GetSize()} from {endpoint.Address}:{endpoint.Port}");
                        break;
                    // Player join notification packet
                    case (short)Server.PacketID.Player_Join_Notification:
                        packetLogger.Log(receivedPacket, endpoint, PacketDirection.Received, (Server.PacketID)packetID);
                        Guid joinedPlayerGuid = Guid.Empty;
                        string joinedPlayerName = string.Empty;
                        int joinedPlayerHue = 0, joinedPlayerNametagHue = 0;
                        float joinedPlayerPosX = 0, joinedPlayerPosY = 0;
                        receivedPacket.Read(ref packetID).Read(ref joinedPlayerGuid).Read(ref joinedPlayerName).Read(ref joinedPlayerHue).Read(ref joinedPlayerNametagHue).Read(ref joinedPlayerPosX).Read(ref joinedPlayerPosY);

                        players.Add(joinedPlayerGuid, new PlayerEntity(joinedPlayerName, game.fonts["montserrat"], new Vector2f(joinedPlayerPosX, joinedPlayerPosY), joinedPlayerHue, joinedPlayerNametagHue));
                        break;
                    // Player leave notification packet
                    case (short)Server.PacketID.Player_Leave_Notification:
                        packetLogger.Log(receivedPacket, endpoint, PacketDirection.Received, (Server.PacketID)packetID);
                        Guid leavingPlayerGuid = Guid.Empty;
                        receivedPacket.Read(ref packetID).Read(ref leavingPlayerGuid);

                        if(leavingPlayerGuid == localPlayerGuid)
                        {
                            game.window.Close();
                        }

                        if(players.ContainsKey(leavingPlayerGuid))
                        {
                            players.Remove(leavingPlayerGuid);
                        }
                        break;
                    // Status packet
                    case (short)Server.PacketID.Status:
                        int playerCount = -1;
                        packetLogger.Log(receivedPacket, endpoint, PacketDirection.Received, (Server.PacketID)packetID);
                        receivedPacket.Read(ref packetID).Read(ref playerCount);

                        for(int i = 0; i < playerCount; i++)
                        {
                            Guid entityGuid = Guid.Empty;
                            float entityPosX = 0, entityPosY = 0, entityRotation = 0;
                            receivedPacket.Read(ref entityGuid).Read(ref entityPosX).Read(ref entityPosY).Read(ref entityRotation);

                            if(entityGuid == localPlayerGuid)
                            {
                                localPlayer.UpdatePosition(new Vector2f(entityPosX, entityPosY));
                                localPlayer.UpdateRotation(entityRotation);
                            } else
                            {
                                if(players.ContainsKey(entityGuid))
                                {
                                    players[entityGuid].UpdatePosition(new Vector2f(entityPosX, entityPosY));
                                    players[entityGuid].UpdateRotation(entityRotation);
                                }
                            }
                        }

                        break;
                }
            }
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
            byte[] rawPingResponsePacket;

            // Send ping packet to server
            Packet pingPacket = new Packet();
            pingPacket.Append((short)Server.PacketID.Ping);

            client.Send(pingPacket.GetData(), pingPacket.GetSize());
            packetLogger.Log(pingPacket, server, PacketDirection.Sent, Server.PacketID.Ping);
            pingTimer.Restart();
            do
            {
                endpointOfPingResponse = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    rawPingResponsePacket = client.Receive(ref endpointOfPingResponse);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Could not connect to server: " + e.Message);
                    game.stateMachine.ReplaceCurrent(new LoginState(game));
                    throw new SocketException(e.ErrorCode);
                }

            } while (!endpointOfPingResponse.Address.Equals(server.Address) || endpointOfPingResponse.Port != server.Port);

            pingTimer.Stop();
            int ping = (int)pingTimer.ElapsedMilliseconds;
            
            // Receive ping response packet
            Packet pingResponsePacket = new Packet(rawPingResponsePacket);
            short pingResponsePacketID = -1;
            int timestampOffset = -1;
            pingResponsePacket.Read(ref pingResponsePacketID).Read(ref timestampOffset);
            packetLogger.Log(pingResponsePacket, endpointOfPingResponse, PacketDirection.Received, (Server.PacketID)pingResponsePacketID);

            return (ping, timestampOffset);
        }

        private void Leave()
        {
            Packet leavePacket = new Packet();
            leavePacket.Append((short)Server.PacketID.Player_Leave).Append(localPlayerGuid).Append(localPlayerToken);
            client.Send(leavePacket.GetData(), leavePacket.GetSize());
        }
    }
}
