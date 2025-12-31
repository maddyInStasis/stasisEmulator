using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesCore;
using stasisEmulator.UI.Controls;
using System.Collections.Generic;
using System.Diagnostics;

namespace stasisEmulator.UI.Windows
{
    public class PatternViewerWindow : UIWindow
    {
        private const int ButtonHeight = 20;

        private UIPatternDisplay _patternDisplay;

        private readonly List<UIControl> upperButtons = [];
        private readonly List<UIControl> lowerButtons = [];
        private readonly List<UIButton> buttons = [];
        private readonly List<UIRectangle> paletteColors = [];

        private Nes _nes;

        private Color _selectedColor = Color.DeepSkyBlue;
        private Color _idleColor = Color.Gray;
        private Color _hoverColor = Color.LightGray;

        public PatternViewerWindow(Nes nes, GameWindow gameWindow, GraphicsDevice graphicsDevice) : base(gameWindow, graphicsDevice) { Init(nes); }
        public PatternViewerWindow(Nes nes, GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice, width, height) { Init(nes); }

        private void Init(Nes nes)
        {
            _nes = nes;

            _patternDisplay = new(nes)
            {
                Width = UISize.Grow(),
                Height = UISize.Grow(),
            };

            for (int i = 0; i < 8; i++)
            {
                List<UIControl> colors = [];
                for (int j = 0; j < 4; j++)
                {
                    UIRectangle rect = new()
                    {
                        Width = UISize.Grow(),
                        Height = UISize.Grow(),
                    };
                    colors.Add(rect);
                    paletteColors.Add(rect);
                }

                UIButton button = new(colors)
                {
                    Width = UISize.Fixed(ButtonHeight * 4),
                    Height = UISize.Fixed(ButtonHeight),
                    Padding = new(2),
                    IdleBorderColor = i == _patternDisplay.Palette ? _selectedColor : _idleColor,
                    HoverBorderColor = _hoverColor,
                    PressBorderColor = _selectedColor,
                    BorderThickness = 2
                };

                button.Click += (sender, e) => 
                {
                    buttons[_patternDisplay.Palette].IdleBorderColor = _idleColor;
                    _patternDisplay.Palette = (byte)buttons.IndexOf(button);
                    button.IdleBorderColor = _selectedColor;
                };
                
                (i < 4 ? upperButtons : lowerButtons).Add(button);
                buttons.Add(button);
            }

            AddChildren([
                _patternDisplay,
                new UIRectangle([
                    new UITextLabel("Palette:"),
                    new UIRectangle(upperButtons) {ChildMargin = 4, BackgroundColor = Color.Transparent},
                    new UIRectangle(lowerButtons) {ChildMargin = 4, BackgroundColor = Color.Transparent},
                ])
                {
                    ChildMargin = 4,
                    Padding = new(8, 2),
                    FillDirection = FillDirection.TopToBottom,
                    Width = UISize.Grow()
                }
                
            ]);

            FillDirection = FillDirection.TopToBottom;
            Title = "Pattern Viewer";
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            base.UpdateElementPostLayout(gameTime);

            for (int i = 0; i < 32; i++)
            {
                var color = paletteColors[i];
                int paletteIndex = i & 3;
                color.BackgroundColor = _nes.Ppu.Palette[_nes.Ppu.PaletteRam[paletteIndex != 0 ? i : 0] & 0x3F];
            }
        }
    }
}
