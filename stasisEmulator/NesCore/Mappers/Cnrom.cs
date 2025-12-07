using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Mappers
{
    public class Cnrom : Mapper
    {
        protected override ushort PrgPageSize => 0x8000;
        protected override ushort ChrPageSize => 0x2000;

        private bool _chrRom8KiB; //TODO: implement

        public Cnrom(Rom rom, Nes nes, bool chrRom8KiB) : base(rom, nes)
        {
            _chrRom8KiB = chrRom8KiB;
            SelectPrgPage(0, 0);
            SelectChrPage(0, 0);
            AddRegisterRange(0x8000, 0xFFFF, true);
        }

        protected override void WriteRegisterCpu(ushort address, byte value)
        {
            SelectChrPage(0, value);
        }
    }
}
