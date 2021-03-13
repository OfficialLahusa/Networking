using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapToolkit
{
    public class VectorMap
    {
        public static readonly float UnitPixelScaleFactor = 100.0f;
        
        public VertexArray Triangles;
        public VertexArray Lines;
        public List<Text> Text;
        public Color? BackgroundColor;
        public Dictionary<string, IMapHook> Hooks;

        public VectorMap()
        {
            Triangles = new VertexArray(PrimitiveType.Triangles);
            Lines = new VertexArray(PrimitiveType.Lines);
            Text = new List<Text>();
            BackgroundColor = null;
            Hooks = new Dictionary<string, IMapHook>();
        }
    }
}
