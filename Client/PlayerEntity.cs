using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class PlayerEntity : Drawable
    {
        private CircleShape entity;
        private CircleShape leftHand;
        private CircleShape rightHand;
        private Font font;
        private Text nametag;

        private const float entityRadius = 45;
        private const float handRadius = 12;
        private const float entityOutline = 6;
        private const float handOutline = 6;
        private float movementSpeed = 300.0f;
        private readonly float sqrtOfTwo = (float)Math.Sqrt(2.0);
        public static readonly int playerColorSaturation = 200;
        public static readonly int playerColorValue = 255;
        public static readonly int playerOutlineColorSaturation = 200;
        public static readonly int playerOutlineColorValue = 180;
        public static readonly int nametagColorSaturation = 195;
        public static readonly int nametagColorValue = 100;

        public Vector2f Position
        {
            get
            {
                return entity.Position;
            }
            set
            {
                entity.Position = value;
            }
        }

        public Color FillColor
        {
            get
            {
                return entity.FillColor;
            }
            set
            {
                entity.FillColor = value;
                leftHand.FillColor = value;
                rightHand.FillColor = value;
            }
        }

        public Color OutlineColor
        {
            get
            {
                return entity.OutlineColor;
            }
            set
            {
                entity.OutlineColor = value;
                leftHand.OutlineColor = value;
                rightHand.OutlineColor = value;
            }
        }

        public PlayerEntity(string name, Font font, Vector2f pos, int playerHue, int nametagHue)
        {
            entity = new CircleShape(entityRadius);
            leftHand = new CircleShape(handRadius);
            rightHand = new CircleShape(handRadius);
            this.font = font;
            nametag = new Text(name, font);

            SetColorFromHue(playerHue);
            SetNametagColorFromHue(nametagHue);

            entity.OutlineThickness = entityOutline;
            leftHand.OutlineThickness = rightHand.OutlineThickness = handOutline;

            float handOffset = entityRadius + entityOutline;
            entity.Origin = new Vector2f(entityRadius, entityRadius);
            leftHand.Origin = new Vector2f(handRadius + handOffset / sqrtOfTwo, handRadius - handOffset / sqrtOfTwo);
            rightHand.Origin = new Vector2f(handRadius + handOffset / sqrtOfTwo, handRadius + handOffset / sqrtOfTwo);

            nametag.CharacterSize = 30;  
            nametag.Origin = new Vector2f(nametag.GetLocalBounds().Width / 2.0f, 1.3f * entityRadius + nametag.GetLocalBounds().Height);

            entity.Position = leftHand.Position = rightHand.Position = nametag.Position = new Vector2f(pos.X, pos.Y);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            target.Draw(entity);
            target.Draw(leftHand);
            target.Draw(rightHand);
            target.Draw(nametag);
        }

        public void UpdateRotation(float playerRotation)
        {
            entity.Rotation = leftHand.Rotation = rightHand.Rotation = playerRotation;
        }

        public void UpdatePosition(Vector2f playerPosition)
        {
            leftHand.Position = rightHand.Position = entity.Position = nametag.Position = playerPosition;
        }

        public void SetColorFromHue(int hue)
        {
            hue %= 360;
            FillColor = ColorConversion.HSVtoRGB(hue, playerColorSaturation, playerColorValue);
            OutlineColor = ColorConversion.HSVtoRGB(hue, playerOutlineColorSaturation, playerOutlineColorValue);
        }

        public void SetNametagColorFromHue(int hue)
        {
            hue %= 360;
            nametag.FillColor = ColorConversion.HSVtoRGB(hue, nametagColorSaturation, nametagColorValue);
        }

        public void Move(Vector2f amount)
        {
            entity.Position += movementSpeed * amount;
        }
    }
}
