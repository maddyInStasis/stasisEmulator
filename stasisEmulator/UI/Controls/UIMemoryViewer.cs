using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using stasisEmulator.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    public class UIMemoryViewer : UIControl
    {
        public FontSystem Font { get; set; } = AssetManager.DefaultMonospaceFont;

        //for some reason, FontStashSharp's font sizes are really small. a factor of 1.75 seems to scale to the correct size
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 10;
        public Color TextColor { get; set; } = Color.Black;

        public Color BackgroundColor { get; set; } = Color.White;
        public int BorderSize { get; set; } = 2;
        public Color BorderColor { get; set; } = Color.LightGray;
        public Padding TextPadding { get; set; } = new(6, 4);

        public bool HighlightStack { get; set; } = true;
        public Color StackHighlightColor { get; set; } = Color.Violet;

        private MouseComponent _mouseComponent;
        private UIScrollBar _scrollBar;

        private RenderTarget2D _renderTarget;

        private Nes _nes;

        private int LeftColumnWidth { get => (int)GetWidthOfChars(4) + TextPadding.HorizontalTotal; }
        private int TopRowHeight { get => TextPadding.VerticalTotal + (int)MeasureStringHeightCorrected(AssetManager.GetFont(Font, CorrectedFontSize), "A"); }

        public UIMemoryViewer(Nes nes) : base() { Init(nes); }
        public UIMemoryViewer(Nes nes, UIControl parent) : base(parent) { Init(nes); }

        private void Init(Nes nes)
        {
            _nes = nes;
            _mouseComponent = new(this);
            _scrollBar = new(this);
            HorizontalContentAlignment = HorizontalAlignment.Right;
            ChildrenLocked = true;
        }

        //only accurate with monospaced text, which is what should always be used
        private float GetWidthOfChars(int numChars)
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            return spriteFont.MeasureString("M").X * numChars;
        }

        private int GetHeightOfLines(int numLines)
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            return spriteFont.LineHeight * numLines;
        }

        private int GetVisualHeightOfLines(int numLines)
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            return spriteFont.LineHeight * (numLines - 1) + (int)MeasureStringHeightCorrected(spriteFont, "A");
        }

        private Rectangle GetScrollableBounds()
        {
            int y = TopRowHeight;
            int width = BorderSize * 2 + TextPadding.HorizontalTotal * 2 + (int)GetWidthOfChars(16 * 2 + 15 + 4);
            int height = ComputedHeight - y;

            return new(ComputedX, ComputedY + y, width, height);
        }

        protected override void UpdateElementPreLayout(GameTime gameTime)
        {
            _scrollBar.ContentTotalSize = BorderSize * 2 + Padding.VerticalTotal + GetHeightOfLines(0x1000);
            _scrollBar.ContentVisibleSize = GetScrollableBounds().Height;
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            _mouseComponent.Update();
            _scrollBar.Scroll(-_mouseComponent.ScrollAmount);
        }

        protected override void RenderElementContents(SpriteBatch spriteBatch)
        {
            if (ComputedWidth <= 0 || ComputedHeight <= 0)
                return;

            var scrollableBounds = GetScrollableBounds();

            GraphicsDevice graphicsDevice = spriteBatch.GraphicsDevice;
            if (_renderTarget == null || _renderTarget.Bounds.Size != scrollableBounds.Size)
                _renderTarget = new(graphicsDevice, scrollableBounds.Width, scrollableBounds.Height);

            graphicsDevice.SetRenderTarget(_renderTarget);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin();

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);

            
            int startLine = (int)(_scrollBar.Value / spriteFont.LineHeight);
            int posOffset = (int)(_scrollBar.Value % spriteFont.LineHeight);
            int lineCount = scrollableBounds.Height / spriteFont.LineHeight + 1; //??? idk why + 1 is needed, but if i don't, there's a line missing at the end
            int endLine = startLine + lineCount;

            int valuesLeft = LeftColumnWidth + BorderSize + TextPadding.Left;
            int valuesTop = TextPadding.Top - posOffset;

            Point getAddressPosition(ushort address)
            {
                int x = address & 0x0F;
                int y = address >> 4;

                return new(valuesLeft + (int)GetWidthOfChars(x * 3), valuesTop + spriteFont.LineHeight * (y - startLine));
            }
            Point getAddressMax(ushort address)
            {
                Point min = getAddressPosition(address);
                return min + new Point((int)GetWidthOfChars(2), GetVisualHeightOfLines(1));
            }

            //potentially extend this to highlight other regions of memory
            if (HighlightStack && endLine >= 0x10 && startLine <= 0x1F)
            {
                Point stackMin = getAddressPosition(0x100) - new Point(TextPadding.Left / 2, TextPadding.Top / 2);
                Point stackMax = getAddressMax(0x1FF) + new Point(TextPadding.Right / 2, 0);
                Rectangle stackRect = new(stackMin, stackMax - stackMin);

                DrawRect(spriteBatch, stackRect, StackHighlightColor * 0.25f);
                DrawBorder(spriteBatch, stackRect, BorderSize, BorderType.Center, StackHighlightColor);

                ushort sEffectiveAddress = (ushort)(0x100 + _nes.Cpu.S);
                Point stackPointerMin = getAddressPosition(sEffectiveAddress);
                Point stackPointerMax = getAddressMax(sEffectiveAddress);
                Rectangle stackPointerRect = new(stackPointerMin, stackPointerMax - stackPointerMin);

                DrawBorder(spriteBatch, stackPointerRect, BorderSize, BorderType.Outside, StackHighlightColor);
            }

            for (int i = startLine; i <= endLine; i++)
            {
                int lineTop = valuesTop + spriteFont.LineHeight * (i - startLine);

                spriteBatch.DrawString(spriteFont, $"{i << 4:X4}", new Vector2(TextPadding.Left, lineTop), TextColor);
                string memoryValues = string.Empty;
                for (int j = 0; j < 16; j++)
                {
                    memoryValues += $"{_nes.Cpu.DebugRead((ushort)(j | (i << 4))):X2} ";
                }
                spriteBatch.DrawString(spriteFont, memoryValues, new Vector2(valuesLeft, lineTop), TextColor);
            }

            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);

            if (_renderTarget == null)
                return;

            Rectangle scrollableBounds = GetScrollableBounds();
            spriteBatch.Draw(_renderTarget, scrollableBounds, Color.White);

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);

            string topLabel = string.Empty;
            for (int i = 0; i < 16; i++)
            {
                topLabel += $"{i:X2} ";
            }

            int leftBorder = ComputedX + LeftColumnWidth;

            DrawBorder(spriteBatch, new Rectangle(leftBorder, scrollableBounds.Top, scrollableBounds.Right - leftBorder, scrollableBounds.Height), BorderSize, BorderType.Inside, BorderColor);

            spriteBatch.DrawString(spriteFont, topLabel, new Vector2(leftBorder + BorderSize + TextPadding.Left, ComputedY + TextPadding.Top), TextColor);
        }
    }
}
