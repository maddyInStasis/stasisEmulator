using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using stasisEmulator.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static UIWindow _scrolledWindow;
        private static int _scrollWheelDelta;
        private static bool _formScrolled;

        public static Point MousePosition { get; private set; }
        public static UIWindow MainWindow { get; set; }
        public static Point MainWindowPosition { get; set; }
        public static Point AbsoluteMousePosition { get => MousePosition + MainWindowPosition; }

        public static void Update()
        {
            _prevMouseState = MouseState;
            MouseState = Mouse.GetState();

            MousePosition = MouseState.Position;
            MouseClickProcessed = false;
            HoverProcessed = false;
            MouseJustPressed = _prevMouseState.LeftButton == ButtonState.Released && MouseState.LeftButton == ButtonState.Pressed;
            MouseJustReleased = _prevMouseState.LeftButton == ButtonState.Pressed && MouseState.LeftButton == ButtonState.Released;
            if (!_formScrolled)
            {
                _scrollWheelDelta = MouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
                _scrolledWindow = MainWindow;
            }
            else
            {
                _formScrolled = false;
            }
        }

        public static void ProcessClick()
        {
            MouseClickProcessed = true;
        }

        public static void ProcessHover()
        {
            HoverProcessed = true;
        }

        public static Point GetWindowMousePosition(UIWindow window)
        {
            return AbsoluteMousePosition - window.GetPosition();
        }

        public static int GetWindowMouseScroll(UIWindow window)
        {
            if (window == _scrolledWindow)
                return _scrollWheelDelta;
            else
                return 0;
        }

        public static void FormScroll(UIWindow window, int delta)
        {
            _scrollWheelDelta = delta;
            _formScrolled = true;
            _scrolledWindow = window;
        }
    }
}
