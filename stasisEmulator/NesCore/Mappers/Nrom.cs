using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Mappers
{
    public class Nrom : Mapper
    {
        protected override ushort PrgPageSize => 0x4000;
        protected override ushort ChrPageSize => 0x2000;
        public Nrom(Rom rom, Nes nes) : base(rom, nes)
        {
            SelectPrgPage(0, 0);
            SelectPrgPage(1, 1);

            SelectChrPage(0, 0);
        }
    }
}
