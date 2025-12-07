using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Mappers
{
    public class UxRom : Mapper
    {
        protected override ushort PrgPageSize => 0x4000;
        protected override ushort ChrPageSize => 0x2000;

        public UxRom(Rom rom, Nes nes) : base(rom, nes)
        {
            SelectPrgPage(0, 0);
            SelectPrgPage(1, -1);

            SelectChrPage(0, 0);

            AddRegisterRange(0x8000, 0xFFFF, true);
        }

        protected override void WriteRegisterCpu(ushort address, byte value)
        {
            SelectPrgPage(0, value);
        }
    }
}
