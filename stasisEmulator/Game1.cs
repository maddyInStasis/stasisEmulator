using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.UI.Controls;
using stasisEmulator.NesCore;
using stasisEmulator.Input;
using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;

namespace stasisEmulator
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private OpenFileDialog _currentRomDialog;
        private OpenFileDialog _currentTasDialog;

        private UIWindow _mainWindow;
        private UIEmulatorDisplay _mainDisplay;

        private UIMessageHandler _messageHandler;

        private UIMenuItem _debugDropdown;

        private UIMenuItem _openRomButton;
        private UIMenuItem _openTasButton;

        private UIMenuItem _tasRestart;
        private UIMenuItem _tasStop;

        private UIMenuItem _showFrameTimeButton;

        private IEmulatorCore _emulatorCore;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _graphics.ApplyChanges();
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();

            //please don't lag when out of focus thank you
            InactiveSleepTime = new();

            Window.AllowUserResizing = true;

            base.Initialize();
        }

        private void TryLoadRom(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLower();
                IEmulatorCore newCore = _emulatorCore;

                switch (ext)
                {
                    case ".nes":
                        if (_emulatorCore == null || _emulatorCore.ConsoleType != ConsoleType.Nes)
                            newCore = new Nes(GraphicsDevice);
                        break;
                    default:
                        throw new Exception($"Unknown ROM file type: \"{ext}\"");
                }

                newCore?.LoadRom(path);

                if (newCore != _emulatorCore)
                {
                    _emulatorCore?.Unload();
                    SetupCore(newCore);
                }
            }
            catch (Exception e)
            {
                _messageHandler.AddMessage($"Error loading ROM: {e.Message}");
            }
        }

        private void SetupCore(IEmulatorCore newCore)
        {
            _emulatorCore = newCore;
            _mainDisplay.EmulatorCore = newCore;

            _debugDropdown.ClearMenuItems();

            foreach(var kv in _emulatorCore.DebugOptions)
            {
                var header = kv.Key;
                var action = kv.Value;

                var item = CreateDropDownItem(header);
                item.Click += (sender, e) => { action.Invoke(); };
                _debugDropdown.AddMenuItem(item);
            }
        }

        private void TryLoadTas(string path)
        {
            try
            {
                _emulatorCore?.LoadTas(path);
            }
            catch (Exception e)
            {
                _messageHandler.AddMessage($"Error loading TAS: {e.Message}");
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new(GraphicsDevice);

            string fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            FontSystem mainFontSystem = new();
            mainFontSystem.AddFont(File.ReadAllBytes(Path.Combine(fontsFolder, "SEGOEUI.ttf")));
            AssetManager.Fonts["MainFont"] = mainFontSystem;
            AssetManager.DefaultFont = mainFontSystem;
            
            FontSystem mainMonospaceFontSystem = new();
            mainMonospaceFontSystem.AddFont(File.ReadAllBytes(Path.Combine(fontsFolder, "CONSOLA.ttf")));
            AssetManager.Fonts["MainMonospaceFont"] = mainMonospaceFontSystem;
            AssetManager.DefaultMonospaceFont = mainMonospaceFontSystem;

            CreateUI();
        }

        private static UIMenuItem CreateDropDownItem(string header, List<UIMenuItem> children)
        {
            return new(children)
            {
                Text = header,
                Padding = new(10, 6),
                IdleColor = Color.Transparent,
                HoverColor = Color.White * 0.3f,
                PressColor = Color.White * 0.15f,
                TextColor = Color.White,
                BorderColor = Color.Transparent,
            };
        }
        private static UIMenuItem CreateDropDownItem(string header) { return CreateDropDownItem(header, []); }

        //TODO: create styles
        //TODO: create UITextButton
        //TODO: make this into its own window class too
        private void CreateUI()
        {
            _mainDisplay = new(_emulatorCore)
            {
                Width = UISize.Grow(),
                Height = UISize.Grow(),
                Padding = new(8, 2),
            };

            _openRomButton = CreateDropDownItem("Open");
            _openRomButton.Click += (sender, e) =>
            {
                if (_currentRomDialog != null)
                    return;

                var window = NativeWindow.FromHandle(Window.Handle);

                string path = "";
                var t = new Thread(() =>
                {
                    _currentRomDialog = new()
                    {
                        Title = "Open ROM",
                        Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*"
                    };

                    var result = _currentRomDialog.ShowDialog(window);
                    path = _currentRomDialog.FileName;
                    _currentRomDialog = null;

                    if (result == DialogResult.Cancel)
                        return;

                    if (path == "")
                        return;

                    TryLoadRom(path);
                });

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            };

            _openTasButton = CreateDropDownItem("Open");
            _openTasButton.Click += (sender, e) =>
            {
                if (_currentTasDialog != null)
                    return;

                var window = NativeWindow.FromHandle(Window.Handle);

                string path = "";
                var t = new Thread(() =>
                {
                    _currentTasDialog = new()
                    {
                        Title = "Open Tas File",
                        Filter = "NES TAS files (*.fm2)|*.fm2|All files (*.*)|*.*"
                    };

                    var result = _currentTasDialog.ShowDialog(window);
                    path = _currentTasDialog.FileName;
                    _currentTasDialog = null;

                    if (result == DialogResult.Cancel)
                        return;

                    if (path == "")
                        return;

                    TryLoadTas(path);
                });

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            };

            _tasRestart = CreateDropDownItem("Restart");
            _tasRestart.Click += (sender, e) => { _emulatorCore.RestartTas(); };

            _tasStop = CreateDropDownItem("Stop");
            _tasStop.Click += (sender, e) => { _emulatorCore.StopTas(); };

            _showFrameTimeButton = CreateDropDownItem("Show Frame Time");
            _showFrameTimeButton.Click += (sender, e) => _mainDisplay.OutputFrameTime = !_mainDisplay.OutputFrameTime;
            

            _messageHandler = new UIMessageHandler()
            {
                Width = UISize.Grow(),
                Height = UISize.Grow()
            };

            static UIMenuItem createTopItem(string header, List<UIMenuItem> children)
            {
                return new UIMenuItem(children)
                {
                    Text = header,
                    Padding = new(10, 6),
                    IdleColor = Color.Transparent,
                    HoverColor = Color.White * 0.3f,
                    PressColor = Color.White * 0.15f,
                    TextColor = Color.White,
                    BorderColor = Color.Transparent,
                    DropdownBackgroundColor = new Color(25, 25, 40),
                };
            }

            _debugDropdown = createTopItem("Debug", []);

            _mainWindow = new UIWindow(Window, GraphicsDevice, [
                new UIRectangle([
                    createTopItem("File", [_openRomButton]),
                    createTopItem("Tas", [_openTasButton, _tasRestart, _tasStop]),
                    _debugDropdown,
                    createTopItem("View", [_showFrameTimeButton]),
                ])
                {
                    Width = UISize.Grow(),
                    Height = UISize.Fit(),
                    BackgroundColor = new Color(25, 25, 40),
                },

                new UIRectangle([
                    _mainDisplay,
                ])
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow(),
                    FillDirection = FillDirection.LeftToRight,
                    VerticalContentAlignment = VerticalAlignment.Center,
                },
                _messageHandler,
            ])
            {
                FillDirection = FillDirection.TopToBottom
            };

            InputManager.MainWindow = _mainWindow;
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            _emulatorCore.Unload();
        }

        protected override void Update(GameTime gameTime)
        {
            InputManager.MainWindowPosition = Window.Position;
            InputManager.Update();
            UIWindow.UpdateWindows(gameTime);
            UIMenuItem.StaticUpdate(gameTime);
            _emulatorCore?.RunFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            UIWindow.RenderWindows(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
