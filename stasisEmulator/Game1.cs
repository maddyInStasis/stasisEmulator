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

namespace stasisEmulator
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private OpenFileDialog _currentDialog;

        private UIWindow _root;
        private UIMessageHandler _messageHandler;
        private UIMenuItem _openButton;

        private readonly Nes _nes = new();

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
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            //path += @"\Roms\nes-test-roms-master\AccuracyCoin.nes";
            //path += @"\Roms\nes-test-roms-master\ppu_read_buffer\test_ppu_read_buffer.nes";
            //path += @"\Roms\Best NES Games\Donkey Kong\Donkey Kong (World) (Rev 1).nes";
            path += @"\Roms\Best NES Games\Mario\Super Mario Bros. (World).nes";

            TryLoadRom(path);

            base.Initialize();
        }

        private void TryLoadRom(string path)
        {
            try
            {
                var rom = RomLoader.LoadRom(path);
                var mapper = MapperFactory.CreateMapper(rom);
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

        private void CreateUI()
        {
            _openButton = new()
            {
                Text = "Open",
                Padding = new(4, 2)
            };

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

            _messageHandler = new UIMessageHandler()
            {
                Width = UISize.Grow(),
                Height = UISize.Grow()
            };

            _root = new(GraphicsDevice, [
                new UIRectangle([
                    new UIMenuItem([
                        _openButton,
                        new()
                        {
                            Text = "Open Recent >",
                            Padding = new(4, 2)
                        }
                    ])
                    {
                        Text = "File",
                        Padding = new(4, 2)
                    },
                    new UIMenuItem([
                        new()
                        {
                            Text = "Trace Logger",
                            Padding = new(4, 2)
                        },
                        new()
                        {
                            Text = "Pattern Viewer",
                            Padding = new(4, 2)
                        },
                        new()
                        {
                            Text = "Nametable Viewer",
                            Padding = new(4, 2)
                        },
                    ])
                    {
                        Text = "Debug",
                        Padding = new(4, 2)
                    }
                    //TODO: PLS MAKE A GITHUB REPO

                    //erm wait, since i have commits stored locally, will those file path strings show up if i upload to github?
                    //see: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository
                    //if clicking a few buttons to load the file becomes too cumbersome, perhaps you could store the paths of recent files
                    //(basically either add an "Open Recent" menuitem or just copy what Mesen does)
                    
                    //why are all of these comments in the children parameter of this UIRectangle
                    
                    //at some point, i will also probably want to start looking into secondary windows for debug stuff
                    //instead of shoving everything into the main window
                    //unfortunately, this means trying to figure out the best way to handle SwapChainRenderTarget, which i don't even fully understand
                    //perhaps it's just like a normal rendertarget, but the "texture" being drawn to is the window
                    //idk
                ])
                {
                    Width = UISize.Grow(),
                    Height = UISize.Fit(),
                    BackgroundColor = new Color(25, 25, 40),
                },

                new UIRectangle([
                    new UIEmulatorDisplay(_nes)
                    {
                        Width = UISize.Grow(),
                        Height = UISize.Grow(),
                        Padding = new(8, 2),
                        OutputFrameTime = true
                    },/*
                    new UINametableDisplay(_nes)
                    {
                        Width = UISize.Grow(),
                        Height = UISize.Grow()
                    },
                    new UIPatternDisplay(_nes)
                    {
                        Width = UISize.Grow(),
                        Height = UISize.Grow()
                    }*/
                ])
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow()
                },/*
                new UITraceLogDisplay(_nes.Cpu.TraceLogger)
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow(max:305),
                    BackgroundColor = new Color(25, 25, 40),
                    TextColor = new Color(25, 179, 184),
                    BorderColor = new Color(64, 64, 102),
                    ScrollBarTrackColor = new Color(25, 25, 40),
                    ScrollBarTrackDisabledColor = new Color(25, 25, 40),
                    ScrollBarThumbIdleColor = new Color(25, 179, 184),
                    ScrollBarThumbHoverColor = new Color(0, 146, 150),
                    ScrollBarThumbDragColor = new Color(0, 87, 91),
                    ShowScrollBarButtons = false
                },*/
                _messageHandler,
            ])
            {
                FillDirection = FillDirection.TopToBottom
            };
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            _nes.Apu.IsRunning = false;
        }

        protected override void Update(GameTime gameTime)
        {
            InputManager.Update();
            _root.Update(gameTime);
            UIMenuItem.StaticUpdate(gameTime);
            _nes.RunFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            _root.Render(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
