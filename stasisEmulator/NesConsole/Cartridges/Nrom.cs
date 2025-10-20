using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Cartridges
{
    public class Nrom(Rom rom) : Cartridge(rom)
    {
        public override void ReadCartridgeCpu(ushort address, ref byte dataBus)
        {
            if (address >= 0x8000)
                dataBus = Rom.PrgRom[(address - 0x8000) % Rom.PrgRom.Length];
        }
        public override void WriteCartridgeCpu(ushort address, byte value)
        {
            throw new NotImplementedException();
        }

        public override void ReadCartridgePpu(ushort address, ref byte dataBus)
        {
            throw new NotImplementedException();
        }
        public override void WriteCartridgePpu(ushort address, byte value)
        {
            throw new NotImplementedException();
        }

        public override (bool usePpuVram, bool useFirstNametable) MapNameTable(ushort address)
        {
            throw new NotImplementedException();
        }
    }
}
