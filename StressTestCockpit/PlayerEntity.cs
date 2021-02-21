using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StressTestCockpit
{
    public class PlayerEntity
    {
        public Guid Guid, Token;
        string Name;
        public int Hue, NametagHue;
        public Vector2f Position;
        public float Rotation;
        public float MovementSpeed;
        public float RotationRate;

        public PlayerEntity(Guid guid, Guid token, string name, int hue, int nametagHue, Vector2f position, float rotation, float movementSpeed, float rotationRate)
        {
            Guid = guid;
            Token = token;
            Name = name;
            Hue = hue;
            NametagHue = nametagHue;
            Position = position;
            Rotation = rotation;
            MovementSpeed = movementSpeed;
            RotationRate = rotationRate;
        }
    }
}
