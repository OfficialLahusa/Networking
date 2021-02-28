using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapToolkit
{
    public class VectorMap
    {
        public VertexArray DrawLayer;
        public VertexArray DebugLines;

        public VectorMap()
        {
            DrawLayer = new VertexArray(PrimitiveType.Triangles);
            DebugLines = new VertexArray(PrimitiveType.Lines);
        }
    }
}
