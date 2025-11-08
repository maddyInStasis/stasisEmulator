using FontStashSharp;
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

        public bool OutputFrameTime { get; set; }

        public FontSystem Font { get; set; }

        //for some reason, FontStashSharp's font sizes are really small. a factor of 1.75 seems to scale to the correct size
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 12;

        public Color TextColor { get; set; } = Color.Black;
        public Color TextBackgroundColor { get; set; } = Color.White;

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

            if (!OutputFrameTime)
                return;

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            if (spriteFont == null)
                return;

            double _cpuElapsedTime = Nes.CpuWatch.Elapsed.TotalMilliseconds;
            double _ppuElapsedTime = Nes.PpuWatch.Elapsed.TotalMilliseconds;
            double _apuElapsedTime = Nes.ApuWatch.Elapsed.TotalMilliseconds;

            double total = Nes.FrameElapsedTime / Nes.FrameCount;

            string text = "";

            if (_cpuElapsedTime > 0)
            {
                text += $"CPU: {_cpuElapsedTime / Nes.FrameCount:n3} ms" +
                $"\nPPU: {_ppuElapsedTime / Nes.FrameCount:n3} ms" +
                $"\nAPU: {_apuElapsedTime / Nes.FrameCount:n3} ms";
            }
                
            text += $"{(text.Length > 0 ? "\n" : "")}Total: {total:n3} ms" +
                $"\nFrame Percentage: {total / (1f / 60 * 1000) * 100:n1}% (Total / 60fps)";

            float width = spriteFont.MeasureString(text).X;
            float height = MeasureStringHeightCorrected(spriteFont, text);
            var blank = GetBlankTexture(spriteBatch);

            spriteBatch.Draw(blank, new Rectangle(Bounds.X, Bounds.Y, (int)(width + Padding.HorizontalTotal), (int)(height + Padding.VerticalTotal)), TextBackgroundColor);
            spriteBatch.DrawString(spriteFont, text, new(Bounds.X + Padding.Left, Bounds.Y + Padding.Top), TextColor);
        }
    }
}
