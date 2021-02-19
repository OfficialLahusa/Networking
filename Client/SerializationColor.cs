using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class SerializationColor
    {
        public byte R;
        public byte G;
        public byte B;

        public SerializationColor(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public SerializationColor()
        {
            R = 0x0;
            G = 0x0;
            B = 0x0;
        }
    }
}
