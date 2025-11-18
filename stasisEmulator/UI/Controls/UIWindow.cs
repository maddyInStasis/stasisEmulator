using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using stasisEmulator.Input;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace stasisEmulator.UI.Controls
{
    //TODO: fix memory leak which occurs when window is resized/closed. possibly has to do with rendertargets?

    //this class represents two things:
    //a ui object which fills the window, creating a convenient window-sized container for ui elements (which was the initial purpose)
    //a way to create secondary windows, since the intended function to do this in monogame does not work. instead, this class uses
    //forms windows, so some extra steps are required to get input working, and there are now two types of window objects to deal with
    public class UIWindow : UIControl
    {
        private string _windowTitle;
        public string Title 
        { 
            get => _windowTitle; 
            set
            {
                _windowTitle = value;

                if (Form != null)
                    Form.Text = value;
                else if (_gameWindow != null)
                    _gameWindow.Title = value;
            }
        }

        public event FormClosedEventHandler WindowClosed { add => Form.FormClosed += value; remove => Form.FormClosed -= value; }

        private static readonly List<UIWindow> _formWindows = [];
        private static readonly List<UIWindow> _mainWindows = [];

        private readonly GraphicsDevice _graphicsDevice;
        private SwapChainRenderTarget _renderTarget;
        private readonly GameWindow _gameWindow;

        private MouseCursor _currentCursor = MouseCursor.Arrow;
        private MouseCursor _targetCursor = MouseCursor.Arrow;

        public MouseCursor Cursor 
        { 
            get => _targetCursor; 
            set
            {
                if (_targetCursor == value)
                    return;

                _targetCursor = value;
            }
        }
        public WindowKeyboardContext Keyboard { get; private set; }

        /// <summary>
        /// The Form instance associated with this UIWindow. Is null for the main GameWindow.
        /// </summary>
        public Form Form { get; private set; }

        //idrk why this is a thing...
        //the title bar, the one pixel border
        //the gradient on the edges???
        //the latter is the only way i can explain the horizontal offset being so large
        private const int HorizontalPadding = 18;
        private const int VerticalPadding = 47;

        public UIWindow(GameWindow gameWindow, GraphicsDevice graphicsDevice) : this(gameWindow, graphicsDevice, []) { }
        public UIWindow(GameWindow gameWindow, GraphicsDevice graphicsDevice, List<UIControl> children) : base()
        {
            _gameWindow = gameWindow;
            _graphicsDevice = graphicsDevice;
            UpdateMaxSize();
            Keyboard = new(this);
            _mainWindows.Add(this);

            AddChildren(children);
        }

        public UIWindow(GraphicsDevice graphicsDevice, int width, int height) : this(graphicsDevice, width, height, []) { }
        public UIWindow(GraphicsDevice graphicsDevice, int width, int height, List<UIControl> children) : base()
        {
            _graphicsDevice = graphicsDevice;
            CreateWindow(width, height);
            UpdateMaxSize();
            Keyboard = new(this);
            _formWindows.Add(this);

            AddChildren(children);
        }

        private void CreateWindow(int width, int height)
        {
            if (Form != null)
                return;

            var form = new Form()
            {
                Width = width,
                Height = height,
            };
            Form = form;
            Form.Show();
            Form.MouseWheel += (sender, e) => 
            {
                InputManager.FormScroll(this, e.Delta);
            };
            WindowClosed += OnWindowClosed;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (_formWindows.Contains(this))
                _formWindows.Remove(this);
            else
                _mainWindows.Remove(this);

            Keyboard.WindowClosing();
        }

        private void UpdateMaxSize()
        {
            int width = Width.Max;
            int height = Height.Max;

            if (Form != null)
            {
                width = Form.Width - HorizontalPadding;
                height = Form.Height - VerticalPadding;
            }
            else if (_graphicsDevice != null)
            {
                width = _graphicsDevice.Viewport.Width;
                height = _graphicsDevice.Viewport.Height;
            }

            SizeLocked = false;
            Width = UISize.Grow(max: width);
            Height = UISize.Grow(max: height);
            SizeLocked = true;
        }

        public void Focus()
        {
            Form?.Focus();
        }

        public Point GetPosition()
        {
            if (Form != null)
                return new Point(Form.Left, Form.Top);
            else if (_gameWindow != null)
                return _gameWindow.Position;

            return Point.Zero;
        }

        public static void UpdateWindows(GameTime gameTime)
        {
            foreach (var window in _mainWindows)
            {
                window.Update(gameTime);
            }

            foreach (var window in _formWindows)
            {
                window.Update(gameTime);
            }
        }

        public static void RenderWindows(SpriteBatch spriteBatch)
        {
            foreach (var window in _formWindows)
            {
                window.Render(spriteBatch);
            }

            foreach (var window in _mainWindows)
            {
                window.Render(spriteBatch);
            }
        }

        private void CreateRenderTarget(int width, int height)
        {
            _renderTarget = new(_graphicsDevice, Form.Handle, Math.Max(width, 1), Math.Max(height, 1));
        }

        protected override void UpdateElementPreLayout(GameTime gameTime)
        {
            UpdateMaxSize();
            Keyboard.Update();
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            UpdateCursor();
        }

        private void UpdateCursor()
        {
            if (_currentCursor != _targetCursor)
            {
                SetCursor(_targetCursor);
                _currentCursor = _targetCursor;
            }
            _targetCursor = MouseCursor.Arrow;
        }

        private static readonly Dictionary<MouseCursor, Cursor> _monogameToFormsCursors = new()
        {
            { MouseCursor.Arrow, Cursors.Arrow },
            { MouseCursor.Crosshair, Cursors.Cross },
            { MouseCursor.Hand, Cursors.Hand },
            { MouseCursor.IBeam, Cursors.IBeam },
            { MouseCursor.No, Cursors.No },
            { MouseCursor.SizeAll, Cursors.SizeAll },
            { MouseCursor.SizeNESW, Cursors.SizeNESW },
            { MouseCursor.SizeNS, Cursors.SizeNS },
            { MouseCursor.SizeNWSE, Cursors.SizeNWSE },
            { MouseCursor.SizeWE, Cursors.SizeWE },
            { MouseCursor.Wait, Cursors.WaitCursor }
        };

        private void SetCursor(MouseCursor cursor)
        {
            if (Form == null)
            {
                Mouse.SetCursor(cursor);
                return;
            }

            //good enough i guess
            //TODO: this needs to be in the setter instead i think
            if (!Form.Focused && InputManager.MainWindow != null && InputManager.MainWindow.Bounds.Contains(InputManager.GetWindowMousePosition(InputManager.MainWindow)))
                return;

            if (_monogameToFormsCursors.TryGetValue(cursor, out Cursor formCursor))
                Form.Cursor = formCursor;
            else
                Form.Cursor = Cursors.Arrow;
        }
        
        public override void Render(SpriteBatch spriteBatch)
        {
            if (Form == null)
            {
                base.Render(spriteBatch);
                return;
            }

            int expectedWidth = Form.Width - HorizontalPadding;
            int expectedHeight = Form.Height - VerticalPadding;

            if (_renderTarget == null || _renderTarget.Width != expectedWidth || _renderTarget.Height != expectedHeight)
                CreateRenderTarget(expectedWidth, expectedHeight);

            TargetRender(spriteBatch, _renderTarget);
            _renderTarget.Present();
        }
    }
}
