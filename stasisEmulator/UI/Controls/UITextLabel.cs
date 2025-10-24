using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace stasisEmulator.UI.Controls
{
    internal class UITextLabel : UIControl
    {
        //TODO: Create some consistent implementation of text
        //TODO: Allow text source to be dynamic, somehow (i guess i could just make a separate TextSource property which makes Text immutable when set)
        public string Text { get; set; } = string.Empty;
        private string _finalText = string.Empty;

        public FontSystem Font { get; set; }
        
        //for some reason, FontStashSharp's font sizes are really small. a factor of 1.75 seems to scale to the correct size
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 12;

        public Color BackgroundColor { get; set; } = Color.White;
        public Color TextColor { get; set; } = Color.Black;

        public UITextLabel() : base() { Init(); }
        public UITextLabel(UIControl parent) : base(parent) { Init(); }

        private void Init()
        {
            ChildrenLocked = true;
            BackgroundColor = Color.Transparent;
        }

        protected override void CalculateContentWidth()
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            if (spriteFont == null)
                return;

            ComputedWidth = (int)spriteFont.MeasureString(Text).X;
        }

        protected override void WrapContents()
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            int availableWidth = ComputedWidth - Padding.HorizontalTotal;

            _finalText = WrapText(spriteFont, Text, availableWidth);
        }

        protected override void CalculateContentHeight()
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            if (spriteFont == null)
                return;

            ComputedHeight = (int)MeasureStringHeightCorrected(spriteFont, _finalText);
            ComputedMinimumHeight = ComputedHeight;
        }

        protected override void RenderElement(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            if (spriteFont == null)
                return;

            int contentWidth = ComputedWidth - Padding.HorizontalTotal;
            int contentHeight = ComputedHeight - Padding.VerticalTotal;

            DrawTextWithAlignment(spriteBatch, spriteFont, _finalText, new Vector2(ComputedX + Padding.Left, ComputedY + Padding.Top), TextColor,
                HorizontalContentAlignment, VerticalContentAlignment, contentWidth, contentHeight);
        }
    }
}
