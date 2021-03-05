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
            // Push starting state
            stateMachine.Add(startingState);
            Console.CursorVisible = false;

            // Game loop
            while (window.IsOpen)
            {
                // Calculate deltaTime
                deltaTime = deltaClock.Restart().AsSeconds();

                // Update cooldown
                if (cd != 0)
                {
                    cd = Math.Max(0, cd - deltaTime);
                }

                // Exit if all states have been closed
                if (stateMachine.states.Count == 0)
                {
                    window.Close();
                    return;
                }

                // Handle window events and trigger callbacks
                window.DispatchEvents();

                // Handle input in current state and update
                stateMachine.GetCurrent().Value.HandleInput(deltaTime);
                stateMachine.GetCurrent().Value.Update(deltaTime);

                // Update all unpaused background states
                for(int i = 1; i < stateMachine.states.Count; i++)
                {
                    State state = stateMachine.states.ElementAt(i);
                    if(!state.IsPaused)
                    {
                        state.BackgroundUpdate(deltaTime);
                    }
                }

                // Clear the buffer
                window.Clear();

                // Draw all states until an opaque one is hit
                drawDepth = stateMachine.GetDrawDepth();
                for (int i = drawDepth - 1; i >= 0; i--)
                {
                    stateMachine.states.ElementAt(i).Draw(deltaTime);
                }

                // Swap buffers
                window.Display();
            }
        }
    }
}
