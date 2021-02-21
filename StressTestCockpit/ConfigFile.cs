using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StressTestCockpit
{
    public class ConfigFile
    {
        public string name = "Stress Test Client";
        public int clientAmount = 30;
        public float rotationRate = 180;
        public int tickrate = 144;
        public string hostname = "localhost";
        public short port = 5555;
    }
}
