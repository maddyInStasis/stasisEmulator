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
        private MouseComponent _mouseComponent;

        public UIButton() { Init(); }
        public UIButton(UIControl parent) : base(parent) { Init(); }
        public UIButton(List<UIControl> children) : base(children) { Init(); }

        public bool Enabled { get; set; } = true;

        public bool MouseJustDown { get => _mouseComponent.ElementMouseJustDown; }
        public event EventHandler<MouseEventArgs> MouseDown { add => _mouseComponent.OnElementMouseDown += value; remove => _mouseComponent.OnElementMouseDown -= value; }
        public bool JustClicked { get => _mouseComponent.ElementMouseJustUp; }
        public event EventHandler<MouseEventArgs> Click { add => _mouseComponent.OnElementMouseUp += value; remove => _mouseComponent.OnElementMouseUp -= value; }

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

        public Color IdleBorderColor { get; set; } = Color.Black;
        public Color HoverBorderColor { get; set; } = Color.Black;
        public Color PressBorderColor { get; set; } = Color.Black;
        public Color DisabledBorderColor { get; set; } = Color.Gray;

        /// <summary>
        /// When set, assigns all border colors to the same value
        /// </summary>
        public Color BorderColor
        { 
            set 
            {
                IdleBorderColor = value;
                HoverBorderColor = value;
                PressBorderColor = value;
                DisabledBorderColor = value;
            } 
        }

        public int BorderThickness { get; set; } = 1;
        public BorderType BorderType { get; set; } = BorderType.Inside;

        private void Init()
        {
            _mouseComponent = new(this);
            Padding = new(8, 2);
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            _mouseComponent.Update(Enabled && PropagatedVisibility);
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            Color drawCol = MouseDownOnButton ? PressColor : (IsButtonHovered ? HoverColor : IdleColor);
            DrawRect(spriteBatch, Bounds, Enabled ? drawCol : DisabledColor);
            Color borderCol = MouseDownOnButton ? PressBorderColor : (IsButtonHovered ? HoverBorderColor : IdleBorderColor);
            DrawBorder(spriteBatch, Bounds, BorderThickness, BorderType, Enabled ? borderCol : DisabledBorderColor);
        }
    }
}
