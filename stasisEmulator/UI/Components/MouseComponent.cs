using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using stasisEmulator.Input;
using stasisEmulator.UI.Controls;
using System;
using System.Diagnostics;

namespace stasisEmulator.UI.Components
{
    public class MouseEventArgs : EventArgs
    {
        /// <summary>
        /// Represents the mouse's position within the current window.
        /// </summary>
        public Point MousePosition { get; set; }
        /// <summary>
        /// Represents the mouse's offset from this control's position.
        /// </summary>
        public Point RelativeMousePosition { get; set; }
        /// <summary>
        /// Represents the mouse's position from 0-1 within this control's bounds.
        /// </summary>
        public Vector2 NormalizedMousePosition { get; set; }
    }

    public class MouseComponent(UIControl owner)
    {
        private readonly UIControl _owner = owner;

        public bool IsMouseHovered { get; private set; }
        public bool IsElementPressed { get; set; }
        public bool MouseDownOnElement { get => IsElementPressed && IsMouseHovered; }

        public int ScrollAmount { get; private set; }

        public bool ElementMouseJustDown { get; private set; }
        public event EventHandler<MouseEventArgs> OnElementMouseDown;

        public bool ElementMouseJustUp { get; private set; }
        public event EventHandler<MouseEventArgs> OnElementMouseUp;

        public Point MousePosition { get => InputManager.GetWindowMousePosition(_owner.Window); }
        public Vector2 NormalizedMousePosition { get; private set; }

        public bool IsMouseHoveredInRectangle(Rectangle rectangle)
        {
            if (_owner.Window == null) return false;

            var mousePos = MousePosition;
            return rectangle.Contains(mousePos.X, mousePos.Y);
        }

        public Vector2 GetNormalizedMousePositionInRectangle(Rectangle rectangle)
        {
            if (_owner.Window == null) return Vector2.Zero;

            var mousePos = MousePosition;
            float normalX = (mousePos.X - rectangle.X) / (float)rectangle.Width;
            float normalY = (mousePos.Y - rectangle.Y) / (float)rectangle.Height;
            return new Vector2(normalX, normalY);
        }

        /// <summary>
        /// Updates this <c>MouseComponent</c> using the bounds of the owner element.
        /// </summary>
        public void Update(bool enabled = true)
        {
            UpdateAsRectangle(_owner.Bounds, enabled);
        }

        /// <summary>
        /// Updates this <c>MouseComponent</c> using the bounds provided, rather than the bounds of the owner.
        /// </summary>
        /// <param name="rect">The bounds to check for mouse interaction.</param>
        public void UpdateAsRectangle(Rectangle rect, bool enabled = true)
        {
            ElementMouseJustDown = false;
            ElementMouseJustUp = false;
            IsMouseHovered = false;
            
            if (!enabled)
            {
                IsElementPressed = false;
                return;
            }

            IsMouseHovered = !InputManager.HoverProcessed && IsMouseHoveredInRectangle(rect);
            if (IsMouseHovered)
                InputManager.ProcessHover();

            if (IsMouseHovered)
                ScrollAmount = InputManager.GetWindowMouseScroll(_owner.Window);
            else
                ScrollAmount = 0;

            NormalizedMousePosition = GetNormalizedMousePositionInRectangle(rect);

            if (InputManager.MouseJustPressed && !InputManager.MouseClickProcessed && IsMouseHovered)
            {
                OnElementMouseDown?.Invoke(this, GetEventArgs(rect));
                ElementMouseJustDown = true;
                IsElementPressed = true;
                InputManager.ProcessClick();
            }

            if (InputManager.MouseJustReleased && IsElementPressed)
            {
                IsElementPressed = false;
                if (IsMouseHovered)
                {
                    ElementMouseJustUp = true;
                    OnElementMouseUp?.Invoke(this, GetEventArgs(rect));
                }
            }
        }

        private MouseEventArgs GetEventArgs(Rectangle bounds)
        {
            return new() 
            { 
                MousePosition = MousePosition,
                RelativeMousePosition = MousePosition - bounds.Location,
                NormalizedMousePosition = GetNormalizedMousePositionInRectangle(bounds)
            };
        }
    }
}
