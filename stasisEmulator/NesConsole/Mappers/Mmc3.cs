using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Mappers
{
    public class Mmc3 : Mapper
    {
        protected override ushort PrgPageSize => 0x2000;
        protected override ushort ChrPageSize => 0x400;

        public Mmc3(Rom rom) : base(rom)
        {

        }
    }
}
