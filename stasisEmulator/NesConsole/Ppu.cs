using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole
{
    public class Ppu
    {
        private ushort _v;
        public ushort v { set => _v = (ushort)(value & FifteenBits); get => _v; }
        private ushort _t;
        public ushort t { set => _t = (ushort)(value & FifteenBits); get => _t; }
        private ushort _x;
        public ushort x { set => _x = (ushort)(value & ThreeBits); get => _x; }
        public bool w { set; get; }

        public readonly byte[] Vram = new byte[0x800];
        public readonly byte[] PaletteRam = new byte[0x20];

        public int Dot { get; private set; } = 4;
        public int Scanline { get; private set; }

        public bool FrameComplete { get; set; }

        //PPUCTRL
        public byte NametableSelect { get; private set; }
        public bool VramInc32 { get; private set; }
        public bool SpriteSecondPatternTable { get; private set; }
        public bool BackgroundSecondPatternTable { get; private set; }
        public bool Use8x16Sprites { get; private set; }
        public bool EnableNMI { get; private set; }

        //PPUMASK
        public bool MaskLeftBG { get; private set; }
        public bool MaskLeftSprites { get; private set; }
        public bool RenderBG { get; private set; }
        public bool RenderSprites { get; private set; }

        //PPUSTATUS
        public bool VBlank { get; private set; }
        public bool Sprite0Hit { get; private set; }
        public bool SpriteOverflow { get; private set; }

        public readonly Color[] Palette = new Color[64];

        private readonly Nes _nes;

        private const ushort FifteenBits = 0xFFFF >> 1;
        private const ushort ThreeBits = 0xFF >> 5;

        public const ushort RegMask = 0x2007;

        public const ushort PPUCTRL   = 0x2000;
        public const ushort PPUMASK   = 0x2001;
        public const ushort PPUSTATUS = 0x2002;
        public const ushort OAMADDR   = 0x2003;
        public const ushort OAMDATA   = 0x2004;
        public const ushort PPUSCROLL = 0x2005;
        public const ushort PPUADDR   = 0x2006;
        public const ushort PPUDATA   = 0x2007;

        private byte _ioBus;
        private byte _readBuffer;

        //from 100th coin patreon post
        private readonly byte[] _pal = [
            0x65, 0x65, 0x65, 0x00, 0x2A, 0x84, 0x15, 0x13, 0xA2, 0x3A, 0x01, 0x9E, 0x59, 0x00, 0x7A, 0x6A, 0x00, 0x3E, 0x68, 0x08, 0x00, 0x53, 0x1D, 0x00, 
            0x32, 0x34, 0x00, 0x0D, 0x46, 0x00, 0x00, 0x4F, 0x00, 0x00, 0x4C, 0x09, 0x00, 0x3F, 0x4B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xAE, 0xAE, 0xAE, 0x17, 0x5F, 0xD6, 0x43, 0x41, 0xFF, 0x75, 0x29, 0xFA, 0x9E, 0x1D, 0xCA, 0xB4, 0x20, 0x7B, 0xB1, 0x33, 0x22, 0x96, 0x4E, 0x00, 
            0x6A, 0x6C, 0x00, 0x39, 0x84, 0x00, 0x0F, 0x90, 0x00, 0x00, 0x8D, 0x33, 0x00, 0x7B, 0x8C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFE, 0xFE, 0xFE, 0x66, 0xAF, 0xFF, 0x93, 0x90, 0xFF, 0xC5, 0x78, 0xFF, 0xEE, 0x6C, 0xFF, 0xFF, 0x6F, 0xCA, 0xFF, 0x82, 0x71, 0xE6, 0x9E, 0x25, 
            0xBA, 0xBC, 0x00, 0x88, 0xD5, 0x01, 0x5E, 0xE1, 0x32, 0x47, 0xDD, 0x82, 0x4A, 0xCB, 0xDC, 0x4E, 0x4E, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFE, 0xFE, 0xFE, 0xC0, 0xDE, 0xFF, 0xD2, 0xD1, 0xFF, 0xE7, 0xC7, 0xFF, 0xF8, 0xC2, 0xFF, 0xFF, 0xC3, 0xE9, 0xFF, 0xCB, 0xC4, 0xF5, 0xD7, 0xA5, 
            0xE2, 0xE3, 0x94, 0xCE, 0xED, 0x96, 0xBC, 0xF2, 0xAA, 0xB3, 0xF1, 0xCB, 0xB4, 0xE9, 0xF0, 0xB6, 0xB6, 0xB6, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];

        public Ppu(Nes nes)
        {
            _nes = nes;

            int i = 0;
            for (int j = 0; j < 64; j++)
            {
                Palette[j] = new Color(_pal[i++], _pal[i++], _pal[i++]);
            }
        }

        public void Power()
        {
            Sprite0Hit = false;
            v = 0;

            Reset();
        }

        public void Reset()
        {
            NametableSelect = 0;
            VramInc32 = false;
            SpriteSecondPatternTable = false;
            BackgroundSecondPatternTable = false;
            Use8x16Sprites = false;
            EnableNMI = false;

            MaskLeftBG = false;
            MaskLeftSprites = false;
            RenderBG = false;
            RenderSprites = false;

            w = false;
            t = 0;
            _readBuffer = 0;
        }

        public byte ReadRegister(ushort address, ref byte dataBus)
        {
            ushort register = (ushort)(address & RegMask);

            switch (register)
            {
                case PPUSTATUS:
                    _ioBus = (byte)(_ioBus & (0xFF >> 3));
                    _ioBus |= (byte)(SpriteOverflow ? 0x20 : 0);
                    _ioBus |= (byte)(Sprite0Hit ? 0x40 : 0);
                    _ioBus |= 0x40; //TODO: temp, remove
                    _ioBus |= (byte)(VBlank ? 0x80 : 0);
                    VBlank = false;
                    w = false;
                    break;
                case OAMDATA:
                    break;
                case PPUDATA:
                    _ioBus = _readBuffer;
                    if (v < 0x2000)
                    {
                        _nes.Cartridge?.ReadCartridgePpu(v, ref _readBuffer);
                    }
                    else if (v < 0x3F00)
                    {
                        ReadNametable(v, ref _readBuffer);
                    }
                    else
                    {
                        ReadNametable(v, ref _readBuffer);
                        if ((v & 3) == 0)
                            _ioBus = PaletteRam[v & 0x0F];
                        else
                            _ioBus = PaletteRam[v & 0x1F];
                    }
                    v += (ushort)(VramInc32 ? 32 : 1);
                    break;
            }

            dataBus = _ioBus;
            return dataBus;
        }

        public void WriteRegister(ushort address, byte value)
        {
            ushort register = (ushort)(address & RegMask);

            _ioBus = value;

            switch (register)
            {
                case PPUCTRL:
                    NametableSelect = (byte)(_ioBus & 3);
                    VramInc32 = (_ioBus & 4) != 0;
                    SpriteSecondPatternTable = (_ioBus & 8) != 0;
                    BackgroundSecondPatternTable = (_ioBus & 16) != 0;
                    Use8x16Sprites = (_ioBus & 32) != 0;
                    EnableNMI = (_ioBus & 128) != 0;
                    break;
                case PPUMASK:
                    MaskLeftBG = (_ioBus & 2) != 0;
                    MaskLeftSprites = (_ioBus & 4) != 0;
                    RenderBG = (_ioBus & 8) != 0;
                    RenderSprites = (_ioBus & 16) != 0;
                    break;
                case OAMADDR:
                    break;
                case OAMDATA:
                    break;
                case PPUSCROLL:
                    break;
                case PPUADDR:
                    if (!w)
                    {
                        //shift value into high byte, mask out uppper two bits, & with lower byte of t
                        t = (ushort)(((_ioBus << 8) & (0xFFFF >> 2)) | (t & 0x00FF));
                    }
                    else
                    {
                        //replace lower byte of t with value
                        t = (ushort)((t & 0xFF00) | _ioBus);
                        v = t;
                    }
                    w = !w;
                    break;
                case PPUDATA:
                    if (v < 0x2000)
                    {
                        _nes.Cartridge?.WriteCartridgePpu(address, _ioBus);
                    }
                    else if (v < 0x3F00)
                    {
                        WriteNametable(v, _ioBus);
                    }
                    else
                    {
                        if ((v & 3) == 0)
                            PaletteRam[v & 0x0F] = _ioBus;
                        else
                            PaletteRam[v & 0x1F] = _ioBus;
                    }
                    v += (ushort)(VramInc32 ? 32 : 1);
                    break;
            }
        }

        public void WriteNametable(ushort address, byte value)
        {
            var cart = _nes.Cartridge;
            if (cart == null)
                return;

            (ushort addrOrIndex, bool useVram) = MapNametable(address);

            if (useVram)
                Vram[addrOrIndex] = value;
            else
                cart.WriteCartridgePpu(addrOrIndex, value);
        }

        public byte ReadNametable(ushort address, ref byte dataBus)
        {
            var cart = _nes.Cartridge;
            if (cart == null)
                return dataBus;

            (ushort addrOrIndex, bool useVram) = MapNametable(address);

            if (useVram)
                dataBus = Vram[addrOrIndex];
            else
                cart.ReadCartridgePpu(addrOrIndex, ref dataBus);

            return dataBus;
        }

        public (ushort addrOrIndex, bool useVram) MapNametable(ushort address)
        {
            var cart = _nes.Cartridge;

            ushort index = (ushort)(address & (0xFFFF >> 4));
            ushort vramAddr = (ushort)(index | 0x2000);
            byte nametable = (byte)((vramAddr >> 10) & 3);
            (bool usePpuVram, bool useFirstPpuVramNametable) = cart.MapNameTable(nametable);

            if (usePpuVram)
            {
                if (useFirstPpuVramNametable)
                    return ((ushort)(index & 0x3FF), true);
                else
                    return ((ushort)((index & 0x3FF) + 0x400), true);
            }
            else
            {
                return (vramAddr, false);
            }
        }

        private bool _nmiLevelDetector;

        public void RunCycle()
        {
            bool prevNmiLevelDetector = _nmiLevelDetector;
            _nmiLevelDetector = EnableNMI && VBlank;
            if (!prevNmiLevelDetector && _nmiLevelDetector)
            {
                _nes.Cpu.DoNmi = true;
            }

            if (Dot == 1 && Scanline == 241)
            {
                VBlank = true;
                FrameComplete = true;
            }
            if (Dot == 1 && Scanline == 261)
            {
                VBlank = false;
                Sprite0Hit = false;
                SpriteOverflow = false;
            }

            Dot++;
            if (Dot > 340)
            {
                Dot = 0;
                Scanline++;
                if (Scanline > 261)
                    Scanline = 0;
            }
        }
    }
}
