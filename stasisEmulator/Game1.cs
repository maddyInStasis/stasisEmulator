using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stasisEmulator.UI.Controls;
using stasisEmulator.NesConsole;
using System.IO;
using stasisEmulator.NesConsole.Cartridges;
using System.Collections.Generic;
using stasisEmulator.Input;
using System;
using System.Windows.Forms;
using System.Threading;

namespace stasisEmulator
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private UIWindow _root;
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

            Window.AllowUserResizing = true;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            //path += @"\Roms\nes-test-roms-master\ppu_read_buffer\test_ppu_read_buffer.nes";
            path += @"\Roms\Best NES Games\Donkey Kong\Donkey Kong (World) (Rev 1).nes";

            _nes.Cartridge = CartridgeFactory.CreateCartridge(RomLoader.LoadRom(path));

            base.Initialize();
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

            _openButton = new()
            {
                Text = "Open",
                Padding = new(4, 2)
            };

            _openButton.Click += (sender, e) => 
            {
                //don't really like how we're freezing the main thread until the file is selected...
                string path = "";
                var t = new Thread(() =>
                {
                    OpenFileDialog dialog = new()
                    {
                        Title = "Open ROM",
                        Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*"
                    };
                    var result = dialog.ShowDialog();

                    if (result == DialogResult.Cancel)
                        return;

                    path = dialog.FileName;
                });

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();

                if (path == "")
                    return;

                _nes.Cartridge = CartridgeFactory.CreateCartridge(RomLoader.LoadRom(path));
                _nes.Power();
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
                    //todo: PLS MAKE A GITHUB REPO

                    //OH ALSO you probably don't want to include your personal local file paths in the github repo :P
                    //TODO: remove "C:\Users\[user]" path strings
                    //erm wait, since i have commits stored locally, will those file path strings show up if i upload to github?
                    //see: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository
                    //if clicking a few buttons to load the file becomes too cumbersome, perhaps you could store the paths of recent files
                    //(basically either add an "Open Recent" menuitem or just copy what Mesen does)
                    
                    //why are all of these comments in the children parameter of this UIRectangle

                    //todo: create menuitem, use to create topbar, create open file option, then try to run some other timing tests
                    //menuitem is just a button which shows and enables a container of other menuitems when clicked
                    //if the parent of menuitem is another menuitem, extend horizontally rather than vertically
                    //since the dropdown isn't part of the main ui layout, it needs to be laid out and drawn as a top level container within the menuitem code
                    //tbh, the main thing i'm not sure about is how to handle the parent-child relationship here
                    //i was thinking that the dropdown could be an actual container to allow for the auto layout to fill the menuitems top to bottom
                    //but that might be redundant, and would certainly complicate setting up the children,
                    //and the aforementioned "if parent is menuitem, extend horizontally"
                    //so perhaps the menu items need to be laid out and drawn individually
                    //this might mean some kind of "DontLayoutDescendants" and/or "DontDrawDescendants" flags may be necessary
                    //i say "and/or" because if i can freely set the positions of the menuitems, drawing them automatically is fine
                    //actually, positioning is really the only step which needs to be overridden
                    //hmmmmm...

                    //important additional note!! the menuitems should ideally grow to fit the size of the other menuitems, with some padding and a minimum size
                    //so positioning is definitely not the only auto-layout feature i would be sacrificing
                    //may need to think about this more...

                    //since the primary way of adding children is through the constructor, couldn't we just have the constructor take in the menuitems
                    //and not necessarily the actual children? like, use a container for the dropdown so we get the nice auto layout stuff, then have
                    //the constructor add the list of UIControls to that instead of the menuitem itself. also, in the constructor of menuitem, default
                    //the size mode to Grow if it is contained within a menuitem dropdown (should UIMenuItemDropdown be its own class to make this check nicer?)
                    //so basically, menuitem is ChildrenLocked = true, and the constructor treats the list as children of the dropdown container rather than
                    //the menuitem. can add an AddMenuItem method if it ends up being necessary
                    //actually that probably will end up being necessary since the "Open Recent" menuitem will probably want to add new menuitems for recently
                    //opened files at runtime (e.g. if you open a rom, it should then show up under recent without having to relaunch the emulator)
                    //also, this would be a good time to replace the exceptions in the rom loader with error messages instead
                    
                    //also, yes, menuitem is a button, but it should also contain an image icon and text (if they are specified)
                    //also might want to add some properties for the dropdown as part of the menuitem
                    
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
                        Height = UISize.Grow()
                    },/*
                    new UINametableDisplay(_nes)
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
            ])
            {
                FillDirection = FillDirection.TopToBottom
            };
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
