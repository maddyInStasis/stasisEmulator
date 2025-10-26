using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using stasisEmulator.Input;
using stasisEmulator.UI.Controls;
using System;

namespace stasisEmulator.UI.Components
{
    public class MouseComponent(UIControl owner)
    {
        private readonly UIControl _owner = owner;

        public bool IsMouseHovered { get; private set; }
        public bool IsElementPressed { get; set; }
        public bool MouseDownOnElement { get => (IsElementPressed && IsMouseHovered); }

        public int ScrollAmount { get; private set; }

        public bool ElementMouseJustDown { get; private set; }
        public event EventHandler OnElementMouseDown;

        public bool ElementMouseJustUp { get; private set; }
        public event EventHandler OnElementMouseUp;

        public Vector2 NormalizedMousePosition { get; private set; }

        public static bool IsMouseHoveredInRectangle(Rectangle rectangle)
        {
            var mouseState = InputManager.MouseState;
            return rectangle.Contains(mouseState.X, mouseState.Y);
        }

        public static Vector2 GetNormalizedMousePositionInRectangle(Rectangle rectangle)
        {
            var mouseState = InputManager.MouseState;
            float normalX = (mouseState.X - rectangle.X) / (float)rectangle.Width;
            float normalY = (mouseState.Y - rectangle.Y) / (float)rectangle.Height;
            return new Vector2(normalX, normalY);
        }

        /// <summary>
        /// Updates this <c>MouseComponent</c> using the bounds of the owner element.
        /// </summary>
        public void Update()
        {
            UpdateAsRectangle(_owner.Bounds);
        }

        /// <summary>
        /// Updates this <c>MouseComponent</c> using the bounds provided, rather than the bounds of the owner.
        /// </summary>
        /// <param name="rect">The bounds to check for mouse interaction.</param>
        public void UpdateAsRectangle(Rectangle rect)
        {
            ElementMouseJustDown = false;
            ElementMouseJustUp = false;

            IsMouseHovered = IsMouseHoveredInRectangle(rect);

            if (IsMouseHovered)
                ScrollAmount = InputManager.ScrollWheelDelta;
            else
                ScrollAmount = 0;

            NormalizedMousePosition = GetNormalizedMousePositionInRectangle(rect);

            if (InputManager.MouseJustClicked && !InputManager.MouseClickProcessed && IsMouseHovered)
            {
                OnElementMouseDown?.Invoke(this, EventArgs.Empty);
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
                    OnElementMouseUp?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
