using SFML.Graphics;
using SFML.System;
using System;

namespace Client.UI
{
    public class Slider : Drawable
    {
        RectangleShape bar;
        RectangleShape slider;
        Color normalColor;
        Color selectedColor;

        public Slider(Vector2f position, Vector2f size, float sliderWidth, Color normalColor, Color selectedColor, Texture texture = null)
        {
            bar = new RectangleShape(size);
            bar.Texture = texture;
            bar.Position = position;
            bar.OutlineColor = normalColor;
            bar.OutlineThickness = 2;

            slider = new RectangleShape(new Vector2f(sliderWidth, size.Y + bar.OutlineThickness * 2));
            slider.Origin = slider.Size / 2;
            slider.FillColor = normalColor;

            this.normalColor = normalColor;
            this.selectedColor = selectedColor;

            SetSliderPosition(0.5f);
        }

        public float GetValue()
        {
            return (slider.Position.X - bar.Position.X) / bar.GetGlobalBounds().Width;
        }

        public void Select()
        {
            bar.OutlineColor = selectedColor;
            slider.FillColor = selectedColor;
        }

        public void Deselect()
        {
            bar.OutlineColor = normalColor;
            slider.FillColor = normalColor;
        }

        public void SetSliderPosition(float position)
        {
            slider.Position = new Vector2f(bar.Position.X + bar.GetGlobalBounds().Width * position, bar.Position.Y + bar.GetGlobalBounds().Height / 2 - 2);
        }

        public void SetSliderPositionFromWorldPos(float horizontalWorldPos)
        {
            slider.Position = new Vector2f(Math.Min(bar.Position.X + bar.GetGlobalBounds().Width, Math.Max(bar.Position.X, horizontalWorldPos)), slider.Position.Y);
        }

        public bool Contains(float x, float y)
        {
            return bar.GetGlobalBounds().Contains(x, y);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            target.Draw(bar);
            target.Draw(slider);
        }
    }
}
