using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Cartridges
{
    public static class CartridgeFactory
    {
        public static Cartridge CreateCartridge(Rom rom)
        {
            var metadata = rom.Metadata;
            byte mapper = metadata.Mapper;

            return mapper switch
            {
                0 => new Nrom(rom),
                3 => new Cnrom(rom),
                _ => throw new Exception($"Mapper {mapper} not implemented.")
            };
        }
    }
}
