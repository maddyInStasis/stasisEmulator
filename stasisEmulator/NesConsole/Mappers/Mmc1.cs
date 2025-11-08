using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Mappers
{
    public class Mmc1(Rom rom, bool isRevisionB) : Mapper(rom)
    {
        protected override ushort PrgPageSize => throw new NotImplementedException();
        protected override ushort ChrPageSize => throw new NotImplementedException();

        private bool _isRevisionB = isRevisionB;
    }
}
