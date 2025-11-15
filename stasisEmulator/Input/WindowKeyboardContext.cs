using Microsoft.Xna.Framework.Input;
using stasisEmulator.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace stasisEmulator.Input
{
    public class KeyboardEventArgs : EventArgs
    {
        public Keys Key { get; set; }
        public int KeyCode { get; set; }
    }

    public readonly struct WindowKeyboardState(HashSet<int> pressedKeys, bool capsLock)
    {
        public bool CapsLock { get; } = capsLock;

        private readonly HashSet<int> _pressedKeys = [.. pressedKeys];

        public readonly bool IsKeyDown(Keys key)
        {
            return _pressedKeys != null && _pressedKeys.Contains((int)key);
        }
        public readonly bool IsKeyUp(Keys key) { return !IsKeyDown(key); }

        public readonly bool AnyShiftHeld()
        {
            return IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);
        }
        public readonly bool AnyCtrlHeld()
        {
            return IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
        }
        public readonly bool AnyAltHeld()
        {
            return IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.LeftAlt);
        }
        public readonly bool AnyWinHeld()
        {
            return IsKeyDown(Keys.LeftWindows) || IsKeyDown(Keys.RightWindows);
        }

        public readonly Keys[] GetPressedKeys()
        {
            if (_pressedKeys == null)
                return [];

            Keys[] keys = new Keys[_pressedKeys.Count];

            int i = 0;
            foreach (int keyCode in _pressedKeys)
            {
                keys[i] = (Keys)keyCode;
                i++;
            }

            return keys;
        }
    }

    public class WindowKeyboardContext
    {
        public event EventHandler<KeyboardEventArgs> KeyDown;
        public event EventHandler<KeyboardEventArgs> KeyUp;

        private readonly bool _isForm = false;

        private readonly HashSet<int> _pressedKeys = [];
        //value represents how many windows have this key pressed
        //probably avoids race conditions and makes it easier to track
        //no need to keep track of all keyboards in a list or something
        private static readonly Dictionary<int, int> _globalPressedKeys = [];
        private static bool _capsLock;

        private readonly static Dictionary<System.Windows.Forms.Keys, Keys> _keyConversion = new()
        {
            { System.Windows.Forms.Keys.ControlKey, Keys.LeftControl },
            { System.Windows.Forms.Keys.ShiftKey, Keys.LeftShift },
        };

        public WindowKeyboardContext(UIWindow window)
        {
            var form = window.Form;
            if (form == null)
                return;

            _isForm = true;
            form.KeyDown += Form_KeyDown;
            form.KeyUp += Form_KeyUp;
            form.LostFocus += Form_LostFocus;
        }

        public void Update()
        {
            if (!_isForm)
            {
                var keyboardState = Keyboard.GetState();
                _capsLock = keyboardState.CapsLock;

                var keys = keyboardState.GetPressedKeys();

                foreach (var key in keys)
                {
                    int keyCode = (int)key;

                    //key was already pressed
                    if (_pressedKeys.Contains(keyCode))
                        continue;

                    SetKeyState(keyCode, true);
                }

                foreach (var keyCode in _pressedKeys)
                {
                    //key was not released
                    if (keys.Contains((Keys)keyCode))
                        continue;

                    SetKeyState(keyCode, false);
                }
            }
        }

        public WindowKeyboardState GetState()
        {
            return new(_pressedKeys, _capsLock);
        }

        public static WindowKeyboardState GetGlobalState()
        {
            HashSet<int> keys = [];
            foreach (var kv in _globalPressedKeys)
            {
                keys.Add(kv.Key);
            }
            return new(keys, _capsLock);
        }

        public void WindowClosing()
        {
            ClearKeyStates();
        }

        //some keys don't get converted as nicely as others
        private static int GetKeyCode(System.Windows.Forms.KeyEventArgs e)
        {
            int keyCode = e.KeyValue;
            if (_keyConversion.TryGetValue(e.KeyCode, out Keys value))
                keyCode = (int)value;

            return keyCode;
        }

        private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            int keyCode = GetKeyCode(e);

            if (_pressedKeys.Contains(keyCode))
                return;

            SetKeyState(keyCode, true);
        }

        private void Form_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            int keyCode = GetKeyCode(e);

            if (!_pressedKeys.Contains(keyCode))
                return;

            SetKeyState(keyCode, false);
        }

        private void Form_LostFocus(object sender, EventArgs e)
        {
            ClearKeyStates();
        }

        private void ClearKeyStates()
        {
            foreach (var keyCode in _pressedKeys)
            {
                SetKeyState(keyCode, false);
            }
        }

        private void SetKeyState(int keyCode, bool value)
        {
            //don't send events if the state did not change
            if (value == _pressedKeys.Contains(keyCode))
                return;

            if (value)
            {
                _pressedKeys.Add(keyCode);
                KeyDown?.Invoke(this, new KeyboardEventArgs() { Key = (Keys)keyCode, KeyCode = keyCode });

                if (_globalPressedKeys.ContainsKey(keyCode))
                    _globalPressedKeys[keyCode]++;
                else
                    _globalPressedKeys[keyCode] = 1;
            }
            else
            {
                _pressedKeys.Remove(keyCode);
                KeyUp?.Invoke(this, new KeyboardEventArgs() { Key = (Keys)keyCode, KeyCode = keyCode });

                _globalPressedKeys[keyCode]--;
                if (_globalPressedKeys[keyCode] == 0)
                    _globalPressedKeys.Remove(keyCode);
            }
        }
    }
}
