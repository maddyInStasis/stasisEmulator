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

        private UIWindow _mainWindow;
        private UIEmulatorDisplay _mainDisplay;

        private UIMessageHandler _messageHandler;

        private UIMenuItem _debugDropdown;

        private UIMenuItem _openRomButton;

        private UIMenuItem _showFrameTimeButton;

        private IEmulatorCore _emulatorCore;
        private readonly SaveState[] _saveStates = new SaveState[10];
        private int _saveSlot = 0;

        private float _playbackSpeed = 1;
        public float PlaybackSpeed 
        { 
            get => _playbackSpeed; 
            set 
            { 
                _playbackSpeed = value;

                if (_emulatorCore != null)
                    _emulatorCore.AudioOutput.PlaybackSpeed = value;
            } 
        }

        private List<float> _speeds = [1/64f, 1/32f, 1/16f, 1/8f, 1/4f, 1/2f, 0.75f, 1, 1.5f, 2f, 3f];
        private int _speedIndex;

        private float _framesToRun = 0;

        private enum EmulatorControl
        {
            Pause,
            Modifier,
            Reset,

            Save,
            Load,
            PrevSave,
            NextSave,

            SpeedUp,
            SpeedDown,
        }

        private readonly InputBindingContext<EmulatorControl> _emulatorControls = new(bindings: new(){
            { EmulatorControl.Pause, new([Microsoft.Xna.Framework.Input.Keys.Escape]) },
            { EmulatorControl.Modifier, new([Microsoft.Xna.Framework.Input.Keys.LeftControl]) },
            { EmulatorControl.Reset, new([Microsoft.Xna.Framework.Input.Keys.R]) },

            { EmulatorControl.Save, new([Microsoft.Xna.Framework.Input.Keys.C]) },
            { EmulatorControl.Load, new([Microsoft.Xna.Framework.Input.Keys.V]) },
            { EmulatorControl.PrevSave, new([Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets]) },
            { EmulatorControl.NextSave, new([Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets]) },

            { EmulatorControl.SpeedUp, new([Microsoft.Xna.Framework.Input.Keys.OemPlus]) },
            { EmulatorControl.SpeedDown, new([Microsoft.Xna.Framework.Input.Keys.OemMinus]) },
        }, null);

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

            _speedIndex = _speeds.IndexOf(1);
            PlaybackSpeed = _speeds[_speedIndex];

            base.Initialize();
        }

        private void ClearSaveStates()
        {
            for (int i = 0; i < _saveStates.Length; i++)
            {
                _saveStates[i] = null;
            }
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

                //save states from other roms are not relevant
                ClearSaveStates();
            }
            catch (Exception e)
            {
                _messageHandler.AddMessage($"Error loading ROM: {e.Message}");
            }
        }

        private void SetupCore(IEmulatorCore newCore)
        {
            _emulatorCore = newCore;
            _emulatorCore.AudioOutput.PlaybackSpeed = PlaybackSpeed;
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

            _showFrameTimeButton = CreateDropDownItem("Show Frame Time");
            _showFrameTimeButton.Click += (sender, e) => _mainDisplay.OutputFrameTime = !_mainDisplay.OutputFrameTime;


            _messageHandler = new UIMessageHandler()
            {
                Width = UISize.Grow(),
                Height = UISize.Grow(),
                MessageBackgroundColor = new Color(25, 25, 40),
                MessageTextColor = Color.White
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
            _emulatorCore?.Unload();
        }

        protected override void Update(GameTime gameTime)
        {
            InputManager.MainWindowPosition = Window.Position;
            InputManager.Update();
            UIWindow.UpdateWindows(gameTime);
            UIMenuItem.StaticUpdate(gameTime);

            _emulatorControls.UpdateInputStates();

            if (_emulatorCore != null)
            {
                if (_emulatorControls.WasBindJustPressed(EmulatorControl.Pause))
                {
                    _emulatorCore.TogglePause();
                }

                if (_emulatorControls.WasBindJustPressed(EmulatorControl.PrevSave))
                {
                    _saveSlot--;
                    if (_saveSlot < 0)
                        _saveSlot = _saveStates.Length - 1;

                    _messageHandler.AddMessage($"Switched to slot {_saveSlot}");
                }
                if (_emulatorControls.WasBindJustPressed(EmulatorControl.NextSave))
                {
                    _saveSlot++;
                    if (_saveSlot > _saveStates.Length - 1)
                        _saveSlot = 0;

                    _messageHandler.AddMessage($"Switched to slot {_saveSlot}");
                }

                if (_emulatorControls.WasBindJustPressed(EmulatorControl.Save))
                {
                    _saveStates[_saveSlot] = _emulatorCore.SaveState();
                    _messageHandler.AddMessage($"Saved to slot {_saveSlot}");
                }
                if (_emulatorControls.WasBindJustPressed(EmulatorControl.Load))
                {
                    var state = _saveStates[_saveSlot];
                    string message = $"No state to load in slot {_saveSlot}";

                    if (state != null)
                    {
                        _emulatorCore.LoadState(state);
                        message = $"Loaded from slot {_saveSlot}";
                    }

                    _messageHandler.AddMessage(message);
                }

                if (_emulatorControls.WasBindJustPressed(EmulatorControl.SpeedUp))
                {
                    _speedIndex = Math.Min(_speedIndex + 1, _speeds.Count - 1);
                    PlaybackSpeed = _speeds[_speedIndex];
                    _messageHandler.AddMessage($"Speed: {PlaybackSpeed * 100:n1}%");
                }

                if (_emulatorControls.WasBindJustPressed(EmulatorControl.SpeedDown))
                {
                    _speedIndex = Math.Max(_speedIndex - 1, 0);
                    PlaybackSpeed = _speeds[_speedIndex];
                    _messageHandler.AddMessage($"Speed: {PlaybackSpeed * 100:n1}%");
                }

                if (_emulatorControls.IsBindPressed(EmulatorControl.Modifier) && _emulatorControls.WasBindJustPressed(EmulatorControl.Reset))
                {
                    _emulatorCore.Reset();
                }

                _framesToRun += _playbackSpeed;
                
                while (_framesToRun >= 1)
                {
                    _emulatorCore.RunFrame();
                    _framesToRun--;
                }
            }

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
