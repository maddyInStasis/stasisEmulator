using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    public class UIPatternDisplay : UIControl
    {
        public Nes Nes { get; set; }

        public Color BackgroundColor { get; set; } = Color.Black;
        public Color BorderColor { get; set; } = Color.LightGray;
        public int BorderThickness { get; set; } = 0;

        public bool Grayscale { get; set; } = false;
        public byte Palette { get; set; } = 0;

        private RenderTarget2D _patternRenderTarget;
        private readonly Color[] _colors = new Color[256 * 128];
        private RenderTarget2D _outputRenderTarget;

        public UIPatternDisplay(Nes nes) { Init(nes); }
        public UIPatternDisplay(Nes nes, UIControl parent) : base(parent) { Init(nes); }

        private void Init(Nes nes)
        {
            Nes = nes;
            ChildrenLocked = true;
        }

        protected override void RenderElementContents(SpriteBatch spriteBatch)
        {
            var graphics = spriteBatch.GraphicsDevice;
            _patternRenderTarget ??= new(graphics, 256, 128);
            graphics.SetRenderTarget(_patternRenderTarget);
            graphics.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            //draw final pattern table to output texture, end spritebatch and rendertarget
            void Present()
            {
                if (_outputRenderTarget == null || _outputRenderTarget.Bounds.Size != Bounds.Size)
                    _outputRenderTarget = new(graphics, Bounds.Width, Bounds.Height);

                graphics.SetRenderTarget(_outputRenderTarget);
                spriteBatch.Draw(_patternRenderTarget, FitRectangle(_patternRenderTarget.Bounds, _outputRenderTarget.Bounds), Color.White);
                spriteBatch.End();
                graphics.SetRenderTarget(null);
            }

            var cart = Nes.Mapper;
            if (cart == null)
            {
                Present();
                return;
            }

            void SetPixel(int x, int y, Color color)
            {
                _colors[x + y * _patternRenderTarget.Width] = color;
            }

            for (int table = 0; table < 2; table++)
            {
                for (int row = 0; row < 16; row++)
                {
                    for (int column = 0; column < 16; column++)
                    {
                        for (int y = 0; y < 8;  y++)
                        {
                            byte lowByte = Nes.Ppu.DebugRead((ushort)(y + column * 16 + row * 256 + table * 4096));
                            byte highByte = Nes.Ppu.DebugRead((ushort)(8 + y + column * 16 + row * 256 + table * 4096));
                            for (int x = 0; x < 8; x++)
                            {
                                int paletteIndex = (lowByte >> (7 - x)) & 1;
                                paletteIndex += ((highByte >> (7 - x)) & 1) * 2;
                                Color color;
                                if (Grayscale)
                                    color = new(paletteIndex * 85, paletteIndex * 85, paletteIndex * 85);
                                else
                                    color = Nes.Ppu.Palette[Nes.Ppu.PaletteRam[paletteIndex != 0 ? Palette * 4 + paletteIndex : 0] & 0x3F];

                                SetPixel(x + column * 8 + table * 128, y + row * 8, color);
                            }
                        }
                    }
                }
            }

            _patternRenderTarget.SetData(_colors);

            Present();
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);
            spriteBatch.Draw(_outputRenderTarget, Bounds, Color.White);

            var blank = GetBlankTexture(spriteBatch);
            var patternBounds = FitRectangle(_patternRenderTarget.Bounds, Bounds);

            for (int x = 0; x <= 32; x++)
            {
                spriteBatch.Draw(blank, new Rectangle((int)Math.Round(patternBounds.X + patternBounds.Width / 32f * x) - (x == 32 ? BorderThickness : 0), patternBounds.Y, BorderThickness, patternBounds.Height), BorderColor);
            }

            for (int y = 0; y <= 16; y++)
            {
                spriteBatch.Draw(blank, new Rectangle(patternBounds.X, (int)Math.Round(patternBounds.Y + patternBounds.Height / 16f * y) - (y == 16 ? BorderThickness : 0), patternBounds.Width, BorderThickness), BorderColor);
            }
        }
    }
}
