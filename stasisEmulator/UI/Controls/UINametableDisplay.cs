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
    public class UINametableDisplay : UIControl
    {
        public Nes Nes { get; set; }
        public Color BackgroundColor { get; set; } = Color.Black;
        public Color BorderColor { get; set; } = Color.LightGray;
        public int BorderThickness { get; set; } = 2;

        private const int PixelWidth = 8 * 32 * 2;
        private const int PixelHeight = 8 * 30 * 2;

        private readonly float _scrollDarkenAmount = 0.333f;

        private RenderTarget2D _nametableRenderTarget;
        private readonly Color[] _colors = new Color[PixelWidth * PixelHeight];
        private RenderTarget2D _outputRenderTarget;

        public UINametableDisplay(Nes nes) { Init(nes); }
        public UINametableDisplay(Nes nes, UIControl parent) : base(parent) { Init(nes); }

        private void Init(Nes nes)
        {
            Nes = nes;
            ChildrenLocked = true;
        }

        //TODO: optimize me! visibly lags emulator screen when nametable viewer allowed to render (figure out how to profile this stuff)
        //note: doesn't visibly lag anymore, but probably still a good idea
        //(also maybe allow comparing previous results with a percentage for silly dopamine hits)
        protected override void RenderElementContents(SpriteBatch spriteBatch)
        {
            if (Bounds.Width == 0 || Bounds.Height == 0)
                return;

            var graphics = spriteBatch.GraphicsDevice;
            _nametableRenderTarget ??= new(graphics, PixelWidth, PixelHeight);
            graphics.SetRenderTarget(_nametableRenderTarget);
            graphics.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            //draw final nametable to output texture, end spritebatch and rendertarget
            void Present()
            {
                if (_outputRenderTarget == null || _outputRenderTarget.Bounds.Size != Bounds.Size)
                    _outputRenderTarget = new(graphics, Bounds.Width, Bounds.Height);

                graphics.SetRenderTarget(_outputRenderTarget);
                spriteBatch.Draw(_nametableRenderTarget, FitRectangle(_nametableRenderTarget.Bounds, _outputRenderTarget.Bounds), Color.White);
                spriteBatch.End();
                graphics.SetRenderTarget(null);
            }

            var cart = Nes.Cartridge;
            if (cart == null)
            {
                Present();
                return;
            }

            void SetPixel(int x, int y, Color color)
            {
                _colors[x + y * _nametableRenderTarget.Width] = color;
            }

            ushort t = Nes.Ppu.t;
            //yyyNNYYYYYXXXXX
            int xScrollLeft = ((t & 0b10000000000) >> 2) | ((t & 0b11111) << 3) | Nes.Ppu.x;
            int yScrollTop = ((t & 0b100000000000) >> 3) | ((t & 0b1111100000) >> 2) | ((t & 0b111000000000000) >> 9);
            int XScrollRight = (xScrollLeft + 255) % 512;
            int yScrollBottom = (yScrollTop + 239) % 480;

            bool xWrap = xScrollLeft > XScrollRight;
            bool yWrap = yScrollTop > yScrollBottom;

            for (int table = 0; table < 4; table++)
            {
                for (int tileY = 0; tileY < 30; tileY++)
                {
                    for (int tileX = 0; tileX < 32; tileX++)
                    {
                        ushort vramAddress = (ushort)(0x2000 + table * 0x400 + tileY * 32 + tileX);
                        byte tile = 0;
                        Nes.Ppu.Read(vramAddress, ref tile);

                        int tileIndex = tile;
                        if (Nes.Ppu.BackgroundSecondPatternTable)
                            tileIndex += 256;

                        byte attributeOffset = (byte)((tileX >> 2) + (tileY >> 2) * 8);
                        byte attribute = 0;
                        Nes.Ppu.Read((ushort)(0x400 * table + 0x23C0 + attributeOffset), ref attribute);
                        byte quadrant = (byte)(((tileX >> 1) & 1) + ((tileY >> 1) & 1) * 2);
                        byte palette = (byte)((attribute >> (quadrant * 2)) & 3);

                        for (int y = 0; y < 8; y++)
                        {
                            byte lowByte = 0;
                            Nes.Ppu.Read((ushort)(y + tileIndex * 16), ref lowByte);
                            byte highByte = 0;
                            Nes.Ppu.Read((ushort)(8 + y + tileIndex * 16), ref highByte);
                            for (int x = 0; x < 8; x++)
                            {
                                int paletteIndex = (lowByte >> (7 - x)) & 1;
                                paletteIndex += ((highByte >> (7 - x)) & 1) * 2;
                                Color color = Nes.Ppu.PaletteRamColors[paletteIndex != 0 ? (paletteIndex + palette * 4) : 0 & 0b111111];

                                int drawX = x + tileX * 8 + (table & 1) * PixelWidth / 2;
                                int drawY = y + tileY * 8 + (table >> 1) * PixelHeight / 2;

                                bool inX = xWrap ? (drawX >= xScrollLeft) || (drawX <= XScrollRight) : (drawX >= xScrollLeft && drawX <= XScrollRight);
                                bool inY = yWrap ? (drawY >= yScrollTop) || (drawY <= yScrollBottom) : (drawY >= yScrollTop && drawY <= yScrollBottom);

                                if (!inX || !inY)
                                {
                                    color.R = (byte)(color.R * _scrollDarkenAmount);
                                    color.G = (byte)(color.G * _scrollDarkenAmount);
                                    color.B = (byte)(color.B * _scrollDarkenAmount);
                                }

                                SetPixel(drawX, drawY, color);
                            }
                        }
                    }
                }
            }

            _nametableRenderTarget.SetData(_colors);

            Present();
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);

            if (_outputRenderTarget == null)
                return;

            spriteBatch.Draw(_outputRenderTarget, Bounds, Color.White);

            var blank = GetBlankTexture(spriteBatch);
            var nametableBounds = FitRectangle(_nametableRenderTarget.Bounds, Bounds);

            spriteBatch.Draw(blank, new Rectangle(nametableBounds.X + nametableBounds.Width / 2, nametableBounds.Y, BorderThickness, nametableBounds.Height), BorderColor);
            spriteBatch.Draw(blank, new Rectangle(nametableBounds.X, nametableBounds.Y + nametableBounds.Height / 2, nametableBounds.Width, BorderThickness), BorderColor);
        }
    }
}
