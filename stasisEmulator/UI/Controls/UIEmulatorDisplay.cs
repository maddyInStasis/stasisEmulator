using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    public class UIEmulatorDisplay : UIControl
    {
        public Nes Nes { get; set; }
        public Color BackgroundColor { get; set; } = Color.Black;
        public Color BorderColor { get; set; } = Color.LightGray;
        public int BorderThickness { get; set; } = 2;

        private Texture2D _screenTexture;
        private RenderTarget2D _outputRenderTarget;

        public UIEmulatorDisplay(Nes nes) { Init(nes); }
        public UIEmulatorDisplay(Nes nes, UIControl parent) : base(parent) { Init(nes); }

        private void Init(Nes nes)
        {
            Nes = nes;
            ChildrenLocked = true;
        }

        protected override void RenderElementContents(SpriteBatch spriteBatch)
        {
            if (Bounds.Width == 0  || Bounds.Height == 0)
                return;

            var graphics = spriteBatch.GraphicsDevice;
            _screenTexture ??= new(graphics, Ppu.PixelWidth, Ppu.PixelHeight);

            _screenTexture.SetData(Nes.Ppu.OutputBuffer);

            if (_outputRenderTarget == null || _outputRenderTarget.Bounds.Size != Bounds.Size)
                _outputRenderTarget = new(graphics, Bounds.Width, Bounds.Height);

            graphics.SetRenderTarget(_outputRenderTarget);
            graphics.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(_screenTexture, FitRectangle(_screenTexture.Bounds, _outputRenderTarget.Bounds), Color.White);
            spriteBatch.End();
            graphics.SetRenderTarget(null);
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);

            if (_outputRenderTarget == null)
                return;

            spriteBatch.Draw(_outputRenderTarget, Bounds, Color.White);
        }
    }
}
