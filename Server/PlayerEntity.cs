using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class PlayerEntity
    {
        public IPEndPoint endpoint;
        public Guid token;
        public string name;
        public Vector2f position;
        public float rotation;
        public int playerHue;
        public int nametagHue;

        public PlayerEntity(IPEndPoint? endpoint = null, Guid? token = null, string name = null, Vector2f? position = null, float? rotation = null, int? playerHue = null, int? nametagHue = null)
        {
            this.endpoint = endpoint ?? new IPEndPoint(IPAddress.Any, 0);
            this.token = token ?? Guid.Empty;
            this.name = name ?? "Bot";
            this.position = position ?? new Vector2f(0, 0);

            Random random = new Random();
            this.rotation = rotation ?? (float)random.NextDouble() * 360.0f;
            this.playerHue = playerHue ?? random.Next(360);
            this.nametagHue = nametagHue ?? random.Next(360);
        }
    }
}
