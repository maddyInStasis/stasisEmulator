using Microsoft.Xna.Framework.Input;
using stasisEmulator.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace stasisEmulator.Input
{
    public class BindInputs
    {
        public readonly List<Keys> KeyboardInputs = [];
        public readonly List<Buttons> GamePadInputs = [];
        public readonly List<Func<bool>> MiscellaneousInputs = [];

        public BindInputs()
        {

        }

        public BindInputs(List<Keys> keyboardInputs = null, List<Buttons> gamePadInputs = null, List<Func<bool>> miscInputs = null)
        {
            KeyboardInputs = keyboardInputs ?? [];
            GamePadInputs = gamePadInputs ?? [];
            MiscellaneousInputs = miscInputs ?? [];
        }

        public void Clear()
        {
            KeyboardInputs.Clear();
            GamePadInputs.Clear();
            MiscellaneousInputs.Clear();
        }
    }

    public class InputBindingContext<T> where T : Enum
    {
        private class Binding(BindInputs inputs)
        {
            public BindInputs Inputs = inputs ?? new();

            private bool _isPressed;
            public bool IsPressed { get { return _isPressed; } }
            private bool _justPressed;
            public bool JustPressed { get { return _justPressed; } }
            private bool _justReleased;
            public bool JustReleased { get { return _justReleased; } }

            public void ResetState()
            {
                _isPressed = false;
                _justPressed = false;
                _justReleased = false;
            }

            public void SetState(bool pressed, bool prevPressed)
            {
                _isPressed |= pressed;
                _justPressed |= pressed && !prevPressed;
                _justReleased |= !pressed && prevPressed;
            }
        }

        public UIWindow Window { get; private set; }
        private readonly bool _useMainWindow;
        public WindowKeyboardContext Keyboard { get => Window?.Keyboard; }

        private WindowKeyboardState _prevKeyboardState;
        private GamePadState _prevGamePadState;
        private Dictionary<Func<bool>, bool> _prevFuncReturns = [];

        private readonly Dictionary<T, Binding> _bindings;

        public InputBindingContext(UIWindow window)
        {
            Window = window;
            _bindings = [];
        }
        public InputBindingContext() : this(InputManager.MainWindow) { _useMainWindow = true; }

        public InputBindingContext(Dictionary<T, BindInputs> bindings, UIWindow window)
        {
            Window = window;
            _bindings = [];

            foreach (KeyValuePair<T, BindInputs> pair in bindings)
            {
                T key = pair.Key;
                BindInputs value = pair.Value;

                _bindings[key] = new Binding(value);
            }
        }
        public InputBindingContext(Dictionary<T, BindInputs> bindings) : this(bindings, InputManager.MainWindow) { _useMainWindow = true; }

        public void BindInput(T bind, BindInputs inputs)
        {
            _bindings[bind] = new(inputs);
        }

        public void UnbindInput(T bind)
        {
            _bindings.Remove(bind);
        }

        public BindInputs GetBindInputs(T bind)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                return binding.Inputs;
            
            return new();
        }

        public List<Keys> GetKeyboardInputsFromBind(T bind)
        {
            if ( _bindings.TryGetValue(bind, out Binding binding))
                return binding.Inputs.KeyboardInputs;

            return [];
        }

        public List<Buttons> GetGamePadInputsFromBind(T bind)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                return binding.Inputs.GamePadInputs;

            return [];
        }

        public List<Func<bool>> GetMiscellaneousInputsFromBind(T bind)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                return binding.Inputs.MiscellaneousInputs;

            return [];
        }

        public void AddInputToBind(T bind, Keys keyboardKey)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
            {
                List<Keys> keyboardInputs = binding.Inputs.KeyboardInputs;

                if (!keyboardInputs.Contains(keyboardKey))
                    keyboardInputs.Add(keyboardKey);
            }
            else
            {
                BindInputs inputs = new();
                inputs.KeyboardInputs.Add(keyboardKey);

                _bindings[bind] = new(inputs);
            }
        }

        public void AddInputToBind(T bind, Buttons gamePadButton)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
            {
                List<Buttons> gamePadInputs = binding.Inputs.GamePadInputs;

                if (!gamePadInputs.Contains(gamePadButton))
                    gamePadInputs.Add(gamePadButton);
            }
            else
            {
                BindInputs inputs = new();
                inputs.GamePadInputs.Add(gamePadButton);

                _bindings[bind] = new(inputs);
            }
        }

        public void AddInputToBind(T bind, Func<bool> miscInput)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
            {
                List<Func<bool>> miscInputs = binding.Inputs.MiscellaneousInputs;

                if (!miscInputs.Contains(miscInput))
                    miscInputs.Add(miscInput);
            }
            else
            {
                BindInputs inputs = new();
                inputs.MiscellaneousInputs.Add(miscInput);

                _bindings[bind] = new(inputs);
            }
        }

        public void RemoveInputFromBind(T bind, Keys keyboardKey)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                binding.Inputs.KeyboardInputs.Remove(keyboardKey);
        }

        public void RemoveInputFromBind(T bind, Buttons gamePadButton)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                binding.Inputs.GamePadInputs.Remove(gamePadButton);
        }

        public void RemoveInputFromBind(T bind, Func<bool> miscInput)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                binding.Inputs.MiscellaneousInputs.Remove(miscInput);
        }

        public void ClearAllInputsFromBind(T bind)
        {
            if (_bindings.TryGetValue(bind, out Binding binding))
                binding.Inputs.Clear();
        }

        public void UpdateInputStates()
        {
            if (_useMainWindow)
                Window = InputManager.MainWindow;

            WindowKeyboardState keyboardState = (_useMainWindow && Window == null) ? 
                new([], WindowKeyboardContext.GetGlobalState().CapsLock) : 
                Keyboard != null ? Keyboard.GetState() : WindowKeyboardContext.GetGlobalState();

            GamePadState gamePadState = GamePad.GetState(0);

            Dictionary<Func<bool>, bool> funcReturns = [];

            foreach (Binding binding in _bindings.Values)
            {
                binding.ResetState();

                BindInputs inputs = binding.Inputs;

                foreach (Keys key in inputs.KeyboardInputs)
                {
                    bool pressed = keyboardState.IsKeyDown(key);
                    bool prevPressed = _prevKeyboardState.IsKeyDown(key);

                    binding.SetState(pressed, prevPressed);
                }

                foreach (Buttons button in inputs.GamePadInputs)
                {
                    bool pressed = gamePadState.IsButtonDown(button);
                    bool prevPressed = _prevGamePadState.IsButtonDown(button);

                    binding.SetState(pressed, prevPressed);
                }

                foreach (Func<bool> func in inputs.MiscellaneousInputs)
                {
                    bool pressed = func();
                    _prevFuncReturns.TryGetValue(func, out bool prevPressed);

                    binding.SetState(pressed, prevPressed);
                    funcReturns[func] = pressed;
                }
            }

            _prevKeyboardState = keyboardState;
            _prevGamePadState = gamePadState;
            _prevFuncReturns = funcReturns;
        }

        public bool IsBindPressed(T bind)
        {
            return _bindings.ContainsKey(bind) && _bindings[bind].IsPressed;
        }

        public bool WasBindJustPressed(T bind)
        {
            return _bindings.ContainsKey(bind) && _bindings[bind].JustPressed;
        }

        public bool WasBindJustReleased (T bind)
        {
            return _bindings.ContainsKey(bind) && _bindings[bind].JustReleased;
        }
    }
}
