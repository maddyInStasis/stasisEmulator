using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using stasisEmulator.UI.Controls;

namespace stasisEmulator.UI.Windows
{
    public class MemoryViewerWindow : UIWindow
    {
        public MemoryViewerWindow(Nes nes, GameWindow gameWindow, GraphicsDevice graphicsDevice) : base(gameWindow, graphicsDevice) { Init(nes); }
        public MemoryViewerWindow(Nes nes, GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice, width, height) { Init(nes); }

        private void Init(Nes nes)
        {
            AddChildren(
            [
                new UIMemoryViewer(nes)
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow()
                }
            ]);

            Title = "Memory Viewer";
        }
    }
}
