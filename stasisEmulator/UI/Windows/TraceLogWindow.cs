using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using stasisEmulator.UI.Controls;
using System;
using System.Diagnostics;

namespace stasisEmulator.UI.Windows
{
    public class TraceLogWindow : UIWindow
    {
        private Nes _nes;
        private UITextBox _countTextBox;

        public TraceLogWindow(Nes nes, GameWindow gameWindow, GraphicsDevice graphicsDevice) : base(gameWindow, graphicsDevice) { Init(nes); }
        public TraceLogWindow(Nes nes, GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice, width, height) { Init(nes); }

        private void Init(Nes nes)
        {
            _nes = nes;

            UIButton instrButton;
            UIButton cycleButton;
            UIButton vblankButton;

            AddChildren([
                new UIRectangle([
                    _countTextBox = new()
                    {
                        SelectAllOnClick = true
                    },
                    instrButton = new([
                        new UITextLabel("Run instructions")
                    ]),
                    cycleButton = new([
                        new UITextLabel("Run cycles")
                    ]),
                    vblankButton = new([
                        new UITextLabel("Run to VBlank")
                    ]),
                ])
                {
                    Width = UISize.Grow(),
                    Padding = new(8, 2),
                    ChildMargin = 8
                },
                new UITraceLogDisplay(nes.Cpu.TraceLogger)
                {
                    Width = UISize.Grow(),
                    Height = UISize.Grow(),
                    BackgroundColor = new Color(25, 25, 40),
                    TextColor = new Color(25, 179, 184),
                    BorderColor = new Color(64, 64, 102),
                    ScrollBarTrackColor = new Color(25, 25, 40),
                    ScrollBarTrackDisabledColor = new Color(25, 25, 40),
                    ScrollBarThumbIdleColor = new Color(25, 179, 184),
                    ScrollBarThumbHoverColor = new Color(0, 146, 150),
                    ScrollBarThumbDragColor = new Color(0, 87, 91),
                    ShowScrollBarButtons = false,
                },
            ]);
            FillDirection = FillDirection.TopToBottom;

            Title = "Trace Logger";

            instrButton.Click += OnInstrButtonClick;
            cycleButton.Click += OnCycleButtonClick;
            vblankButton.Click += OnVblankButtonClick;
        }

        private void OnInstrButtonClick(object sender, EventArgs e)
        {
            string textBoxValue = _countTextBox.Text;
            if (!ulong.TryParse(textBoxValue, out ulong count))
                return;

            _nes.Advance(Nes.AdvanceType.Instructions, count);
        }

        private void OnCycleButtonClick(object sender, EventArgs e)
        {
            string textBoxValue = _countTextBox.Text;
            if (!ulong.TryParse(textBoxValue, out ulong count))
                return;

            _nes.Advance(Nes.AdvanceType.Cycles, count);
        }

        private void OnVblankButtonClick(object sender, EventArgs e)
        {
            _nes.Advance(Nes.AdvanceType.VBlank);
        }
    }
}
