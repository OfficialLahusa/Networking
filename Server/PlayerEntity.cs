using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dynamics = tainicom.Aether.Physics2D.Dynamics;
using Common = tainicom.Aether.Physics2D.Common;
using Collision = tainicom.Aether.Physics2D.Collision;

namespace Server
{
    public class PlayerEntity
    {
        public IPEndPoint endpoint;
        public Guid token;
        public string name;
        public Dynamics.Body body;
        public int playerHue;
        public int nametagHue;

        public static readonly float entityRadius = 45;
        public static readonly float entityOutline = 6;

        public PlayerEntity(Dynamics.Body body, IPEndPoint? endpoint = null, Guid? token = null, string name = null, int? playerHue = null, int? nametagHue = null)
        {
            this.body = body;
            this.endpoint = endpoint ?? new IPEndPoint(IPAddress.Any, 0);
            this.token = token ?? Guid.Empty;
            this.name = name ?? "Bot";

            Random random = new Random();
            this.playerHue = playerHue ?? random.Next(360);
            this.nametagHue = nametagHue ?? random.Next(360);
        }
    }
}
