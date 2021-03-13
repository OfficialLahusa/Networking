using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapToolkit
{
    public class PointHook : IMapHook
    {
        public readonly Vector2f Position;

        public PointHook(Vector2f position)
        {
            Position = position;
        }
    }
}
