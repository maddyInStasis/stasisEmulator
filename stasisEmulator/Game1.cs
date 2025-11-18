using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.UI.Controls;
using stasisEmulator.NesConsole;
using stasisEmulator.NesConsole.Mappers;
using stasisEmulator.Input;
using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using stasisEmulator.UI.Windows;
using System.Collections.Generic;

namespace stasisEmulator
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private OpenFileDialog _currentDialog;

        private UIWindow _mainWindow;
        private UIEmulatorDisplay _mainDisplay;

        private TraceLogWindow _tracelogger;
        private MemoryViewerWindow _memoryViewer;
        private PatternViewerWindow _patternViewer;
        private NametableViewerWindow _nametableViewer;

        private UIMessageHandler _messageHandler;

        private UIMenuItem _openButton;

        private UIMenuItem _traceloggerButton;
        private UIMenuItem _memoryViewerButton;
        private UIMenuItem _patternViewerButton;
        private UIMenuItem _nametableViewerButton;

        private UIMenuItem _showFrameTimeButton;

        private Nes _nes;

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

            _nes = new();
            //please don't lag when out of focus thank you
            InactiveSleepTime = new();

            Window.AllowUserResizing = true;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            path += @"\Roms\nes-test-roms-master\AccuracyCoin.nes";
            //path += @"\Roms\nes-test-roms-master\ppu_read_buffer\test_ppu_read_buffer.nes";
            //path += @"\Roms\Best NES Games\Donkey Kong\Donkey Kong (World) (Rev 1).nes";
            //path += @"\Roms\Best NES Games\Mario\Super Mario Bros. (World).nes";
            //path += @"\Roms\Best NES Games\Tetris\Tetris (USA).nes";
            //path += @"\Roms\Best NES Games\Zelda\Legend of Zelda, The (USA) (Rev 1).nes";

            TryLoadRom(path);

            base.Initialize();
        }

        private void TryLoadRom(string path)
        {
            try
            {
                var rom = RomLoader.LoadRom(path);
                var mapper = MapperFactory.CreateMapper(rom, _nes);
                _nes.Cartridge = mapper;
                _nes.Power();
            }
            catch (Exception e)
            {
                _messageHandler.AddMessage($"Error loading ROM: {e.Message}");
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

        //TODO: create UITextButton
        //TODO: make this into its own window class too
        private void CreateUI()
        {
            _mainDisplay = new(_nes)
            {
                Width = UISize.Grow(),
                Height = UISize.Grow(),
                Padding = new(8, 2)
            };

            static UIMenuItem createDropDownItem(string header, List<UIMenuItem> children)
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

            _openButton = createDropDownItem("Open", []);
            _openButton.Click += (sender, e) =>
            {
                if (_currentDialog != null)
                    return;

                var window = NativeWindow.FromHandle(Window.Handle);

                string path = "";
                var t = new Thread(() =>
                {
                    _currentDialog = new()
                    {
                        Title = "Open ROM",
                        Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*"
                    };

                    var result = _currentDialog.ShowDialog(window);
                    path = _currentDialog.FileName;
                    _currentDialog = null;

                    if (result == DialogResult.Cancel)
                        return;

                    if (path == "")
                        return;

                    TryLoadRom(path);
                });

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            };

            _traceloggerButton = createDropDownItem("Trace Logger", []);
            _traceloggerButton.Click += (sender, e) =>
            {
                if (_tracelogger != null)
                {
                    _tracelogger.Focus();
                    return;
                }

                _tracelogger = new TraceLogWindow(_nes, GraphicsDevice, 1280, 720);
                _tracelogger.WindowClosed += (sender, e) => { _tracelogger = null; };
            };

            _memoryViewerButton = createDropDownItem("Memory Viewer", []);
            _memoryViewerButton.Click += (sender, e) =>
            {
                if ( _memoryViewer != null)
                {
                    _memoryViewer.Focus();
                    return;
                }

                _memoryViewer = new MemoryViewerWindow(_nes, GraphicsDevice, 1280, 720);
                _memoryViewer.WindowClosed += (sender, e) => { _memoryViewer = null; };
            };

            _patternViewerButton = createDropDownItem("Pattern Viewer", []);
            _patternViewerButton.Click += (sender, e) =>
            {
                if (_patternViewer != null)
                {
                    _patternViewer.Focus();
                    return;
                }

                _patternViewer = new PatternViewerWindow(_nes, GraphicsDevice, 1280, 720);
                _patternViewer.WindowClosed += (sender, e) => { _patternViewer = null; };
            };

            _nametableViewerButton = createDropDownItem("Nametable Viewer", []);
            _nametableViewerButton.Click += (sender, e) =>
            {
                if (_nametableViewer != null)
                {
                    _nametableViewer.Focus();
                    return;
                }

                _nametableViewer = new NametableViewerWindow(_nes, GraphicsDevice, 1080, 1080);
                _nametableViewer.WindowClosed += (sender, e) => { _nametableViewer = null; };
            };

            _showFrameTimeButton = createDropDownItem("Show Frame Time", []);
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

            _mainWindow = new UIWindow(Window, GraphicsDevice, [
                new UIRectangle([
                    createTopItem("File", [_openButton]),
                    createTopItem("Debug", [
                        _traceloggerButton,
                        _memoryViewerButton,
                        _patternViewerButton,
                        _nametableViewerButton,
                    ]),
                    createTopItem("View", [_showFrameTimeButton]),
                    //TODO: PLS MAKE A GITHUB REPO

                    //erm wait, since i have commits stored locally, will those file path strings show up if i upload to github?
                    //see: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository
                    //if clicking a few buttons to load the file becomes too cumbersome, perhaps you could store the paths of recent files
                    //(basically either add an "Open Recent" menuitem or just copy what Mesen does)

                    //READ ME!!!!!!!!!!!!!!
                    //TODO: create memory viewer to figure out wtf is happening to the stack during the b flag test
                    //manually modifying the code to enable/disable debug views is getting pretty impractical, so now would be a good time to add secondary windows
                    //(and also obviously the instruction advance should belong to the trace viewer and not be triggered by a button press, add a dialog or something
                    //to advance specified amount)
                    //add debug cpu read and ppu read functions
                    //also add dmc!!!!!
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
            _nes.Apu.IsRunning = false;
        }

        protected override void Update(GameTime gameTime)
        {
            InputManager.MainWindowPosition = Window.Position;
            InputManager.Update();
            UIWindow.UpdateWindows(gameTime);
            UIMenuItem.StaticUpdate(gameTime);
            _nes.RunFrame();

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
