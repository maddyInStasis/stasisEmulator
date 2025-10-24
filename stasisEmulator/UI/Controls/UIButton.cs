using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.DXGI;
using stasisEmulator.Input;
using stasisEmulator.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    public class UIButton : UIControl
    {
        private readonly MouseComponent _mouseComponent;

        public UIButton() { _mouseComponent = new(this); }
        public UIButton(UIControl parent) : base(parent) { _mouseComponent = new(this); }
        public UIButton(List<UIControl> children) : base(children) { _mouseComponent = new(this); }

        public bool Enabled { get; set; } = true;

        public bool MouseJustDown { get => _mouseComponent.ElementMouseJustDown; }
        public event EventHandler MouseDown { add => _mouseComponent.OnElementMouseDown += value; remove => _mouseComponent.OnElementMouseDown -= value; }
        public bool JustClicked { get => _mouseComponent.ElementMouseJustUp; }
        public event EventHandler Click { add => _mouseComponent.OnElementMouseUp += value; remove => _mouseComponent.OnElementMouseUp -= value; }

        /// <summary>
        /// True when cursor is within the bounds of the button.
        /// </summary>
        public bool IsButtonHovered { get => _mouseComponent.IsMouseHovered; }

        /// <summary>
        /// True between initial mouse down on button, false on next mouse up.
        /// </summary>
        public bool IsButtonPressed { get =>  _mouseComponent.IsElementPressed; }
        /// <summary>
        /// Equivalent to <c>IsButtonPressed &amp;&amp; IsMouseHovered</c>. Reflects the behavior seen on some scroll bar up/down or left/right buttons.
        /// </summary>
        public bool MouseDownOnButton { get => _mouseComponent.MouseDownOnElement; }

        public Color IdleColor { get; set; } = Color.White;
        public Color HoverColor { get; set; } = Color.LightGray;
        public Color PressColor { get; set; } = Color.Gray;
        public Color DisabledColor { get; set; } = Color.LightGray;

        protected override void UpdateElementPreLayout()
        {
            if (!Enabled)
            {
                _mouseComponent.IsElementPressed = false;
                return;
            }

            _mouseComponent.Update();
        }

        protected override void RenderElement(SpriteBatch spriteBatch)
        {
            Color drawCol = MouseDownOnButton ? PressColor : (IsButtonHovered ? HoverColor : IdleColor);
            DrawRect(spriteBatch, new(ComputedX, ComputedY, ComputedWidth, ComputedHeight), Enabled ? drawCol : DisabledColor);
        }
    }
}
