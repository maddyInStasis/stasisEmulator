using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Cartridges
{
    public class Cnrom(Rom rom) : Cartridge(rom)
    {
        private byte _bank;

        public override void ReadCartridgeCpu(ushort address, ref byte dataBus)
        {
            if (address >= 0x8000)
                dataBus = Rom.PrgRom[(address - 0x8000) % Rom.PrgRom.Length];
        }
        public override void WriteCartridgeCpu(ushort address, byte value)
        {
            if (address >= 0x8000)
                _bank = (byte)(value & 3);
        }

        public override void ReadCartridgePpu(ushort address, ref byte dataBus)
        {
            if (address < 0x2000)
            {
                dataBus = Rom.ChrRom[address + _bank * 0x2000];
            }
        }
        public override void WriteCartridgePpu(ushort address, byte value) { }

        public override (bool usePpuVram, bool useFirstPpuVramNametable) MapNameTable(byte nametable)
        {
            if (Rom.Metadata.VerticalMirror)
                return (true, (nametable & 1) == 0);
            else
                return (true, nametable > 1);
        }
    }
}
