using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    internal class UIRectangle : UIControl
    {
        public UIRectangle() : base() { }
        public UIRectangle(UIControl parent) : base(parent) { }
        public UIRectangle(List<UIControl> children) : base(children) { }

        public Color BackgroundColor { get; set; } = Color.White;

        protected override void RenderElement(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);
        }
    }
}
