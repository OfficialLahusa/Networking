using System;
using Client.States;
using SFML.Graphics;
using SFML_Engine;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            Game game = new Game(800, 800, "Networking Client - (C) Lasse Huber-Saffer, " + DateTime.UtcNow.Year);

            if(Config.data.autoConnect)
            {
                game.Run(new GameState(game));
            } else
            {
                game.Run(new LoginState(game));
            }
        }
    }
}
