using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace stasisEmulator.NesConsole
{
    public struct Rom
    {
        public byte[] Trainer;
        public byte[] PrgRom;
        public byte[] ChrRom;

        public int PrgRomSizeBytes;
        public int PrgWorkRamSizeBytes;
        public int PrgSaveRamSizeBytes;

        public int ChrRomSizeBytes;
        public int ChrRamSizeBytes;
        public int ChrNvRamSizeBytes;

        public RomFormatVersion Version;

        public bool VerticalMirror;
        public bool HasPrgRam;
        public bool HasTrainer;
        public bool AltNametable;
        public ushort Mapper;

        public byte ConsoleType;

        public byte Submapper;
    }

    public enum RomFormatVersion
    {
        ArchaicINes,
        INes07,
        INes,
        Nes20,
    }

    public static class RomLoader
    {
        private const int KiB = 1024;

        private const int HeaderLength = 16;
        private const int TrainerLength = 512;

        private const int PrgRomSizeInterval = KiB * 16;
        private const int ChrRomSizeInterval = KiB * 8;

        private static readonly byte[] _signature = [0x4E , 0x45, 0x53, 0x1A];

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

            if (fileBytes.Length < HeaderLength)
                throw new Exception("File too short to contain a .nes file header.");

            byte[] header = new byte[16];
            Array.Copy(fileBytes, header, 16);

            for (int i = 0; i < _signature.Length; i++)
            {
                if (header[i] != _signature[i])
                    throw new Exception("File does not contain .nes signature.");
            }

            Rom rom = new();

            //byte 4
            rom.PrgRomSizeBytes = header[4] * PrgRomSizeInterval;

            //byte 5
            rom.ChrRomSizeBytes = header[5] * ChrRomSizeInterval;

            //byte 6
            byte flags6 = header[6];
            rom.VerticalMirror = (flags6 & 1) != 0;
            rom.HasPrgRam = (flags6 & 2) != 0;
            rom.HasTrainer = (flags6 & 4) != 0;
            rom.AltNametable = (flags6 & 8) != 0;
            rom.Mapper = (ushort)(flags6 >> 4);

            switch (header[7] & 0x0C)
            {
                case 0:
                    for (int i = 12; i < HeaderLength; i++)
                    {
                        if (header[i] != 0)
                        {
                            rom.Version = RomFormatVersion.INes07;
                            break;
                        }
                    }
                    rom.Version = RomFormatVersion.INes;
                    break;
                case 4:
                    rom.Version = RomFormatVersion.ArchaicINes;
                    break;
                case 8:
                    rom.Version = RomFormatVersion.Nes20;
                    break;
                default:
                    rom.Version = RomFormatVersion.ArchaicINes;
                    break;
            }

            if (rom.ChrRomSizeBytes == 0)
                rom.ChrRamSizeBytes = 8 * KiB;

            if (rom.Version != RomFormatVersion.ArchaicINes)
                ReadFlags7(header, ref rom);

            if (rom.Version == RomFormatVersion.INes)
                ReadINes(header, ref rom);

            if (rom.Version == RomFormatVersion.Nes20)
                ReadNes20(header, ref rom);

            int offset = HeaderLength;
            int expectedLength = HeaderLength + (rom.HasTrainer ? TrainerLength : 0) + rom.PrgRomSizeBytes + rom.ChrRomSizeBytes;

            Debug.Assert(fileBytes.Length >= expectedLength, $"File too short for contents specified in header. (Expected at least {expectedLength} bytes, including the header)");

            if (rom.HasTrainer)
            {
                rom.Trainer = new byte[TrainerLength];
                Array.Copy(fileBytes, offset, rom.Trainer, 0, TrainerLength);
            }

            rom.PrgRom = new byte[rom.PrgRomSizeBytes];
            Array.Copy(fileBytes, offset, rom.PrgRom, 0, rom.PrgRomSizeBytes);

            offset += rom.PrgRomSizeBytes;

            rom.ChrRom = new byte[rom.ChrRomSizeBytes];
            Array.Copy(fileBytes, offset, rom.ChrRom, 0, rom.ChrRomSizeBytes);

            return rom;
        }

        private static void ReadFlags7(byte[] header, ref Rom rom)
        {
            byte flags7 = header[7];
            rom.ConsoleType = (byte)(flags7 & 3);
            rom.Mapper |= (byte)(flags7 & 0xF0);
        }

        private static void ReadINes(byte[] header, ref Rom rom)
        {
            rom.PrgWorkRamSizeBytes = header[8];
        }

        private static void ReadNes20(byte[] header, ref Rom rom)
        {
            byte flags8 = header[8];
            rom.Mapper |= (ushort)((flags8 & 0x0F) << 8);
            rom.Submapper = (byte)((flags8 & 0xF0) >> 4);

            byte flags9 = header[9];
            rom.PrgRomSizeBytes += ((flags9 & 0x0F) << 8) * PrgRomSizeInterval;
            rom.ChrRomSizeBytes += ((flags9 & 0xF0) << 4) * ChrRomSizeInterval;

            byte flags10 = header[10];

            int prgRamShiftCount = flags10 & 0x0F;
            if (prgRamShiftCount > 0)
                rom.PrgWorkRamSizeBytes = 64 << prgRamShiftCount;

            int nvRamShiftCount = (flags10 & 0xF0) >> 4;
            if (nvRamShiftCount > 0)
                rom.PrgSaveRamSizeBytes = 64 << nvRamShiftCount;

            byte flags11 = header[11];

            int chrRamShiftCount = flags11 & 0x0F;
            if (chrRamShiftCount > 0)
                rom.ChrRamSizeBytes = 64 << chrRamShiftCount;

            int chrNvRamShiftCount = (flags11 & 0xF0) >> 4;
            if (chrNvRamShiftCount > 0)
                rom.ChrNvRamSizeBytes = 64 << chrNvRamShiftCount;

            //the rest are probably not relevant right now
        }
    }
}
