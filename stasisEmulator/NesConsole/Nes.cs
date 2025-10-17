using stasisEmulator.NesConsole.Cartridges;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole
{
    public class Nes
    {
        public readonly Cpu Cpu;

        public Cartridge Cartridge {  get; set; }

        private const int _masterClockSpeed = 236250000 / 11;
        private const int _cyclesPerFrame = _masterClockSpeed / 12;

        public Nes()
        {
            Cpu = new(this);
        }

        public void RunFrame()
        {
            for (int i = 0; i < _cyclesPerFrame; i++)
            {
                Cpu.RunCycle();
            }
        }
    }
}
