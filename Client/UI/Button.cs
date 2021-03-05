using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.UI
{
    public class Button : Drawable
    {
        Text text;
        RectangleShape box;
        Color normalColor;
        Color selectedColor;

        public Button(string content, Font font, uint characterSize, Vector2f position, Color normalColor, Color selectedColor)
        {
            text = new Text(content, font, characterSize)
            {
                FillColor = normalColor
            };
            float horizontalMargin = text.CharacterSize * 0.4f;
            box = new RectangleShape(new Vector2f(text.GetGlobalBounds().Width + horizontalMargin, text.CharacterSize * 1.3f))
            {
                FillColor = Color.Transparent,
                OutlineColor = normalColor,
                OutlineThickness = 2
            };

            box.Origin = box.Size / 2;
            text.Origin = box.Origin - new Vector2f(horizontalMargin / 3.0f, 0.0f);
            text.Position = box.Position = position;

            this.normalColor = normalColor;
            this.selectedColor = selectedColor;
        }

        public void Select()
        {
            box.OutlineColor = selectedColor;
            text.FillColor = selectedColor;
        }

        public void Deselect()
        {
            box.OutlineColor = normalColor;
            text.FillColor = normalColor;
        }

        public bool Contains(float x, float y)
        {
            return box.GetGlobalBounds().Contains(x, y);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            target.Draw(box);
            target.Draw(text);
        }
    }
}
