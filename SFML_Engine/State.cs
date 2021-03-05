using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFML_Engine
{
    public abstract class State
    {
        protected Game game;
        public bool IsPaused = false;
        public abstract bool IsOpaque
        {
            get;
        }

        public State(Game game)
        {
            this.game = game;
        }

        ~State()
        {

        }

        public abstract void HandleInput(float deltaTime);

        public abstract void Update(float deltaTime);

        public abstract void BackgroundUpdate(float deltaTime);

        public abstract void Draw(float deltaTime);

    }
}
