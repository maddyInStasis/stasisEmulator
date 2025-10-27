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

namespace stasisEmulator
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private UIWindow _testRoot;

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

            //_nes.Cartridge = new Nrom(RomLoader.LoadRom(@"C:\Users\Jacob\Downloads\TestRoms\__PatreonRoms\7_Graphics.nes"));
            //_nes.Cartridge = new Nrom(RomLoader.LoadRom(@"C:\Users\Jacob\Documents\Roms\Best NES Games\Mario\Super Mario Bros. (World).nes"));
            _nes.Cartridge = new Nrom(RomLoader.LoadRom(@"C:\Users\Jacob\Downloads\AccuracyCoin.nes"));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new(GraphicsDevice);

            //TODO: this should probably just be looking in C:/Windows/Fonts? idk how you're meant to do it tbh
            FontSystem mainFontSystem = new();
            mainFontSystem.AddFont(File.ReadAllBytes(@"Content/Fonts/SEGOEUI.ttf"));
            AssetManager.Fonts["MainFont"] = mainFontSystem;
            AssetManager.DefaultFont = mainFontSystem;

            FontSystem mainMonospaceFontSystem = new();
            mainMonospaceFontSystem.AddFont(File.ReadAllBytes(@"Content/Fonts/CONSOLA.ttf"));
            AssetManager.Fonts["MainMonospaceFont"] = mainMonospaceFontSystem;
            AssetManager.DefaultMonospaceFont = mainMonospaceFontSystem;

            _testRoot = new(GraphicsDevice, [
                new UITraceLogDisplay(_nes.Cpu.TraceLogger)
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow(),
                    BackgroundColor = new Color(25, 25, 40),
                    TextColor = new Color(25, 179, 184),
                    BorderColor = new Color(64, 64, 102),
                    ScrollBarTrackColor = new Color(25, 25, 40),
                    ScrollBarTrackDisabledColor = new Color(25, 25, 40),
                    ScrollBarThumbIdleColor = new Color(25, 179, 184),
                    ScrollBarThumbHoverColor = new Color(0, 146, 150),
                    ScrollBarThumbDragColor = new Color(0, 87, 91),
                    ShowScrollBarButtons = false
                },
                new UINametableDisplay(_nes)
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow()
                }
            ])
            {

            };
        }

        protected override void Update(GameTime gameTime)
        {
            InputManager.Update();
            _testRoot.Update(gameTime);
            _nes.RunFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            _testRoot.Render(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
