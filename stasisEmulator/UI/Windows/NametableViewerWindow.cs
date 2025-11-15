using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using stasisEmulator.UI.Controls;

namespace stasisEmulator.UI.Windows
{
    public class NametableViewerWindow : UIWindow
    {
        private UINametableDisplay _nametableDisplay;

        public NametableViewerWindow(Nes nes, GameWindow gameWindow, GraphicsDevice graphicsDevice) : base(gameWindow, graphicsDevice) { Init(nes); }
        public NametableViewerWindow(Nes nes, GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice, width, height) { Init(nes); }

        private void Init(Nes nes)
        {
            _nametableDisplay = new(nes, this)
            {
                Width = UISize.Grow(),
                Height = UISize.Grow(),
            };

            Title = "Nametable Viewer";
        }
    }
}
