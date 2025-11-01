using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Cartridges
{
    public class Nrom : Cartridge
    {
        private readonly byte[] ChrRam;

        public Nrom(Rom rom) : base(rom)
        {
            if (rom.ChrRom.Length == 0)
                ChrRam = new byte[0x2000];
        }

        public override void ReadCartridgeCpu(ushort address, ref byte dataBus)
        {
            if (address >= 0x8000)
                dataBus = Rom.PrgRom[(address - 0x8000) % Rom.PrgRom.Length];
        }
        public override void WriteCartridgeCpu(ushort address, byte value) { }

        public override void ReadCartridgePpu(ushort address, ref byte dataBus)
        {
            if (address < 0x2000)
            {
                if (ChrRam != null && ChrRam.Length > 0)
                    dataBus = ChrRam[address % ChrRam.Length];
                else
                    dataBus = Rom.ChrRom[address % Rom.ChrRom.Length];
            }
        }
        public override void WriteCartridgePpu(ushort address, byte value)
        {
            if (address < 0x2000)
            {
                if (ChrRam != null && ChrRam.Length > 0)
                    ChrRam[address % ChrRam.Length] = value;
            }
        }

        public override (bool usePpuVram, bool useFirstPpuVramNametable) MapNameTable(byte nametable)
        {
            if (Rom.Metadata.VerticalMirror)
                return (true, (nametable & 1) == 0);
            else
                return (true, nametable > 1);
        }
    }
}
