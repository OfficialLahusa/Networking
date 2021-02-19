using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFML_Engine
{
    public class ColorState : State
    {
        Color color;

        public ColorState(Game game, Color color) : base(game)
        {
            this.color = color;
        }

        ~ColorState()
        {

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
            game.window.Clear(color);
        }

        public override void HandleInput(float deltaTime)
        {
            if(game.window.HasFocus() && game.cd == 0)
            {
                if (Mouse.IsButtonPressed(Mouse.Button.Left) && game.cd == 0)
                {
                    game.stateMachine.RemoveCurrent();
                    game.cd += 0.3f;
                }
                if (Keyboard.IsKeyPressed(Keyboard.Key.Space) && game.cd == 0)
                {
                    game.stateMachine.Add(new ColorState(game, new Color((byte)game.random.Next(256), (byte)game.random.Next(256), (byte)game.random.Next(256))));
                    game.cd += 0.3f;
                }
                if (Keyboard.IsKeyPressed(Keyboard.Key.Enter) && game.cd == 0)
                {
                    game.stateMachine.Add(new OverlayState(game));
                    game.cd += 0.3f;
                }
            }
        }

        public override void Update(float deltaTime)
        {
            
        }
    }
}
