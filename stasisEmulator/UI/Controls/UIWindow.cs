using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    internal class UIWindow : UIControl
    {
        private readonly GraphicsDevice _graphicsDevice;

        public UIWindow(GraphicsDevice graphicsDevice) : base()
        {
            _graphicsDevice = graphicsDevice;
            SetMaxSize();
        }

        public UIWindow(GraphicsDevice graphicsDevice, List<UIControl> children) : base(children)
        {
            _graphicsDevice = graphicsDevice;
            SetMaxSize();
        }

        private void SetMaxSize()
        {
            if (_graphicsDevice == null)
                return;

            SizeLocked = false;
            Width = UISize.Grow(max: _graphicsDevice.Viewport.Width);
            Height = UISize.Grow(max: _graphicsDevice.Viewport.Height);
            SizeLocked = true;
        }

        protected override void UpdateElementPreLayout(GameTime gameTime)
        {
            SetMaxSize();
        }
    }
}
