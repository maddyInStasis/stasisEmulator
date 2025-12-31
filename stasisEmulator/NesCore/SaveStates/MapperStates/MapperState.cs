using stasisEmulator.NesCore.Mappers;

namespace stasisEmulator.NesCore.SaveStates.MapperStates
{
    public class MapperState
    {
        public byte[] PrgRom;
        public byte[] WorkRam;
        public byte[] SaveRam;

        public byte[] ChrRom;
        public byte[] ChrRam;
        public byte[] Vram = new byte[0x1000];

        public Mapper.NametableMirrorType _mirrorType;

        public bool[] _isReadRegister = new bool[0x10000];
        public bool[] _isWriteRegister = new bool[0x10000];

        public int[] _prgSourceOffsets = new int[0x100];
        public int[] _chrSourceOffsets = new int[0x100];

        public Mapper.PrgMemoryType[] _prgMemoryTypes = new Mapper.PrgMemoryType[0x100];
        public Mapper.ChrMemoryType[] _chrMemoryTypes = new Mapper.ChrMemoryType[0x100];

        public Mapper.MemoryAccessType[] _prgAccessTypes = new Mapper.MemoryAccessType[0x100];
        public Mapper.MemoryAccessType[] _chrAccessTypes = new Mapper.MemoryAccessType[0x100];
    }
}
