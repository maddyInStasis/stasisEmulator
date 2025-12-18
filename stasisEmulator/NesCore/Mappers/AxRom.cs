using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Mappers
{
    public class AxRom : Mapper
    {
        protected override ushort PrgPageSize => 0x8000;
        protected override ushort ChrPageSize => 0x2000;

        private byte _bank = 0;

        public AxRom(Rom rom, Nes nes) : base(rom, nes)
        {
            AddRegisterRange(0x8000, 0xFFFF, true);

            MapPrg();
            SelectChrPage(0, 0);

            MirrorType = NametableMirrorType.OnlyScreenA;
        }

        protected override void WriteRegisterCpu(ushort address, byte value)
        {
            _bank = (byte)(value & 7);
            MirrorType = (value & 0x10) == 0 ? NametableMirrorType.OnlyScreenA : NametableMirrorType.OnlyScreenB;
            MapPrg();
        }

        private void MapPrg()
        {
            SelectPrgPage(0, _bank);
        }
    }
}
