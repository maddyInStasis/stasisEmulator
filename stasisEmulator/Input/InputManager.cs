using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.Input
{
    public static class InputManager
    {
        public static MouseState MouseState { get; private set; }
        private static MouseState _prevMouseState;

        public static bool MouseJustPressed { get; private set; }
        public static bool MouseJustReleased { get; private set; }
        
        public static bool HoverProcessed { get; private set; }
        public static bool MouseClickProcessed { get; private set; }

        public static int ScrollWheelDelta { get; private set; }

        public static void Update()
        {
            _prevMouseState = MouseState;
            MouseState = Mouse.GetState();

            MouseClickProcessed = false;
            HoverProcessed = false;
            MouseJustPressed = _prevMouseState.LeftButton == ButtonState.Released && MouseState.LeftButton == ButtonState.Pressed;
            MouseJustReleased = _prevMouseState.LeftButton == ButtonState.Pressed && MouseState.LeftButton == ButtonState.Released;
            ScrollWheelDelta = MouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
        }

        public static void ProcessClick()
        {
            MouseClickProcessed = true;
        }

        public static void ProcessHover()
        {
            HoverProcessed = true;
        }
    }
}
