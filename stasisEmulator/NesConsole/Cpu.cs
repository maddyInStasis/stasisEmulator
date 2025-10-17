using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole
{
    public class Cpu(Nes nes)
    {
        private readonly Nes _nes = nes;

        public byte A { get; private set; }
        public byte X { get; private set; }
        public byte Y { get; private set; }

        public ushort PC { get; private set; }
        public ushort SP { get; private set; }

        public readonly byte[] Ram = new byte[0x800];

        public ushort addressBus { get; private set; }
        public byte dataBus { get; private set; }

        public void Power()
        {

        }

        public void Reset()
        {

        }

        private byte Read(ushort address)
        {
            if (address < 0x2000)
                dataBus = Ram[address & (0x800 - 1)];

            return dataBus;
        }
        private void Write(ushort address, byte value)
        {
            if (address < 0x2000)
                Ram[address & (0x800 - 1)] = value;
        }

        public void RunCycle()
        {

        }
    }
}
