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

        public abstract void ReadCartridgeCpu(ref byte dataBus);
        public abstract void WriteCartridgeCpu(byte value);

        public abstract void ReadCartridgePpu(ref byte dataBus);
        public abstract void WriteCartridgePpu(byte value);

        //TODO: i don't remember if this is accurate, i just know the cartridge maps the nametables sorta like this?
        public abstract (bool usePpuVram, bool useFirstNametable) MapNameTable(ushort address);
    }
}
