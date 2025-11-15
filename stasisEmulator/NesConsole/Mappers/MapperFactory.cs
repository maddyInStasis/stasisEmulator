using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Mappers
{
    public static class MapperFactory
    {
        public static Mapper CreateMapper(Rom rom)
        {
            ushort mapper = rom.Mapper;

            return mapper switch
            {
                0 => new Nrom(rom),
                1 => new Mmc1(rom, true),
                2 => new UxRom(rom),
                3 => new Cnrom(rom, false),
                4 => new Mmc3(rom),
                155 => new Mmc1(rom, false),
                185 => new Cnrom(rom, true),
                _ => throw new Exception($"Mapper {mapper} not implemented.")
            };
        }
    }
}
