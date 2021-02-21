using System;
using SFML.System;

namespace StressTestCockpit
{
    class Program
    {
        static void Main(string[] args)
        {
            StressTester stressTester = new StressTester();
            stressTester.Run();
        }
    }
}
