﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public static class Config
    {
        public static ConfigFile data;
        static Config()
        {
            data = Serialiser.LoadConfigFile();
        }
    }
}
