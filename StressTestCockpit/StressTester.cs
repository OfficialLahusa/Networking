using LahusaPackets;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StressTestCockpit
{
    public class StressTester
    {
        List<PlayerEntity> entities;
        UdpClient client;
        Random random;

        public void Run()
        {
            Console.WriteLine("Networking StressTestCockpit - (C) Lasse Huber-Saffer, " + DateTime.UtcNow.Year);
            Console.CursorVisible = false;

            entities = new List<PlayerEntity>();
            client = new UdpClient();
            random = new Random();

            Console.WriteLine($"Connecting to {Config.data.hostname}:{Config.data.port}");

            client.Connect(Config.data.hostname, Config.data.port);

            for (int i = 0; i < Config.data.clientAmount; i++)
            {
                int hue = random.Next(360), nametagHue = random.Next(360);
                float rotation = (float)random.NextDouble() * 360.0f;

                Packet playerJoinPacket = new Packet();
                playerJoinPacket.Append((short)Server.PacketID.Player_Join).Append(Config.data.name + " #" + (i + 1).ToString()).Append(hue).Append(nametagHue);
                client.Send(playerJoinPacket.GetData(), playerJoinPacket.GetSize());

                IPEndPoint playerJoinResponseEndpoint;
                byte[] playerJoinResponseBytes;
                Packet playerJoinResponsePacket;
                short playerJoinResponsePacketID;
                do
                {
                    playerJoinResponseEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    playerJoinResponseBytes = client.Receive(ref playerJoinResponseEndpoint);
                    playerJoinResponsePacket = new Packet(playerJoinResponseBytes);

                    playerJoinResponsePacketID = -1;
                    playerJoinResponsePacket.ResetReadPos().Read(ref playerJoinResponsePacketID);
                } while (playerJoinResponsePacketID != (short)Server.PacketID.Player_Join_Response);

                bool acceptedPlayerJoin = false;
                playerJoinResponsePacket.Read(ref acceptedPlayerJoin);

                if (!acceptedPlayerJoin)
                {
                    Console.WriteLine($"Server rejected player join at #{i}");
                    break;
                }
                else
                {
                    Guid guid = Guid.Empty, token = Guid.Empty;
                    float posx = 0, posy = 0;

                    playerJoinResponsePacket.Read(ref guid).Read(ref token).Read(ref posx).Read(ref posy);

                    float randomMovementSpeed = random.Next(300, 2*300);
                    float randomRotationSpeed = (random.Next(0, 2) == 1 ? 1 : -1) * Config.data.rotationRate; // ((float)random.NextDouble() * 2.0f - 1.0f) * 

                    entities.Add(new PlayerEntity(guid, token, Config.data.name + " #" + (i + 1).ToString(), hue, nametagHue, new Vector2f(posx, posy), rotation, randomMovementSpeed, randomRotationSpeed));
                    Console.WriteLine($"Initialized local player entity with guid {guid} and token {token}");
                }
            }

            float tickInterval = 1.0f / (float)Config.data.tickrate;
            Stopwatch lastTickTimer = new Stopwatch();
            lastTickTimer.Start();
            bool running = true;

            Console.WriteLine("Press [L] to disconnect clients");
            while(running)
            {
                // Check if tick should happen
                if(lastTickTimer.Elapsed.TotalSeconds > tickInterval)
                {
                    lastTickTimer.Restart();

                    if(Keyboard.IsKeyPressed(Keyboard.Key.L))
                    {
                        foreach (PlayerEntity entity in entities)
                        {
                            Packet playerLeavePacket = new Packet();
                            playerLeavePacket.Append((short)Server.PacketID.Player_Leave).Append(entity.Guid).Append(entity.Token);

                            client.Send(playerLeavePacket.GetData(), playerLeavePacket.GetSize());
                        }
                        Console.WriteLine("Sent leave packets");

                        running = false;
                    } else
                    {
                        int i = 0;
                        foreach (PlayerEntity entity in entities)
                        {
                            float rotation = /*(float)((random.NextDouble() * 2.0f) - 1.0f) **/ entity.RotationRate * tickInterval;
                            entity.Rotation += rotation;
                            Vector2f moveVector = entity.MovementSpeed * new Vector2f(MathF.Cos((entity.Rotation + 180.0f) / 180.0f * MathF.PI), MathF.Sin((entity.Rotation + 180.0f) / 180.0f * MathF.PI)) * tickInterval;
                            entity.Position += moveVector;

                            Packet playerRotatePacket = new Packet();
                            playerRotatePacket.Append((short)Server.PacketID.Player_Rotate).Append(entity.Guid).Append(entity.Token).Append(entity.Rotation);
                            Packet playerMovePacket = new Packet();
                            playerMovePacket.Append((short)Server.PacketID.Player_Move).Append(entity.Guid).Append(entity.Token).Append(moveVector.X).Append(moveVector.Y);

                            int rotateBytesSent = client.Send(playerRotatePacket.GetData(), playerRotatePacket.GetSize());
                            int moveBytesSent = client.Send(playerMovePacket.GetData(), playerMovePacket.GetSize());
                            i++;

                            //Console.WriteLine($"Sent player move packet (Bytes: {moveBytesSent}): {i}, (x: {moveVector.X}, y: {moveVector.Y})");
                        }
                    }
                }
            }

            //Console.ReadLine();
        }
    }
}
