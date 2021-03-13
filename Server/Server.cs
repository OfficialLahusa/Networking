using LahusaPackets;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dynamics = tainicom.Aether.Physics2D.Dynamics;
using Common = tainicom.Aether.Physics2D.Common;
using Collision = tainicom.Aether.Physics2D.Collision;

namespace Server
{
    public class Server
    {
        UdpClient server;
        PacketHandler packetHandler;
        Dictionary<Guid, PlayerEntity> players;
        Stopwatch runtimeTimer;
        Stopwatch lastTickTimer;
        Dynamics.World world;

        public void Run()
        {
            Console.WriteLine("Networking Server - (C) Lasse Huber-Saffer, " + DateTime.UtcNow.Year);
            Console.CursorVisible = false;

            Console.WriteLine($"Starting as \"{Config.data.name}\" on port {Config.data.port} with tickrate {Config.data.tickrate}");
            Serialiser.SaveConfigFile(Config.data);

            double tickInterval = 1.0f / Config.data.tickrate;

            server = new UdpClient(Config.data.port);
            packetHandler = new PacketHandler(server);
            players = new Dictionary<Guid, PlayerEntity>();
            runtimeTimer = new Stopwatch();
            lastTickTimer = new Stopwatch();
            world = new Dynamics.World(new Common.Vector2(0, 0));

            // Disable error that closes the socket when a client forcibly disconnects
            // For ref: https://stackoverflow.com/questions/38191968/c-sharp-udp-an-existing-connection-was-forcibly-closed-by-the-remote-host
            server.Client.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);

            runtimeTimer.Start();
            lastTickTimer.Start();

            bool running = true;
            while (running)
            {
                // Receive incoming packets if there are any
                packetHandler.HandleImmediatePackets(players, world, runtimeTimer);

                // Check if a server tick should occur
                if (lastTickTimer.Elapsed.TotalSeconds >= tickInterval)
                {
                    lastTickTimer.Restart();
                    // Handle Tick-Packets
                    packetHandler.HandleTickPackets(players, world);

                    // Update Physics
                    world.Step((float)tickInterval);

                    // Send status packet to all players
                    packetHandler.SendStatusUpdate(players);
                }
            }

            Console.ReadKey();
        }
    }
}
