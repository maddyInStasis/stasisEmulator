using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    internal class UIMessageHandler : UIControl
    {
        //TODO: add timer progress bar, close button, pause on hover
        public string Text { get; set; } = string.Empty;
        private string _wrappedText = string.Empty;

        public FontSystem Font { get; set; }

        public float FontSize { get; set; } = 12;

        public Color MessageBackgroundColor { get; set; } = Color.White;
        public Color MessageTextColor { get; set; } = Color.Black;

        public Padding MessagePadding { get; set; } = new(4, 2);

        public float MessageDuration { get; set; } = 5;

        private readonly List<UIControl> _messages = [];
        private readonly List<float> _messageTimers = [];

        public UIMessageHandler() { Init(); }

        public UIMessageHandler(UIControl parent) : base(parent) { Init(); }

        private void Init()
        {
            FillsAutoLayoutSpace = false;
            FillDirection = FillDirection.TopToBottom;
            VerticalContentAlignment = VerticalAlignment.Bottom;
            ChildrenLocked = true;
        }

        public void AddMessage(string message)
        {
            UIRectangle messageRect = new([
                new UITextLabel()
                {
                    Text = message,
                    TextColor = MessageTextColor
                }
            ])
            {
                BackgroundColor = MessageBackgroundColor,
                Padding = MessagePadding,
            };

            _messages.Add(messageRect);
            _messageTimers.Add(MessageDuration);

            ChildrenLocked = false;
            messageRect.Parent = this;
            ChildrenLocked = true;
        }

        protected override void UpdateElementPreLayout(GameTime gameTime)
        {
            float deltaTime = (float)(gameTime.ElapsedGameTime.TotalSeconds);

            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                _messageTimers[i] -= deltaTime;
                if (_messageTimers[i] > 0)
                    continue;

                ChildrenLocked = false;
                _messages[i].Parent = null;
                ChildrenLocked = true;
                _messages.RemoveAt(i);
                _messageTimers.RemoveAt(i);
            }
        }
    }
}
