using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.NesConsole;
using System;
using System.Collections.Generic;

namespace stasisEmulator.UI.Controls
{
    internal class UITraceLogDisplay : UIControl
    {
        public readonly TraceLogger TraceLogSource;

        public FontSystem Font { get; set; } = AssetManager.DefaultMonospaceFont;
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 12;

        public int BorderSize { get; set; } = 2;
        public Color BorderColor { get; set; } = Color.LightGray;

        public UITraceLogDisplay(TraceLogger traceLogger) : base() { Init(); TraceLogSource = traceLogger; }
        public UITraceLogDisplay(TraceLogger traceLogger, UIControl parent) : base(parent) { Init(); TraceLogSource = traceLogger; }

        private void Init()
        {
            ChildrenLocked = true;
            Padding = new Padding(4, 2);
        }

        //only accurate with monospaced text, which is what should always be used
        private float GetWidthOfChars(int numChars)
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            return spriteFont.MeasureString("M").X * numChars;
        }

        private static string GetInstrName(Cpu.Instr instruction)
        {
            switch (instruction)
            {
                case Cpu.Instr.JMP_abs:
                    return "JMP";
            }

            string name = Enum.GetName(instruction);

            if (name.Length > 3)
                throw new Exception($"hey girly you forgor a special case :3 ({name})");

            return name;
        }

        //TODO: bytecode is definitely the wrong way to handle this. should probably just include a ushort operand in the disassembly
        private static string GetAddrString(Disassembly disassembly)
        {
            ushort argument = disassembly.Argument;

            return disassembly.AddressingMode switch
            {
                Cpu.Addr.Imm => $"#${argument:X2}",
                Cpu.Addr.Zero => $"${argument:X2}",
                Cpu.Addr.Abs => $"${argument:X4}",
                Cpu.Addr.Imp => "",
                Cpu.Addr.Acc => "",
                Cpu.Addr.Rel => $"${argument:X4}",
                Cpu.Addr.Other => disassembly.Instruction switch
                {
                    Cpu.Instr.JMP_abs => $"${argument:X4}",
                    _ => "DisplayNotImpl"
                },
                _ => "DisplayNotImpl"
            };
        }

        //TODO: this is, like, bad. please fix
        protected override void RenderElement(SpriteBatch spriteBatch)
        {
            base.RenderElement(spriteBatch);

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            float lineHeight = MeasureStringHeightCorrected(spriteFont, "");

            int totalWidth = 0;
            HashSet<int> columnDividers = [];

            var blank = GetBlankTexture(spriteBatch);

            for (int i = 0; i < TraceLogSource.Count; i++)
            {
                int columnOffset = 0;
                void DrawStringInCell(string text)
                {
                    spriteBatch.DrawString(spriteFont, text, new Vector2(BorderSize + Padding.Left + columnOffset, BorderSize + Padding.Top + (lineHeight + Padding.VerticalTotal + BorderSize) * i), Color.Black);
                }
                void IncColumnOffset(int numChars)
                {
                    columnOffset += (int)(GetWidthOfChars(numChars) + BorderSize + Padding.HorizontalTotal);
                    columnDividers.Add(columnOffset);
                }

                var traceLogRow = TraceLogSource[i];

                DrawStringInCell($"{traceLogRow.PC:X4}");
                IncColumnOffset(4);
                var byteCode = traceLogRow.ByteCode;
                DrawStringInCell($"{byteCode.Opcode:X2}{(byteCode.Length > 1 ? $" {byteCode.OperandA:X2}" : "")}{(byteCode.Length > 2 ? $" {byteCode.OperandB:X2}" : "")}");
                IncColumnOffset(8);
                var disassembly = traceLogRow.Disassembly;
                DrawStringInCell($"{GetInstrName(disassembly.Instruction)} {GetAddrString(disassembly)}");
                IncColumnOffset(38);
                string regString = $"A:{traceLogRow.Registers.A:X2} X:{traceLogRow.Registers.X:X2} Y:{traceLogRow.Registers.Y:X2} S:{traceLogRow.Registers.S:X2} P:";
                byte p = traceLogRow.Registers.P;
                regString +=
                    ((p & 0x80) != 0 ? "N" : "n") +
                    ((p & 0x40) != 0 ? "V" : "v") +
                    "--" +
                    ((p & 0x08) != 0 ? "D" : "d") +
                    ((p & 0x04) != 0 ? "I" : "i") +
                    ((p & 0x02) != 0 ? "Z" : "z") +
                    ((p & 0x01) != 0 ? "C" : "c");
                DrawStringInCell(regString);
                IncColumnOffset(regString.Length);

                if (i == 0)
                    totalWidth = columnOffset;

                spriteBatch.Draw(blank, new Rectangle(0, (int)((lineHeight + Padding.VerticalTotal + BorderSize) * (i + 1)), totalWidth, BorderSize), BorderColor);
            }

            spriteBatch.Draw(blank, new Rectangle(0, 0, totalWidth, BorderSize), BorderColor);
            spriteBatch.Draw(blank, new Rectangle(0, 0, BorderSize, (int)((lineHeight + Padding.VerticalTotal + BorderSize) * (TraceLogSource.Count))), BorderColor);
            foreach (int offset in columnDividers)
            {
                spriteBatch.Draw(blank, new Rectangle(offset, 0, BorderSize, (int)((lineHeight + Padding.VerticalTotal + BorderSize) * (TraceLogSource.Count))), BorderColor);
            }
        }
    }
}
