using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesCore;
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
        Registers,
        CycleCount
    }

    internal class UITraceLogDisplay : UIControl
    {
        public TraceLogger TraceLogSource { get; set; }

        public FontSystem Font { get; set; } = AssetManager.DefaultMonospaceFont;
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 10;
        public Color TextColor { get; set; } = Color.Black;

        public Color BackgroundColor { get; set; } = Color.White;
        public int BorderSize { get; set; } = 1;
        public Color BorderColor { get; set; } = Color.LightGray;
        public Padding TextPadding { get; set; } = new(4, 2);

        public int AdvanceWidth { get => TextPadding.HorizontalTotal + BorderSize; }
        public int AdvanceHeight { get => TextPadding.VerticalTotal + BorderSize; }

        public Color ScrollBarThumbIdleColor { get => _scrollBar.ThumbIdleColor; set => _scrollBar.ThumbIdleColor = value; }
        public Color ScrollBarThumbHoverColor { get => _scrollBar.ThumbHoverColor; set => _scrollBar.ThumbHoverColor = value; }
        public Color ScrollBarThumbDragColor { get => _scrollBar.ThumbDragColor; set => _scrollBar.ThumbDragColor = value; }

        public Color ScrollBarTrackColor { get => _scrollBar.TrackColor; set => _scrollBar.TrackColor = value; }
        public Color ScrollBarTrackDisabledColor { get => _scrollBar.TrackDisabledColor; set => _scrollBar.TrackDisabledColor = value; }

        public Color ScrollBarButtonIdleColor { get => _scrollBar.ButtonIdleColor; set => _scrollBar.ButtonIdleColor = value; }
        public Color ScrollBarButtonHoverColor { get => _scrollBar.ButtonHoverColor; set => _scrollBar.ButtonHoverColor = value; }
        public Color ScrollBarButtonPressColor { get => _scrollBar.ButtonPressColor; set => _scrollBar.ButtonPressColor = value; }
        public Color ScrollBarButtonDisabledColor { get => _scrollBar.ButtonDisabledColor; set => _scrollBar.ButtonDisabledColor = value; }

        public bool ShowScrollBarButtons { get => _scrollBar.ShowButtons; set => _scrollBar.ShowButtons = value; }

        private MouseComponent _mouseComponent;
        private UIScrollBar _scrollBar;

        private RenderTarget2D _renderTarget;

        private const int MinVisibleRows = 1;

        private static readonly Dictionary<TraceLoggerColumnType, int> _columnCharacterWidth = new(){
            { TraceLoggerColumnType.ProgramCounter, GetColumnString(0).Length },
            { TraceLoggerColumnType.ByteCode, GetColumnString(new ByteCode(0, 0, 0)).Length },
            { TraceLoggerColumnType.Disassembly, GetColumnString(new Disassembly(Cpu.Instr.LDA, Cpu.Addr.Imm, 0, 0, false)).Length },
            { TraceLoggerColumnType.Registers, GetColumnString(new Registers()).Length },
            { TraceLoggerColumnType.CycleCount, GetColumnString((ulong)0).Length }
        };

        private static readonly List<TraceLoggerColumnType> _traceLogColumns = [
            TraceLoggerColumnType.ProgramCounter,
            TraceLoggerColumnType.ByteCode,
            TraceLoggerColumnType.Disassembly,
            TraceLoggerColumnType.Registers,
            TraceLoggerColumnType.CycleCount
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
            return $"{GetInstrName(disassembly.Instruction)} {GetAddrString(disassembly)}".PadRight(39);
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

        private static string GetColumnString(ulong cycleCount)
        {
            return $"Cycle: {cycleCount}".PadRight(7 + 15);
        }

        private static string GetColumnString(TraceLoggerRow traceLoggerRow, TraceLoggerColumnType type)
        {
            return type switch
            {
                TraceLoggerColumnType.ProgramCounter => GetColumnString(traceLoggerRow.PC),
                TraceLoggerColumnType.ByteCode => GetColumnString(traceLoggerRow.ByteCode),
                TraceLoggerColumnType.Disassembly => GetColumnString(traceLoggerRow.Disassembly),
                TraceLoggerColumnType.Registers => GetColumnString(traceLoggerRow.Registers),
                TraceLoggerColumnType.CycleCount => GetColumnString(traceLoggerRow.CycleCount),
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
            foreach (var columnType in _traceLogColumns)
            {
                offset += (int)GetWidthOfChars(_columnCharacterWidth[columnType]);
                offset += AdvanceWidth;
            }

            offset += BorderSize + _scrollBar.Thickness;

            return offset;
        }

        private int GetRowHeight()
        {
            return AdvanceHeight + (int)MeasureStringHeightCorrected(AssetManager.GetFont(Font, CorrectedFontSize), "A");
        }

        private int GetTotalHeight()
        {
            return TraceLogSource.Count * GetRowHeight() + BorderSize;
        }

        protected override void CalculateContentWidth()
        {
            ComputedWidth = GetTotalWidth();
            ComputedMinimumWidth = ComputedWidth;
        }

        protected override void CalculateContentHeight()
        {
            ComputedHeight = GetRowHeight() * MinVisibleRows + BorderSize;
            ComputedMinimumHeight = ComputedHeight;
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

            return name;
        }

        readonly struct LogLabel(string readName, string writeName)
        {
            readonly string _readName = readName;
            readonly string _writeName = writeName;

            public readonly bool HasMultipleNames = readName != writeName;

            public LogLabel(string name) : this(name, name) { }

            public string GetName(bool write)
            {
                return write ? _writeName : _readName;
            }
        }

        private static readonly Dictionary<ushort, LogLabel> _labels = new(){
            //PPU
            {0x2000, new("PpuControl")},
            {0x2001, new("PpuMask")},
            {0x2002, new("PpuStatus")},
            {0x2003, new("OamAddr")},
            {0x2004, new("OamData")},
            {0x2005, new("PpuScroll")},
            {0x2006, new("PpuAddr")},
            {0x2007, new("PpuData")},

            {0x4014, new("SpriteDma")},

            //APU
            {0x4000, new("Pulse1DutyEnv")},
            {0x4001, new("Pulse1Sweep")},
            {0x4002, new("Pulse1Period")},
            {0x4003, new("Pulse1Length")},

            {0x4004, new("Pulse2DutyEnv")},
            {0x4005, new("Pulse2Sweep")},
            {0x4006, new("Pulse2Period")},
            {0x4007, new("Pulse2Length")},

            {0x4008, new("TriangleCounter")},
            {0x400A, new("TrianglePeriod")},
            {0x400B, new("TriangleLength")},

            {0x400C, new("NoiseEnv")},
            {0x400E, new("NoisePeriod")},
            {0x400F, new("NoiseLength")},

            {0x4010, new("DmcFreq")},
            {0x4011, new("DmcLoad")},
            {0x4012, new("DmcAddress")},
            {0x4013, new("DmcLength")},

            {0x4015, new("ApuStatus", "ApuControl")},

            //IO / Frame counter
            {0x4016, new("Controller1")},
            {0x4017, new("Controller2", "FrameCounter")},
        };

        private static ushort MirrorAddress(ushort address)
        {
            if (address < 0x2000)
                return (ushort)(address & 0x7FF);
            if (address < 0x4000)
                return (ushort)((address & 7) | 0x2000);

            return address;
        }

        private static string GetLabeledString(ushort address, bool writeInstruction)
        {
            string output = $"${address:X4}";
            ushort mirroredAddress = MirrorAddress(address);

            //why is it initially null here????
            if (_labels != null && _labels.TryGetValue(mirroredAddress, out var label))
            {
                output = label.GetName(writeInstruction);
                output += $"_{mirroredAddress:X4}";
                if (label.HasMultipleNames)
                    output += $" {(writeInstruction ? "(Write)" : "(Read)")}";
                if (address != mirroredAddress)
                    output += " (Mirror)";
            }

            return output;
        }

        private static string GetAddrString(Disassembly disassembly)
        {
            ushort argument = disassembly.Argument;
            ushort effectiveAddr = disassembly.EffectiveAddress;
            bool writeInstruction = disassembly.WriteInstruction;

            string regString = GetLabeledString(argument, writeInstruction);
            string effectiveRegString = GetLabeledString(effectiveAddr, writeInstruction);

            return disassembly.AddressingMode switch
            {
                Cpu.Addr.Imp => "",
                Cpu.Addr.Acc => "A",
                Cpu.Addr.Imm => $"#${argument:X2}",
                Cpu.Addr.Zero => $"${argument:X2}",
                Cpu.Addr.ZeroX => $"${argument:X2},X [${effectiveAddr:X2}]",
                Cpu.Addr.ZeroY => $"${argument:X2},Y [${effectiveAddr:X2}]",
                Cpu.Addr.Abs => $"{regString}",
                Cpu.Addr.AbsX => $"${argument:X4},X [{effectiveRegString}]",
                Cpu.Addr.AbsXW => $"${argument:X4},X [{effectiveRegString}]",
                Cpu.Addr.AbsY => $"${argument:X4},Y [{effectiveRegString}]",
                Cpu.Addr.AbsYW => $"${argument:X4},Y [{effectiveRegString}]",
                Cpu.Addr.IndX => $"(${argument:X2},X) [{effectiveRegString}]",
                Cpu.Addr.IndY => $"(${argument:X2}),Y [{effectiveRegString}]",
                Cpu.Addr.IndYW => $"(${argument:X2}),Y [{effectiveRegString}]",
                Cpu.Addr.Rel => $"${argument:X4}",
                Cpu.Addr.Other => disassembly.Instruction switch
                {
                    Cpu.Instr.JMP_abs => $"{regString}",
                    Cpu.Instr.JMP_ind => $"(${argument:X4}) [{effectiveRegString}]",
                    Cpu.Instr.JSR => $"{regString}",
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
            if (ComputedWidth <= 0 || ComputedHeight <= 0)
                return;

            GraphicsDevice graphicsDevice = spriteBatch.GraphicsDevice;
            if (_renderTarget == null || _renderTarget.Bounds.Size != Bounds.Size)
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

            if (_renderTarget == null)
                return;

            spriteBatch.Draw(_renderTarget, new Rectangle(ComputedX, ComputedY, ComputedWidth, ComputedHeight), Color.White);
        }
    }
}
