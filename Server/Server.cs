using LahusaPackets;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        UdpClient server;
        PacketHandler packetHandler;
        Dictionary<Guid, PlayerEntity> players;
        Stopwatch runtimeTimer;
        Stopwatch lastTickTimer;

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

            runtimeTimer.Start();
            lastTickTimer.Start();

            bool running = true;
            while (running)
            {
                // Receive incoming packets if there are any
                packetHandler.HandleImmediatePackets(players, runtimeTimer);

                // Check if a server tick should occur
                if (lastTickTimer.Elapsed.TotalSeconds >= tickInterval)
                {
                    lastTickTimer.Restart();
                    // Handle Tick-Packets
                    packetHandler.HandleTickPackets(players);

                    // Send status packet to all players
                    packetHandler.SendStatusUpdate(players);
                }
            }

            Console.ReadKey();
        }
    }
}
