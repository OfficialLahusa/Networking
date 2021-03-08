using System;
using System.Collections.Generic;
using System.Text;

namespace MapToolkit
{
    [Flags]
    enum VertexFlags
    {
        None = 0,
        Draw = 1,
        PlayerBlock = 1 << 1,
        ShotBlock = 1 << 2,
        Wallbang = 1 << 3,
        Hook = 1 << 4
    }
}
