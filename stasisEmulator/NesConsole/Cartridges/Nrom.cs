using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Cartridges
{
    public class Nrom(Rom rom) : Cartridge(rom)
    {
        public override void ReadCartridgeCpu(ref byte dataBus)
        {
            throw new NotImplementedException();
        }
        public override void WriteCartridgeCpu(byte value)
        {
            throw new NotImplementedException();
        }

        public override void ReadCartridgePpu(ref byte dataBus)
        {
            throw new NotImplementedException();
        }
        public override void WriteCartridgePpu(byte value)
        {
            throw new NotImplementedException();
        }

        public override (bool usePpuVram, bool useFirstNametable) MapNameTable(ushort address)
        {
            throw new NotImplementedException();
        }
    }
}
