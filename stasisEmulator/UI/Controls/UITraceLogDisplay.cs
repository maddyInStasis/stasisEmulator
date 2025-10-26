using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using stasisEmulator.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace stasisEmulator.UI.Controls
{
    public enum TraceLoggerColumnType
    {
        ProgramCounter,
        ByteCode,
        Disassembly,
        Registers
    }

    internal class UITraceLogDisplay : UIControl
    {
        public TraceLogger TraceLogSource { get; set; }

        public FontSystem Font { get; set; } = AssetManager.DefaultMonospaceFont;
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 12;
        public Color TextColor { get; set; } = Color.Black;

        public Color BackgroundColor { get; set; } = Color.White;
        public int BorderSize { get; set; } = 2;
        public Color BorderColor { get; set; } = Color.LightGray;

        public Padding TextPadding = new(4, 2);
        public int AdvanceWidth { get => TextPadding.HorizontalTotal + BorderSize; }

        public Color ScrollBarThumbIdleColor { get => _scrollBar.ThumbIdleColor; set => _scrollBar.ThumbIdleColor = value; }
        public Color ScrollBarThumbHoverColor { get => _scrollBar.ThumbHoverColor; set => _scrollBar.ThumbHoverColor = value; }
        public Color ScrollBarThumbDragColor { get => _scrollBar.ThumbDragColor; set => _scrollBar.ThumbDragColor = value; }

        public Color ScrollBarTrackColor { get => _scrollBar.TrackColor; set => _scrollBar.TrackColor = value; }

        public Color ScrollBarButtonIdleColor
        {
            get => _scrollBar.ButtonIdleColor;
            set => _scrollBar.ButtonIdleColor = value;
        }
        public Color ScrollBarButtonHoverColor
        {
            get => _scrollBar.ButtonHoverColor;
            set => _scrollBar.ButtonHoverColor = value;
        }
        public Color ScrollBarButtonPressColor
        {
            get => _scrollBar.ButtonPressColor;
            set => _scrollBar.ButtonPressColor = value;
        }

        public bool ShowScrollBarButtons { get => _scrollBar.ShowButtons; set => _scrollBar.ShowButtons = value; }

        private MouseComponent _mouseComponent;
        private UIScrollBar _scrollBar;

        private RenderTarget2D _renderTarget;

        private const int MinVisibleRows = 1;

        private static readonly Dictionary<TraceLoggerColumnType, int> _columnCharacterWidth = new(){
            { TraceLoggerColumnType.ProgramCounter, GetColumnString(0).Length },
            { TraceLoggerColumnType.ByteCode, GetColumnString(new ByteCode(0, 0, 0)).Length },
            { TraceLoggerColumnType.Disassembly, GetColumnString(new Disassembly(Cpu.Instr.LDA, Cpu.Addr.Imm, 0)).Length },
            { TraceLoggerColumnType.Registers, GetColumnString(new Registers()).Length }
        };

        private static readonly List<TraceLoggerColumnType> _traceLogColumns = [
            TraceLoggerColumnType.ProgramCounter,
            TraceLoggerColumnType.ByteCode,
            TraceLoggerColumnType.Disassembly,
            TraceLoggerColumnType.Registers
        ];

        public UITraceLogDisplay(TraceLogger traceLogger) : base() { Init(traceLogger); }
        public UITraceLogDisplay(TraceLogger traceLogger, UIControl parent) : base(parent) { Init(traceLogger); }

        private void Init(TraceLogger traceLogger)
        {
            _mouseComponent = new(this);
            _scrollBar = new(this);
            _scrollBar.Value = _scrollBar.Range.Max;
            HorizontalContentAlignment = HorizontalAlignment.Right;
            ChildrenLocked = true;
            TraceLogSource = traceLogger;
        }

        private static string GetColumnString(ushort programCounter)
        {
            return $"{programCounter:X4}";
        }

        private static string GetColumnString(ByteCode byteCode)
        {
            return byteCode.Length switch
            {
                1 => $"{byteCode.Opcode:X2}",
                2 => $"{byteCode.Opcode:X2} {byteCode.OperandA:X2}",
                3 => $"{byteCode.Opcode:X2} {byteCode.OperandA:X2} {byteCode.OperandB:X2}",
                _ => throw new Exception($"A fucky wucky occurred and we have a bytecode length which is not a value from 1 to 3. Got {byteCode.Length}"),
            };
        }

        private static string GetColumnString(Disassembly disassembly)
        {
            return $"{GetInstrName(disassembly.Instruction)} {GetAddrString(disassembly)}".PadRight(38);
        }

        private static string GetColumnString(Registers registers)
        {
            string regString = $"A:{registers.A:X2} X:{registers.X:X2} Y:{registers.Y:X2} S:{registers.S:X2} P:";
            byte p = registers.P;
            regString +=
                ((p & 0x80) != 0 ? "N" : "n") +
                ((p & 0x40) != 0 ? "V" : "v") +
                "--" +
                ((p & 0x08) != 0 ? "D" : "d") +
                ((p & 0x04) != 0 ? "I" : "i") +
                ((p & 0x02) != 0 ? "Z" : "z") +
                ((p & 0x01) != 0 ? "C" : "c");

            return regString;
        }

        private static string GetColumnString(TraceLoggerRow traceLoggerRow, TraceLoggerColumnType type)
        {
            return type switch
            {
                TraceLoggerColumnType.ProgramCounter => GetColumnString(traceLoggerRow.PC),
                TraceLoggerColumnType.ByteCode => GetColumnString(traceLoggerRow.ByteCode),
                TraceLoggerColumnType.Disassembly => GetColumnString(traceLoggerRow.Disassembly),
                TraceLoggerColumnType.Registers => GetColumnString(traceLoggerRow.Registers),
                _ => throw new Exception($"Unrecognized trace logger column type: {type}")
            };
        }

        //only accurate with monospaced text, which is what should always be used
        private float GetWidthOfChars(int numChars)
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            return spriteFont.MeasureString("M").X * numChars;
        }

        private int GetTotalWidth()
        {
            int offset = 0;
            foreach(var columnType in _traceLogColumns)
            {
                offset += (int)GetWidthOfChars(_columnCharacterWidth[columnType]);
                offset += AdvanceWidth;
            }

            offset += BorderSize + _scrollBar.Thickness;

            return offset;
        }

        private int GetRowHeight()
        {
            return AdvanceWidth + (int)MeasureStringHeightCorrected(AssetManager.GetFont(Font, FontSize), "A");
        }

        private int GetTotalHeight()
        {
            return TraceLogSource.Count * GetRowHeight() + BorderSize;
        }

        protected override void CalculateContentWidth()
        {
            ComputedWidth = GetTotalWidth();
        }

        protected override void CalculateContentHeight()
        {
            ComputedHeight = GetTotalHeight();
            ComputedMinimumHeight = GetRowHeight() * MinVisibleRows + BorderSize;
        }

        private static string GetInstrName(Cpu.Instr instruction)
        {
            switch (instruction)
            {
                case Cpu.Instr.JMP_abs:
                    return "JMP";
                case Cpu.Instr.JMP_ind:
                    return "JMP";
            }

            string name = Enum.GetName(instruction);

            if (name.Length > 3)
                throw new Exception($"hey girly you forgor a special case :3 ({name})");

            return name;
        }
        
        private static string GetAddrString(Disassembly disassembly)
        {
            ushort argument = disassembly.Argument;

            return disassembly.AddressingMode switch
            {
                Cpu.Addr.Imp => "",
                Cpu.Addr.Acc => "",
                Cpu.Addr.Imm => $"#${argument:X2}",
                Cpu.Addr.Zero => $"${argument:X2}",
                Cpu.Addr.ZeroX => $"${argument:X2},X",
                Cpu.Addr.ZeroY => $"${argument:X2},Y",
                Cpu.Addr.Abs => $"${argument:X4}",
                Cpu.Addr.AbsX => $"${argument:X4},X",
                Cpu.Addr.AbsXW => $"${argument:X4},X",
                Cpu.Addr.AbsY => $"${argument:X4},Y",
                Cpu.Addr.AbsYW => $"${argument:X4},Y",
                Cpu.Addr.IndX => $"(${argument:X2},X)",
                Cpu.Addr.IndY => $"(${argument:X2}),Y",
                Cpu.Addr.IndYW => $"(${argument:X2}),Y",
                Cpu.Addr.Rel => $"${argument:X4}",
                Cpu.Addr.Other => disassembly.Instruction switch
                {
                    Cpu.Instr.JMP_abs => $"${argument:X4}",
                    Cpu.Instr.JMP_ind => $"(${argument:X4})",
                    Cpu.Instr.RTI => "",
                    _ => "DisplayNotImpl"
                },
                _ => "DisplayNotImpl"
            };
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            _mouseComponent.Update();
            //keep scroll at bottom as log fills if scroll is already located at bottom
            bool bottom = _scrollBar.Value == _scrollBar.Range.Max;
            _scrollBar.ContentTotalSize = GetTotalHeight();
            _scrollBar.ContentVisibleSize = ComputedHeight;
            if (bottom)
                _scrollBar.Value = _scrollBar.Range.Max;
            _scrollBar.Scroll(-_mouseComponent.ScrollAmount);
        }

        protected override void RenderElementContents(SpriteBatch spriteBatch)
        {
            GraphicsDevice graphicsDevice = spriteBatch.GraphicsDevice;
            if (_renderTarget == null || _renderTarget.Width != ComputedWidth || _renderTarget.Height != ComputedHeight)
                _renderTarget = new(graphicsDevice, ComputedWidth, ComputedHeight);

            graphicsDevice.SetRenderTarget(_renderTarget);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin();

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            var blank = GetBlankTexture(spriteBatch);
            int rowHeight = GetRowHeight();

            int startIndex = (int)_scrollBar.Value / rowHeight;
            int offsetVert = -(int)_scrollBar.Value % rowHeight;

            for (int i = startIndex; i < TraceLogSource.Count; i++)
            {
                var traceLogRow = TraceLogSource[i];
                int offsetHori = 0;

                foreach (var logType in _traceLogColumns)
                {
                    string drawString = GetColumnString(traceLogRow, logType);
                    spriteBatch.DrawString(spriteFont, drawString, new Vector2(offsetHori + BorderSize + TextPadding.Left, offsetVert + BorderSize + TextPadding.Top), TextColor);
                    offsetHori += (int)GetWidthOfChars(_columnCharacterWidth[logType]) + AdvanceWidth;
                }

                spriteBatch.Draw(blank, new Rectangle(0, offsetVert, ComputedWidth, BorderSize), BorderColor);

                offsetVert += rowHeight;
                if (offsetVert > ComputedHeight)
                    break;
            }

            spriteBatch.Draw(blank, new Rectangle(0, offsetVert, ComputedWidth, BorderSize), BorderColor);

            int borderOffset = 0;
            foreach (var logType in _traceLogColumns)
            {
                spriteBatch.Draw(blank, new Rectangle(borderOffset, 0, BorderSize, ComputedHeight), BorderColor);
                borderOffset += (int)GetWidthOfChars(_columnCharacterWidth[logType]) + AdvanceWidth;
            }
            spriteBatch.Draw(blank, new Rectangle(ComputedWidth - BorderSize - _scrollBar.Thickness, 0, BorderSize, ComputedHeight), BorderColor);

            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, BackgroundColor);
            spriteBatch.Draw(_renderTarget, new Rectangle(ComputedX, ComputedY, ComputedWidth, ComputedHeight), Color.White);
        }
    }
}
