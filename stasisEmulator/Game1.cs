using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stasisEmulator.UI.Controls;
using stasisEmulator.NesConsole;
using System.IO;
using stasisEmulator.NesConsole.Cartridges;
using System.Collections.Generic;

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

            _nes.Cartridge = new Nrom(RomLoader.LoadRom(@"C:\Users\Jacob\Downloads\TestRoms\__PatreonRoms\5_Instructions1.nes"));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new(GraphicsDevice);

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
            ]);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _testRoot.Update();
            _nes.RunFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            _spriteBatch.Begin();
            _testRoot.Render(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
