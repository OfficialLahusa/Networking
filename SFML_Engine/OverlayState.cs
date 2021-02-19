using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFML_Engine
{
    public class OverlayState : State
    {
        RectangleShape bg;
        RectangleShape box;

        public OverlayState(Game game) : base(game)
        {
            bg = new RectangleShape(new Vector2f(game.window.Size.X, game.window.Size.Y));
            bg.FillColor = new Color(0, 0, 0, 50);
            box = new RectangleShape(new Vector2f(game.window.Size.X / 3, game.window.Size.Y / 3));
            box.Position = new Vector2f(game.window.Size.X / 6 + (float)game.random.NextDouble() * game.window.Size.X / 3, game.window.Size.X / 6 + (float)game.random.NextDouble() * game.window.Size.X / 3);
            box.FillColor = Color.Green;
        }

        ~OverlayState()
        {

        }

        public override bool IsOpaque
        {
            get
            {
                return false;
            }
        }

        public override void Draw(float deltaTime)
        {
            game.window.Draw(bg);
            game.window.Draw(box);
        }

        public override void HandleInput(float deltaTime)
        {
            if (game.window.HasFocus() && game.cd == 0)
            {
                if (Mouse.IsButtonPressed(Mouse.Button.Left) /*&& box.GetGlobalBounds().Contains(Mouse.GetPosition().X, Mouse.GetPosition().Y)*/ && game.cd == 0)
                {
                    game.stateMachine.RemoveCurrent();
                    game.cd += 0.3f;
                }
            }
        }

        public override void Update(float deltaTime)
        {
            
        }
    }
}
