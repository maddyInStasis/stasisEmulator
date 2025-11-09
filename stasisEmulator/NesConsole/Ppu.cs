using Microsoft.Xna.Framework;
using System;

namespace stasisEmulator.NesConsole
{
    public class Ppu
    {
        public ushort v;
        public ushort t;
        public ushort x;
        public bool w;

        public byte OamAddress;
        private byte _secondaryOamAddress = 0;
        public byte SecondaryOamAddress { get => _secondaryOamAddress; set => _secondaryOamAddress = (byte)(value & 0x1F); }
        public bool SecondaryOamFull;
        
        public readonly byte[] PaletteRam = new byte[0x20];
        public readonly byte[] Oam = new byte[0x100];
        public readonly byte[] SecondaryOam = new byte[0x20];

        public int Dot;
        public int Scanline;

        private bool _oddFrame;

        public bool FrameComplete;

        //PPUCTRL
        public bool VramInc32;
        public bool SpriteSecondPatternTable;
        public bool BackgroundSecondPatternTable;
        public bool Use8x16Sprites;
        public bool EnableNMI;

        //PPUMASK
        public bool ShowLeftBG;
        public bool ShowLeftSprites;
        public bool RenderBG;
        public bool RenderSprites;

        //PPUSTATUS
        public bool VBlank;
        public bool Sprite0Hit;
        public bool SpriteOverflow;

        public readonly Color[] Palette = new Color[64];

        //save an array lookup when outputting colors by storing the colors directly
        //(PaletteRamColors[i] is equivalent to Palette[PaletteRam[i]])
        public readonly Color[] PaletteRamColors = new Color[64];

        //is the current background color the same as when the frame began?
        //if so, skip outputting transparent pixels, as the output buffer is filled with the bg color ahead of time
        //limited testing, unclear if this is performant when the background barely shows through, or the bg color changes
        //(the overall effect is small, regardless)
        private bool _backgroundColorOkay = true;

        public const int PixelWidth = 256;
        public const int PixelHeight = 240;
        public readonly Color[] OutputBuffer = new Color[PixelWidth * PixelHeight];

        public const ushort RegMask = 0x2007;

        public const ushort PPUCTRL   = 0x2000;
        public const ushort PPUMASK   = 0x2001;
        public const ushort PPUSTATUS = 0x2002;
        public const ushort OAMADDR   = 0x2003;
        public const ushort OAMDATA   = 0x2004;
        public const ushort PPUSCROLL = 0x2005;
        public const ushort PPUADDR   = 0x2006;
        public const ushort PPUDATA   = 0x2007;

        private readonly Nes _nes;

        private const ushort CoarseXMask = 0b11111;
        private const ushort CoarseYMask = 0b11111 << 5;
        private const ushort NametableMask = 0b11 << 10;
        private const ushort NametableXMask = 0b01 << 10;
        private const ushort NametableYMask = 0b10 << 10;
        private const ushort FineYMask = 0b111 << 12;
        private const ushort AllXMask = (NametableXMask | CoarseXMask);
        private const ushort AllYMask = (FineYMask | NametableYMask | CoarseYMask);

        private byte _ioBus;
        private byte _readBuffer;

        private byte _spriteEvalTemp;
        private ushort _spriteAddressBus;
        private byte _spriteEvalTick;
        private bool _scanlineContainsSprite0;
        private bool _nextScanlineContainsSprite0;
        private bool _spriteEvalOverflowed;
        private byte _secondaryOamSize;
        private byte _currentSpriteCount;

        private readonly byte[] _shiftSpritePatternLow = new byte[8];
        private readonly byte[] _shiftSpritePatternHigh = new byte[8];

        private readonly byte[] _spriteAttribute = new byte[8];
        private readonly byte[] _spriteTileIndex = new byte[8];
        private readonly byte[] _spriteXPosition = new byte[8];
        private readonly byte[] _spriteYPosition = new byte[8];

        private ushort _bgAddressBus;
        private byte _fetchTemp;
        private byte _fetchTileIndex;

        private byte _fetchBgPatternLow;
        private byte _fetchBgPatternHigh;
        private byte _fetchBgAttribute;

        private ushort _shiftBgPatternLow;
        private ushort _shiftBgPatternHigh;
        private ushort _shiftBgAttributeLow;
        private ushort _shiftBgAttributeHigh;

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
            OamAddress = 0;

            Reset();
        }

        public void Reset()
        {
            VramInc32 = false;
            SpriteSecondPatternTable = false;
            BackgroundSecondPatternTable = false;
            Use8x16Sprites = false;
            EnableNMI = false;

            ShowLeftBG = false;
            ShowLeftSprites = false;
            RenderBG = false;
            RenderSprites = false;

            w = false;
            t = 0;
            _readBuffer = 0;
            _oddFrame = false;
        }

        public void Read(ushort address, ref byte dataBus)
        {
            if (address < 0x3F00)
            {
                _nes.Cartridge?.ReadPpu(address, ref dataBus);
            }
            else
            {
                byte pal;
                if ((address & 3) == 0)
                    pal = PaletteRam[address & 0x0F];
                else
                    pal = PaletteRam[address & 0x1F];
                dataBus = (byte)((dataBus & (0xF0 << 2)) | (pal & (0xFF >> 2)));
            }
        }

        public void Write(ushort address, byte dataBus)
        {
            if (v < 0x3F00)
            {
                _nes.Cartridge?.WritePpu(address, dataBus);
            }
            else
            {
                if ((address & 3) == 0)
                {
                    if ((address & 0x0F) == 0)
                        _backgroundColorOkay = false;

                    PaletteRam[address & 0x0F] = dataBus;
                    PaletteRamColors[address & 0x0F] = Palette[dataBus & 63];
                }
                else
                {
                    PaletteRam[address & 0x1F] = dataBus;
                    PaletteRamColors[address & 0x1F] = Palette[dataBus & 63];
                }
            }
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
                    _ioBus |= (byte)(VBlank ? 0x80 : 0);
                    VBlank = false;
                    w = false;
                    break;
                case OAMDATA:
                    if (Scanline < 240 && Dot >= 1 && Dot <= 64 && (RenderBG || RenderSprites))
                    {
                        _ioBus = 0xFF;
                        break;
                    }

                    if ((OamAddress & 3) != 2)
                        _ioBus = Oam[OamAddress];
                    else
                        _ioBus = (byte)((_ioBus & 0b00011100) | (Oam[OamAddress] & 0b11100011));

                    break;
                case PPUDATA:
                    if (v < 0x3F00)
                    {
                        _ioBus = _readBuffer;
                        Read(v, ref _readBuffer);
                    }
                    else
                    {
                        ReadVram(v, ref _readBuffer);
                        Read(v, ref _ioBus);
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
                    t = (ushort)((t & (0xFFFF ^ NametableMask)) | ((_ioBus & 3) << 10));
                    VramInc32 = (_ioBus & 4) != 0;
                    SpriteSecondPatternTable = (_ioBus & 8) != 0;
                    BackgroundSecondPatternTable = (_ioBus & 16) != 0;
                    Use8x16Sprites = (_ioBus & 32) != 0;
                    EnableNMI = (_ioBus & 128) != 0;
                    break;
                case PPUMASK:
                    ShowLeftBG = (_ioBus & 2) != 0;
                    ShowLeftSprites = (_ioBus & 4) != 0;
                    RenderBG = (_ioBus & 8) != 0;
                    RenderSprites = (_ioBus & 16) != 0;
                    break;
                case OAMADDR:
                    OamAddress = _ioBus;
                    break;
                case OAMDATA:
                    if (Scanline < 240)
                    {
                        OamAddress += 4;
                        break;
                    }
                    Oam[OamAddress] = _ioBus;
                    OamAddress++;
                    break;
                case PPUSCROLL:
                    if (!w)
                    {
                        t = (ushort)((t & (AllYMask | NametableMask)) | (_ioBus >> 3));
                        x = (byte)(_ioBus & 7);
                    }
                    else
                    {
                        t = (ushort)((t & (AllXMask | NametableMask)) | (_ioBus << 12) | ((_ioBus << 2) & CoarseYMask));
                    }
                    w = !w;
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
                    Write(v, _ioBus);
                    v += (ushort)(VramInc32 ? 32 : 1);
                    break;
            }
        }

        public byte ReadVram(ushort address, ref byte dataBus)
        {
            _nes.Cartridge?.ReadPpu(address, ref dataBus);
            return dataBus;
        }

        public void RunCycle()
        {
            if (Dot == 1)
            {
                if (Scanline == 241)
                {
                    VBlank = true;
                    FrameComplete = true;
                    _oddFrame = !_oddFrame;
                }
                if (Scanline == 261)
                {
                    VBlank = false;
                    Sprite0Hit = false;
                    SpriteOverflow = false;
                    _backgroundColorOkay = true;
                    Array.Fill(OutputBuffer, PaletteRamColors[0]);
                }
            }

            _nes.Cpu.NmiLine = EnableNMI && VBlank;
            
            DoSpriteEvaluation();
            DoBgRead();
            if (Dot <= 256 && Dot > 1)
            {
                if (RenderBG || RenderSprites)
                {
                    for (int i = 0; i < _currentSpriteCount; i++)
                    {
                        if (_spriteXPosition[i] > 0)
                        {
                            _spriteXPosition[i]--;
                        }
                        else
                        {
                            _shiftSpritePatternLow[i] <<= 1;
                            _shiftSpritePatternHigh[i] <<= 1;
                        }
                    }
                }
            }

            if (Scanline < 240 && Dot > 0 && Dot <= 256)
            {
                byte bgPalette = 0;
                byte bgPaletteIndex = 0;
                if (RenderBG && (Dot > 8 || ShowLeftBG))
                {
                    byte colLow = (byte)((_shiftBgPatternLow >> (15 - x)) & 1);
                    byte colHigh = (byte)((_shiftBgPatternHigh >> (15 - x)) & 1);
                    bgPaletteIndex = (byte)((colHigh << 1) | colLow);

                    byte paletteLow = (byte)((_shiftBgAttributeLow >> (15 - x)) & 1);
                    byte paletteHigh = (byte)((_shiftBgAttributeHigh >> (15 - x)) & 1);
                    bgPalette = (byte)((paletteHigh << 1) | paletteLow);

                    if (bgPaletteIndex == 0)
                        bgPalette = 0;
                }

                byte spritePalette = 0;
                byte spritePaletteIndex = 0;
                bool spritePriority = false;
                if (RenderSprites && (Dot > 8 || ShowLeftSprites))
                {
                    for (int i = 0; i < _currentSpriteCount; i++)
                    {
                        if (_spriteXPosition[i] > 0)
                            continue;

                        byte spriteColLow = (byte)(_shiftSpritePatternLow[i] >> 7);
                        byte spriteColHigh = (byte)(_shiftSpritePatternHigh[i] >> 7);
                        spritePaletteIndex = (byte)((spriteColHigh << 1) | spriteColLow);

                        if (spritePaletteIndex == 0)
                            continue;

                        spritePalette = (byte)((_spriteAttribute[i] & 0x03) | 0x04);
                        spritePriority = (_spriteAttribute[i] & 0x20) == 0;

                        if (i == 0 && _scanlineContainsSprite0 && spritePaletteIndex != 0 && bgPaletteIndex != 0 && RenderBG && Dot < 256)
                        {
                            Sprite0Hit = true;
                        }

                        break;
                    }
                }

                bool drawSprite = (spritePriority || bgPaletteIndex == 0) && spritePaletteIndex != 0;
                byte palette = drawSprite ? spritePalette : bgPalette;
                byte paletteIndex = drawSprite ? spritePaletteIndex : bgPaletteIndex;

                //maybe instead of calculating the index based on the scanline and dot, we could just increment an index variable?
                if (paletteIndex != 0 || !_backgroundColorOkay)
                    OutputBuffer[Scanline * 256 + (Dot - 1)] = PaletteRamColors[(palette * 4 + paletteIndex) & 63];
            }
        
            Dot++;
            if (Dot > 340)
            {
                Dot = 0;
                Scanline++;
                if (Scanline > 261)
                    Scanline = 0;
            }

            if (Dot == 0 && Scanline == 0 && !_oddFrame)
                Dot++;
        }

        private void DoBgRead()
        {
            if (Scanline > 239 && Scanline != 261)
                return;

            if (!RenderBG && !RenderSprites)
                return;

            if ((Dot < 257 && Dot > 0) || (Dot > 320 && Dot < 337))
            {
                _shiftBgPatternLow <<= 1;
                _shiftBgPatternHigh <<= 1;
                _shiftBgAttributeLow <<= 1;
                _shiftBgAttributeHigh <<= 1;

                switch ((Dot - 1) & 7)
                {
                    case 0:
                        _shiftBgPatternLow = (ushort)((_shiftBgPatternLow & 0xFF00) | _fetchBgPatternLow);
                        _shiftBgPatternHigh = (ushort)((_shiftBgPatternHigh & 0xFF00) | _fetchBgPatternHigh);
                        _shiftBgAttributeLow = (ushort)((_shiftBgAttributeLow & 0xFF00) | ((_fetchBgAttribute & 1) != 0 ? 0xFF : 0));
                        _shiftBgAttributeHigh = (ushort)((_shiftBgAttributeHigh & 0xFF00) | ((_fetchBgAttribute & 2) != 0 ? 0xFF : 0));
                        //v = yyyNNYYYYYXXXXX
                        //nametable start address + NNYYYYYXXXXX
                        _bgAddressBus = (ushort)(0x2000 + (v & (NametableMask | CoarseYMask | CoarseXMask)));
                        Read(_bgAddressBus, ref _fetchTemp);
                        break;
                    case 1:
                        _fetchTileIndex = _fetchTemp;
                        break;
                    case 2:
                        //v = yyyNNYYYYYXXXXX
                        //(attribute start address) | (nametable select) | ((upper 3 bits of Y) << 3) | (upper 3 bits of X)
                        _bgAddressBus = (ushort)(0x23C0 | (v & NametableMask) | (((v >> 7) & 0b111) << 3) | ((v >> 2) & 0b111));
                        Read(_bgAddressBus, ref _fetchTemp);
                        break;
                    case 3:
                        _fetchBgAttribute = _fetchTemp;
                        //v = yyyNNYYYYYXXXXX
                        //is bit 1 of X set?
                        if ((v & 0b10) != 0)
                        {
                            _fetchBgAttribute >>= 2;
                        }
                        //is bit 1 of Y set?
                        if ((v & 0b1000000) != 0)
                        {
                            _fetchBgAttribute >>= 4;
                        }
                        _fetchBgAttribute &= 3;
                        break;
                    case 4:
                        //v = yyyNNYYYYYXXXXX
                        //yyy (pixel row) + (tile index * tile size)
                        //add second pattern table offset if using second pattern table
                        _bgAddressBus = (ushort)(((v & FineYMask) >> 12) + _fetchTileIndex * 16 + (BackgroundSecondPatternTable ? 0x1000 : 0));
                        Read(_bgAddressBus, ref _fetchTemp);
                        break;
                    case 5:
                        _fetchBgPatternLow = _fetchTemp;
                        _bgAddressBus += 8;
                        break;
                    case 6:
                        Read(_bgAddressBus, ref _fetchTemp);
                        break;
                    case 7:
                        _fetchBgPatternHigh = _fetchTemp;
                        IncrementScrollX();
                        break;
                }

                if (Dot == 256)
                    IncrementScrollY();

                return;
            }
            if (Dot >= 280 && Dot <= 304 && Scanline == 261)
            {
                ResetScrollY();
                return;
            }

            if (Dot == 257)
                ResetScrollX();
        }

        public void IncrementScrollX()
        {
            //is XXXXX at its max value?
            if ((v & CoarseXMask) == CoarseXMask)
            {
                //handle nametable crossing ($2400 should cross back to $2000, not increment to $2800)
                //clear XXXXX
                v &= (0xFFFF ^ CoarseXMask);
                //flip bit 0 of NN
                v ^= NametableXMask;
            }
            else
            {
                v++;
            }
        }

        public void IncrementScrollY()
        {
            if ((v & FineYMask) != FineYMask)
            {
                //increment yyy
                v += 0x1000;
            }
            else
            {
                v &= (NametableMask | CoarseYMask | CoarseXMask);
                int coarseY = (v & CoarseYMask) >> 5;
                if (coarseY == 29) //only 30 tiles vertically (starting at 0)
                {
                    //handle nametable crossing ($2000 should cross to $2800, not increment to $2400)
                    //clear YYYYY
                    coarseY = 0;
                    //flip bit 1 of NN
                    v ^= NametableYMask;
                }
                else
                {
                    coarseY++;
                }
                //clear old coarse y, replace with new coarse y
                v = (ushort)((v & (0xFFFF ^ CoarseYMask)) | (coarseY << 5));
            }
        }

        public void ResetScrollX()
        {
            v = (ushort)((v & AllYMask) | (t & AllXMask));
        }

        public void ResetScrollY()
        {
            v = (ushort)((v & AllXMask) | (t & AllYMask));
        }

        private void DoSpriteEvaluation()
        {
            if (Scanline > 239)
                return;

            if (Dot <= 64)
                DoOamClear();
            if (Dot > 64 && Dot <= 256)
                EvaluateSprites();
            if (Dot > 256 && Dot <= 320)
            {
                OamAddress = 0;
                if (Dot == 257)
                {
                    _secondaryOamSize = SecondaryOamAddress;
                    if (SecondaryOamFull)
                        _secondaryOamSize = 0x20;
                    _currentSpriteCount = (byte)(_secondaryOamSize / 4);
                    SecondaryOamAddress = 0;
                    _spriteEvalTick = 0;
                }

                byte secondaryOamSlot = (byte)(SecondaryOamAddress >> 2);

                switch(_spriteEvalTick)
                {
                    case 0:
                        _spriteYPosition[secondaryOamSlot] = SecondaryOam[SecondaryOamAddress];
                        SecondaryOamAddress++;
                        break;
                    case 1:
                        _spriteTileIndex[secondaryOamSlot] = SecondaryOam[SecondaryOamAddress];
                        SecondaryOamAddress++;
                        break;
                    case 2:
                        _spriteAttribute[secondaryOamSlot] = SecondaryOam[SecondaryOamAddress];
                        SecondaryOamAddress++;
                        break;
                    case 3:
                        _spriteXPosition[secondaryOamSlot] = SecondaryOam[SecondaryOamAddress];
                        break;
                    case 4:
                        byte spriteTileIndex = _spriteTileIndex[secondaryOamSlot];
                        bool flipVert = (_spriteAttribute[secondaryOamSlot] & 0x80) != 0;
                        byte row = (byte)(Scanline - _spriteYPosition[secondaryOamSlot]);

                        ushort tileAddress = (ushort)(SpriteSecondPatternTable ? 0x1000 : 0);
                        if (Use8x16Sprites)
                        {
                            //in 8x16 mode, bit 0 determines the pattern table
                            tileAddress = (ushort)((spriteTileIndex & 1) != 0 ? 0x1000 : 0);
                            //clear bit 0, so spriteTileIndex represents the actual tile index, including bit 0
                            spriteTileIndex &= 0xFE;
                            //if row > 7, we're in the second tile, so increment the tile index
                            if (row > 7)
                                spriteTileIndex++;
                            //if the sprite is vertically flipped, flip bit 1, effectively swapping the upper and lower tile
                            if (flipVert)
                                spriteTileIndex ^= 1;
                        }

                        tileAddress += (ushort)(spriteTileIndex * 16);

                        //AND the row with 7, so the row is now limited to the tile (only relevant with 8x16 sprites)
                        row &= 7;
                        if (flipVert)
                            row = (byte)(7 - row);

                        _spriteAddressBus = (ushort)(tileAddress + row);
                        break;
                    case 5:
                        Read(_spriteAddressBus, ref _spriteEvalTemp);
                        if (Scanline == 261)
                        {
                            _spriteEvalTemp = 0;
                        }
                        //flip x
                        if ((_spriteAttribute[secondaryOamSlot] & 0x40) != 0)
                        {
                            //reverse bit order
                            _spriteEvalTemp = (byte)(((_spriteEvalTemp & 0xF0) >> 4) | ((_spriteEvalTemp & 0x0F) << 4));
                            _spriteEvalTemp = (byte)(((_spriteEvalTemp & 0xCC) >> 2) | ((_spriteEvalTemp & 0x33) << 2));
                            _spriteEvalTemp = (byte)(((_spriteEvalTemp & 0xAA) >> 1) | ((_spriteEvalTemp & 0x55) << 1));
                        }
                        _shiftSpritePatternLow[secondaryOamSlot] = _spriteEvalTemp;
                        break;
                    case 6:
                        _spriteAddressBus += 8;
                        break;
                    case 7:
                        Read(_spriteAddressBus, ref _spriteEvalTemp);
                        if (Scanline == 261)
                        {
                            _spriteEvalTemp = 0;
                        }
                        //flip x
                        if ((_spriteAttribute[secondaryOamSlot] & 0x40) != 0)
                        {
                            //reverse bit order
                            _spriteEvalTemp = (byte)(((_spriteEvalTemp & 0xF0) >> 4) | ((_spriteEvalTemp & 0x0F) << 4));
                            _spriteEvalTemp = (byte)(((_spriteEvalTemp & 0xCC) >> 2) | ((_spriteEvalTemp & 0x33) << 2));
                            _spriteEvalTemp = (byte)(((_spriteEvalTemp & 0xAA) >> 1) | ((_spriteEvalTemp & 0x55) << 1));
                        }
                        _shiftSpritePatternHigh[secondaryOamSlot] = _spriteEvalTemp;
                        SecondaryOamAddress++;
                        break;
                }
                _spriteEvalTick++;
                _spriteEvalTick &= 7;
            }
        }

        private void DoOamClear()
        {
            if (Dot == 0)
            {
                SecondaryOamFull = false;
                _spriteEvalOverflowed = false;

                if (Scanline == 0)
                    _nextScanlineContainsSprite0 = false;

                _scanlineContainsSprite0 = _nextScanlineContainsSprite0;
                _nextScanlineContainsSprite0 = false;
                return;
            }

            if ((Dot & 1) == 1)
            {
                _spriteEvalTemp = 0xFF;
            }
            else
            {
                SecondaryOam[SecondaryOamAddress] = _spriteEvalTemp;
                SecondaryOamAddress++;
            }
        }

        private void EvaluateSprites()
        {
            if (_spriteEvalOverflowed)
                return;

            if ((Dot & 1) == 1)
            {
                _spriteEvalTemp = Oam[OamAddress];
                return;
            }

            if (!SecondaryOamFull)
            {
                SecondaryOam[SecondaryOamAddress] = _spriteEvalTemp;
            }
            if (_spriteEvalTick == 0)
            {
                if (Scanline - _spriteEvalTemp >= 0 && Scanline - _spriteEvalTemp < (Use8x16Sprites ? 16 : 8)) //sprite on scanline
                {
                    if (!SecondaryOamFull)
                    {
                        if (Dot == 66)
                            _nextScanlineContainsSprite0 = true;
                        SecondaryOamAddress++;
                        OamAddress++;
                    }
                    else
                    {
                        SpriteOverflow = true;
                    }
                    _spriteEvalTick++;
                }
                else
                {
                    OamAddress += 4;
                }
            }
            else
            {
                SecondaryOamAddress++;
                OamAddress++;
                if (SecondaryOamAddress == 0)
                    SecondaryOamFull = true;
                _spriteEvalTick++;
                _spriteEvalTick &= 3;
            }
            if (OamAddress == 0)
            {
                _spriteEvalOverflowed = true;
            }
        }
    }
}
