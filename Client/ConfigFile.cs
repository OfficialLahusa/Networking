using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class ConfigFile
    {
        public string name = "Networking Test Client";
        public string hostname = "localhost";
        public short port = 5555;
        public int nametagHue = 90;
        public int playerHue = 22;
        public bool autoConnect = false;

        public ConfigFile()
        {

        }
    }
}
