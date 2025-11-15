using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using stasisEmulator.UI.Controls;

namespace stasisEmulator.UI.Windows
{
    public class PatternViewerWindow : UIWindow
    {
        private UIPatternDisplay _patternDisplay;

        public PatternViewerWindow(Nes nes, GameWindow gameWindow, GraphicsDevice graphicsDevice) : base(gameWindow, graphicsDevice) { Init(nes); }
        public PatternViewerWindow(Nes nes, GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice, width, height) { Init(nes); }

        private void Init(Nes nes)
        {
            _patternDisplay = new(nes, this)
            {
                Width = UISize.Grow(),
                Height = UISize.Grow(),
            };

            Title = "Pattern Viewer";
        }
    }
}
