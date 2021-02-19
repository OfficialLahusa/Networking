using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class PlayerEntity
    {
        public string name;
        public int playerHue;
        public int nametagHue;

        public Vector2f position;

        public PlayerEntity(string name = null, Vector2f? position = null, int? playerHue = null, int? nametagHue = null)
        {
            this.name = name ?? "Bot";
            this.position = position ?? new Vector2f(0, 0);

            Random random = new Random();
            this.playerHue = playerHue ?? random.Next(360);
            this.nametagHue = nametagHue ?? random.Next(360);
        }
    }
}
