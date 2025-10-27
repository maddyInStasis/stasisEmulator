using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Cartridges
{
    public abstract class Cartridge(Rom rom)
    {
        protected Rom Rom { get; set; } = rom;

        public abstract void ReadCartridgeCpu(ushort address, ref byte dataBus);
        public abstract void WriteCartridgeCpu(ushort address, byte value);

        public abstract void ReadCartridgePpu(ushort address, ref byte dataBus);
        public abstract void WriteCartridgePpu(ushort address, byte value);

        public abstract (bool usePpuVram, bool useFirstPpuVramNametable) MapNameTable(byte nametable);
    }
}
