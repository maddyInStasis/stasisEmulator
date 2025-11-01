using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole
{
    public struct RomMetadata
    {
        public bool VerticalMirror;
        public bool AltNametable;

        public byte Mapper;
    }

    public struct Rom
    {
        public byte[] PrgRom;
        public byte[] ChrRom;
        public RomMetadata Metadata;
    }

    public static class RomLoader
    {
        private const int KiB = 1024;

        private static readonly byte[] _signature = [0x4E , 0x45, 0x53, 0x1A];

        //TODO: either don't throw, or catch later. idk what you're supposed to do tbh
        public static Rom LoadRom(string path)
        {
            byte[] fileBytes;

            try
            {
                fileBytes = File.ReadAllBytes(path);
            }
            catch
            {
                throw new FileNotFoundException(path);
            }

            if (fileBytes.Length < 16)
                throw new Exception("File too short to contain a .nes file header.");

            byte[] header = new byte[16];
            Array.Copy(fileBytes, header, 16);

            for (int i = 0; i < _signature.Length; i++)
            {
                if (header[i] != _signature[i])
                    throw new Exception("File does not contain .nes signature.");
            }

            Rom rom = new();

            int prgRomSize = header[4] * KiB * 16;
            rom.PrgRom = new byte[prgRomSize];
            Array.Copy(fileBytes, 16, rom.PrgRom, 0, prgRomSize);

            int chrRomSize = header[5] * KiB * 8;
            rom.ChrRom = new byte[chrRomSize];
            Array.Copy(fileBytes, 16 + prgRomSize, rom.ChrRom, 0, chrRomSize);

            RomMetadata metadata = new();

            byte flags6 = header[6];
            metadata.VerticalMirror = (flags6 & 1) != 0;
            metadata.AltNametable = (flags6 & 8) != 0;
            metadata.Mapper = (byte)(flags6 >> 4);

            rom.Metadata = metadata;

            return rom;
        }
    }
}
