using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public static class ColorConversion
    {
        public static Color HSVtoRGB(int hue, int sat, int val)
        {
            Color colors = new Color(0, 0, 0);
            // hue: 0-359, sat: 0-255, val (lightness): 0-255
            int r = 0, g = 0, b = 0, base_;
            if (sat == 0)
            {                     // Achromatic color (gray).
                colors.R = (byte)val;
                colors.G = (byte)val;
                colors.B = (byte)val;
            }
            else
            {
                base_ = ((255 - sat) * val) >> 8;
                switch (hue / 60)
                {
                    case 0:
                        r = val;
                        g = (((val - base_) * hue) / 60) + base_;
                        b = base_;
                        break;
                    case 1:
                        r = (((val - base_) * (60 - (hue % 60))) / 60) + base_;
                        g = val;
                        b = base_;
                        break;
                    case 2:
                        r = base_;
                        g = val;
                        b = (((val - base_) * (hue % 60)) / 60) + base_;
                        break;
                    case 3:
                        r = base_;
                        g = (((val - base_) * (60 - (hue % 60))) / 60) + base_;
                        b = val;
                        break;
                    case 4:
                        r = (((val - base_) * (hue % 60)) / 60) + base_;
                        g = base_;
                        b = val;
                        break;
                    case 5:
                        r = val;
                        g = base_;
                        b = (((val - base_) * (60 - (hue % 60))) / 60) + base_;
                        break;
                }
                colors.R = (byte)r;
                colors.G = (byte)g;
                colors.B = (byte)b;
            }

            return colors;
        }

        public static (int hue, int sat, int val) HSLtoHSV(int h, int s, int l)
        {
            int sat = s * (l < 127 ? l : 255 - l);

            return (h, 2 * sat / (l + sat), l + sat);
        }

        public static System.Drawing.Color SFMLColorToSystemColor(Color color)
        {
            System.Drawing.Color color1 = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B);

            return color1;
        }

        public static System.Drawing.Color SerializationColorToSystemColor(SerializationColor color)
        {
            System.Drawing.Color color1 = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B);

            return color1;
        }
        public static Color SerializationColorToSFMLColor(SerializationColor color)
        {
            Color color1 = new Color(color.R, color.G, color.B);

            return color1;
        }

        public static Color SystemColorToSFMLColor(System.Drawing.Color color)
        {
            Color color1 = new Color(color.R, color.G, color.B);

            return color1;
        }
    }
}
