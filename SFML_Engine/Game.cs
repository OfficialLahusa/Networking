using SFML.Graphics;
using SFML.System;
using SFML.Audio;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFML_Engine
{
    public class Game
    {
        public RenderWindow window;
        public StateMachine stateMachine;
        public Random random;
        public Dictionary<string, Texture> textures;
        public Dictionary<string, Font> fonts;
        public Dictionary<string, SoundBuffer> soundBuffers;
        public Clock runtimeClock;
        private Clock deltaClock;
        private float deltaTime;
        private int drawDepth = 0;
        public float cd = 0;

        public Game(uint width, uint height, string title)
        {
            textures = new Dictionary<string, Texture>();
            fonts = new Dictionary<string, Font>();
            soundBuffers = new Dictionary<string, SoundBuffer>();

            ContextSettings contextSettings = new ContextSettings(0, 0, 8);
            window = new(new VideoMode(width, height), title, Styles.Default, contextSettings);
            window.Closed += Window_Closed;

            random = new();

            deltaClock = new();
            runtimeClock = new();

            stateMachine = new();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            window.Close();
        }

        public void Run(State startingState)
        {
            stateMachine.Add(startingState);

            while (window.IsOpen)
            {
                deltaTime = deltaClock.Restart().AsSeconds();
                Console.CursorVisible = false;

                if (cd != 0)
                {
                    cd = Math.Max(0, cd - deltaTime);
                }

                if (stateMachine.states.Count == 0)
                {
                    window.Close();
                    return;
                }

                window.DispatchEvents();

                stateMachine.GetCurrent().Value.HandleInput(deltaTime);

                foreach(State state in stateMachine.states)
                {
                    if(!state.IsPaused)
                    {
                        state.Update(deltaTime);
                    }
                }

                //Rendering
                drawDepth = stateMachine.GetDrawDepth();
                window.Clear();

                for (int i = drawDepth - 1; i >= 0; i--)
                {
                    stateMachine.states.ElementAt(i).Draw(deltaTime);
                }

                window.Display();
            }
        }
    }
}
