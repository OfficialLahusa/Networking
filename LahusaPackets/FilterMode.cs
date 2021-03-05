using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahusaPackets
{
    public enum FilterMode
    {
        None = 0,
        Whitelist = 1,
        Blacklist = 1 << 1
    }
}
