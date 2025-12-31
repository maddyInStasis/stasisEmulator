using Microsoft.Xna.Framework;

namespace stasisEmulator.NesCore.SaveStates
{
    public class PpuState
    {
        public ushort v;
        public ushort t;
        public ushort x;
        public bool w;

        public byte OamAddress;
        public byte _secondaryOamAddress = 0;
        public bool SecondaryOamFull;

        public byte[] PaletteRam;
        public byte[] Oam;
        public byte[] SecondaryOam;

        public int Dot;
        public int Scanline;

        public bool _oddFrame;

        public bool FrameComplete;

        //PPUCTRL
        public bool VramInc32;
        public bool SpriteSecondPatternTable;
        public bool BackgroundSecondPatternTable;
        public bool Use8x16Sprites;
        public bool EnableNMI;

        //PPUMASK
        public bool Grayscale;
        public bool ShowLeftBG;
        public bool ShowLeftSprites;
        public bool RenderBG;
        public bool RenderSprites;
        public bool EmphasizeRed;
        public bool EmphasizeGreen;
        public bool EmphasizeBlue;

        //PPUSTATUS
        public bool VBlank;
        public bool VBlankDelayed;
        public bool _vblankRead = false;
        public bool Sprite0Hit;
        public bool SpriteOverflow;

        public bool _reset;

        public byte _ioBus;
        public ushort _ioBusDecayTimer;
        public byte _ioBusDecayIndex;

        public byte _readBuffer;

        public byte _spriteEvalTemp;
        public ushort _spriteAddressBus;
        public byte _spriteEvalTick;
        public bool _scanlineContainsSprite0;
        public bool _nextScanlineContainsSprite0;
        public bool _spriteEvalOverflowed;
        public byte _secondaryOamSize;
        public byte _currentSpriteCount;

        public byte[] _shiftSpritePatternLow;
        public byte[] _shiftSpritePatternHigh;

        public byte[] _spriteAttribute;
        public byte[] _spriteTileIndex;
        public byte[] _spriteXPosition;
        public byte[] _spriteYPosition;

        public ushort _bgAddressBus;
        public byte _fetchTemp;
        public byte _fetchTileIndex;

        public byte _fetchBgPatternLow;
        public byte _fetchBgPatternHigh;
        public byte _fetchBgAttribute;

        public ushort _shiftBgPatternLow;
        public ushort _shiftBgPatternHigh;
        public ushort _shiftBgAttributeLow;
        public ushort _shiftBgAttributeHigh;
    }
}
