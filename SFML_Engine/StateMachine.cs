using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFML_Engine
{
    public class StateMachine
    {
        public LinkedList<State> states;

        public StateMachine()
        {
            states = new LinkedList<State>();
        }

        public void Add(State state)
        {
            states.AddFirst(state);
        }

        public void ReplaceState(LinkedListNode<State> target, State state)
        {
            if(states.Count == 0 || target is null)
            {
                return;
            }

            target.Value = state;
        }

        public void ReplaceCurrent(State state)
        {
            if (states.Count == 0)
            {
                Add(state);
            }

            ReplaceState(states.First, state);
        }

        public void RemoveCurrent()
        {
            if (states.Count > 0)
            {
                states.RemoveFirst();
            }
        }

        public LinkedListNode<State> GetCurrent()
        {
            return states.First;
        }

        public int GetDrawDepth()
        {
            int drawDepth = 0;
            for (int i = 0; i < states.Count; i++)
            {
                drawDepth++;
                if (states.ElementAt(i).IsOpaque)
                {
                    break;
                }
            }
            return drawDepth;
        }

    }
}
