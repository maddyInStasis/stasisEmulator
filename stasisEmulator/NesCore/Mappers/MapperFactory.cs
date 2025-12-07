using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Mappers
{
    public static class MapperFactory
    {
        public static Mapper CreateMapper(Rom rom, Nes nes)
        {
            ushort mapper = rom.Mapper;

            return mapper switch
            {
                0 => new Nrom(rom, nes),
                1 => new Mmc1(rom, nes, true),
                2 => new UxRom(rom, nes),
                3 => new Cnrom(rom, nes, false),
                4 => new Mmc3(rom, nes),
                155 => new Mmc1(rom, nes, false),
                185 => new Cnrom(rom, nes, true),
                _ => throw new Exception($"Mapper {mapper} not implemented.")
            };
        }
    }
}
