using System;

namespace stasisEmulator.NesConsole.Mappers
{
    public abstract class Mapper
    {
        protected enum PrgMemoryType
        {
            None = 0,
            PrgRom,
            WorkRam,
            SaveRam,
            Mapper
        }

        protected enum ChrMemoryType
        {
            Default = 0,
            ChrRom,
            ChrRam,
            Vram,
            Mapper
        }

        protected enum MemoryAccessType
        {
            Default = -1,
            None = 0,
            Read = 1,
            Write = 2,
            ReadWrite = 3
        }

        protected enum NametableMirrorType
        {
            Horizontal,
            Vertical,
            OnlyScreenA,
            OnlyScreenB,
            FourScreen
        }

        protected Rom Rom { get; set; }
        protected readonly Nes _nes;

        protected readonly byte[] PrgRom;
        protected readonly byte[] WorkRam;
        protected readonly byte[] SaveRam;

        protected readonly byte[] ChrRom;
        protected readonly byte[] ChrRam;
        protected byte[] Vram = new byte[0x1000];

        //based on mesen, thanks open source

        /// <summary>
        /// Should represent the smallest PRG bank size at or after address $8000
        /// </summary>
        protected abstract ushort PrgPageSize { get; }
        /// <summary>
        /// Should represent the smallest CHR bank size.
        /// </summary>
        protected abstract ushort ChrPageSize { get; }

        private NametableMirrorType _mirrorType;
        protected NametableMirrorType MirrorType
        {
            set
            {
                _mirrorType = value;
                MapNametables();
            }
            get => _mirrorType;
        }

        private readonly bool[] _isReadRegister = new bool[0x10000];
        private readonly bool[] _isWriteRegister = new bool[0x10000];

        private readonly int[] _prgSourceOffsets = new int[0x100];
        private readonly int[] _chrSourceOffsets = new int[0x100];

        private readonly PrgMemoryType[] _prgMemoryTypes = new PrgMemoryType[0x100];
        private readonly ChrMemoryType[] _chrMemoryTypes = new ChrMemoryType[0x100];

        private readonly MemoryAccessType[] _prgAccessTypes = new MemoryAccessType[0x100];
        private readonly MemoryAccessType[] _chrAccessTypes = new MemoryAccessType[0x100];

        public Mapper(Rom rom, Nes nes)
        {
            Rom = rom;

            PrgRom = rom.PrgRom;
            WorkRam = new byte[rom.PrgWorkRamSizeBytes];
            SaveRam = new byte[rom.PrgSaveRamSizeBytes];

            ChrRom = rom.ChrRom;
            ChrRam = new byte[rom.ChrRamSizeBytes];

            MirrorType = rom.VerticalMirror ? NametableMirrorType.Vertical : NametableMirrorType.Horizontal;
            _nes = nes;
        }

        //somehow this is faster than just calculating the index on a per-mapper basis???
        //what???? like, significantly faster. it makes no sense
        public void ReadCpu(ushort address, ref byte dataBus)
        {
            if (_isReadRegister[address])
            {
                ReadRegisterCpu(address, ref dataBus);
                return;
            }

            byte page = (byte)(address >> 8);
            byte pageIndex = (byte)(address & 0xFF);

            var access = _prgAccessTypes[page];
            if ((access & MemoryAccessType.Read) == 0)
                return;

            var type = _prgMemoryTypes[page];

            if (type == PrgMemoryType.Mapper)
            {
                MapperReadCpu(address, ref dataBus);
                return;
            }

            int sourcePage = _prgSourceOffsets[page];

            byte[] array = type switch
            {
                PrgMemoryType.PrgRom => PrgRom,
                PrgMemoryType.WorkRam => WorkRam,
                PrgMemoryType.SaveRam => SaveRam,
                _ => null
            };

            if (array == null || array.Length == 0)
                return;

            int sourceIndex = (sourcePage << 8) + pageIndex;
            sourceIndex %= array.Length;

            dataBus = array[sourceIndex];
        }
        public byte DebugReadCpu(ushort address)
        {
            byte value = (byte)(address >> 8);

            if (_isReadRegister[address])
            {
                return value;
            }

            byte page = (byte)(address >> 8);
            byte pageIndex = (byte)(address & 0xFF);

            var type = _prgMemoryTypes[page];

            if (type == PrgMemoryType.Mapper)
            {
                MapperReadCpu(address, ref value);
                return value;
            }

            int sourcePage = _prgSourceOffsets[page];

            byte[] array = type switch
            {
                PrgMemoryType.PrgRom => PrgRom,
                PrgMemoryType.WorkRam => WorkRam,
                PrgMemoryType.SaveRam => SaveRam,
                _ => null
            };

            if (array == null || array.Length == 0)
                return value;

            int sourceIndex = (sourcePage << 8) + pageIndex;
            sourceIndex %= array.Length;

            return array[sourceIndex];
        }
        protected virtual void ReadRegisterCpu(ushort address, ref byte dataBus) { }
        protected virtual void MapperReadCpu(ushort address, ref byte dataBus) { } //mapper-specific memory

        public void WriteCpu(ushort address, byte value)
        {
            if (_isWriteRegister[address])
            {
                WriteRegisterCpu(address, value);
                return;
            }

            byte page = (byte)(address >> 8);
            byte pageIndex = (byte)(address & 0xFF);

            var access = _prgAccessTypes[page];
            if ((access & MemoryAccessType.Write) == 0)
                return;

            var type = _prgMemoryTypes[page];

            if (type == PrgMemoryType.PrgRom)
                return;

            if (type == PrgMemoryType.Mapper)
            {
                MapperWriteCpu(address, value);
                return;
            }

            int sourcePage = _prgSourceOffsets[page];

            byte[] array = type switch
            {
                PrgMemoryType.PrgRom => PrgRom,
                PrgMemoryType.WorkRam => WorkRam,
                PrgMemoryType.SaveRam => SaveRam,
                _ => null
            };

            if (array == null || array.Length == 0)
                return;

            int sourceIndex = (sourcePage << 8) + pageIndex;
            sourceIndex %= array.Length;

            array[sourceIndex] = value;
        }
        protected virtual void WriteRegisterCpu(ushort address, byte value) { }
        protected virtual void MapperWriteCpu(ushort address, byte value) { }

        protected void SelectPrgPage(byte destinationPage, int sourcePage, PrgMemoryType memoryType = PrgMemoryType.PrgRom, MemoryAccessType memoryAccess = MemoryAccessType.Default)
        {
            ushort startAddress = (ushort)(0x8000 + (destinationPage * PrgPageSize));
            ushort endAddress = (ushort)(startAddress + PrgPageSize - 1);
            int sourceOffset = sourcePage * PrgPageSize;

            SetPrgMemoryMapping(startAddress, endAddress, sourceOffset, memoryType, memoryAccess);
        }
        protected void SetPrgMemoryMapping(ushort startAddress, ushort endAddress, int sourceOffset, PrgMemoryType memoryType, MemoryAccessType memoryAccess = MemoryAccessType.Default)
        {
            byte startPage = (byte)(startAddress >> 8);
            byte pageCount = (byte)((endAddress + 1 - startAddress) >> 8);

            if (sourceOffset < 0)
                sourceOffset += memoryType switch
                {
                    PrgMemoryType.PrgRom => PrgRom.Length,
                    PrgMemoryType.WorkRam => WorkRam.Length,
                    PrgMemoryType.SaveRam => SaveRam.Length,
                    _ => sourceOffset
                };

            if (memoryAccess == MemoryAccessType.Default)
                memoryAccess = memoryType switch
                {
                    PrgMemoryType.None => MemoryAccessType.None,
                    PrgMemoryType.PrgRom => MemoryAccessType.Read,
                    PrgMemoryType.WorkRam => MemoryAccessType.ReadWrite,
                    PrgMemoryType.SaveRam => MemoryAccessType.ReadWrite,
                    PrgMemoryType.Mapper => throw new Exception("Mapper-specific memory must explicitly define access type."),
                    _ => throw new Exception($"Memory type {memoryType} does not have a specified default access type.")
                };

            for (int i = 0; i < pageCount; i++)
            {
                int index = i + startPage;
                _prgMemoryTypes[index] = memoryType;
                _prgAccessTypes[index] = memoryAccess;
                _prgSourceOffsets[index] = (sourceOffset >> 8) + i;
            }
        }

        protected void SelectChrPage(byte destinationPage, int sourcePage, ChrMemoryType memoryType = ChrMemoryType.Default, MemoryAccessType memoryAccess = MemoryAccessType.Default)
        {
            //TODO: chr page size may depend on the memory type
            ushort startAddress = (ushort)(destinationPage * ChrPageSize);
            ushort endAddress = (ushort)(startAddress + ChrPageSize - 1);
            int sourceOffset = sourcePage * ChrPageSize;

            SetChrMemoryMapping(startAddress, endAddress, sourceOffset, memoryType, memoryAccess);
        }
        protected void SetChrMemoryMapping(ushort startAddress, ushort endAddress, int sourceOffset, ChrMemoryType memoryType, MemoryAccessType memoryAccess = MemoryAccessType.Default)
        {
            byte startPage = (byte)(startAddress >> 8);
            byte pageCount = (byte)((endAddress + 1 - startAddress) >> 8);

            if (sourceOffset < 0)
                sourceOffset += memoryType switch
                {
                    ChrMemoryType.Default => ChrRom.Length > 0 ? ChrRom.Length : ChrRam.Length,
                    ChrMemoryType.ChrRom => ChrRom.Length,
                    ChrMemoryType.ChrRam => ChrRam.Length,
                    _ => sourceOffset
                };

            if (memoryAccess == MemoryAccessType.Default)
                memoryAccess = memoryType switch
                {
                    ChrMemoryType.Default => MemoryAccessType.Default,
                    ChrMemoryType.ChrRom => MemoryAccessType.Read,
                    ChrMemoryType.ChrRam => MemoryAccessType.ReadWrite,
                    ChrMemoryType.Vram => MemoryAccessType.ReadWrite,
                    ChrMemoryType.Mapper => throw new Exception("Mapper-specific memory must explicitly define access type."),
                    _ => throw new Exception($"Memory type {memoryType} does not have a specified default access type.")
                };

            for (int i = 0; i < pageCount; i++)
            {
                int index = i + startPage;
                _chrMemoryTypes[index] = memoryType;
                _chrAccessTypes[index] = memoryAccess;
                _chrSourceOffsets[index] = (sourceOffset >> 8) + i;
            }
        }

        protected void AddRegisterRange(ushort startAddress, ushort endAddress, bool isWriteRegister)
        {
            for (int i = startAddress; i <= endAddress; i++)
            {
                if (isWriteRegister)
                    _isWriteRegister[i] = true;
                else
                    _isReadRegister[i] = true;
            }
        }
        protected void RemoveRegisterRange(ushort startAddress, ushort endAddress, bool isWriteRegister)
        {
            for (int i = startAddress; i <= endAddress; i++)
            {
                if (isWriteRegister)
                    _isWriteRegister[i] = false;
                else
                    _isReadRegister[i] = false;
            }
        }

        public void ReadPpu(ushort address, ref byte dataBus)
        {
            byte page = (byte)(address >> 8);
            byte pageIndex = (byte)(address & 0xFF);

            var type = _chrMemoryTypes[page];

            var access = _chrAccessTypes[page];
            if (access == MemoryAccessType.Default)
                access = type == ChrMemoryType.ChrRom ? MemoryAccessType.Read : MemoryAccessType.ReadWrite;

            if ((access & MemoryAccessType.Read) == 0)
                return;

            if (type == ChrMemoryType.Mapper)
            {
                MapperReadPpu(address, ref dataBus);
                return;
            }

            int sourcePage = _chrSourceOffsets[page];

            byte[] array = type switch
            {
                ChrMemoryType.ChrRom => ChrRom,
                ChrMemoryType.ChrRam => ChrRam,
                ChrMemoryType.Vram => Vram,
                ChrMemoryType.Default => ChrRom.Length > 0 ? ChrRom : ChrRam,
                _ => null
            };

            if (array == null || array.Length == 0)
                return;

            int sourceIndex = (sourcePage << 8) + pageIndex;
            sourceIndex %= array.Length;

            dataBus = array[sourceIndex];
        }
        public byte DebugReadPpu(ushort address)
        {
            byte value = (byte)(address >> 8);

            byte page = (byte)(address >> 8);
            byte pageIndex = (byte)(address & 0xFF);

            var type = _chrMemoryTypes[page];

            if (type == ChrMemoryType.Mapper)
            {
                MapperReadPpu(address, ref value);
                return value;
            }

            int sourcePage = _chrSourceOffsets[page];

            byte[] array = type switch
            {
                ChrMemoryType.ChrRom => ChrRom,
                ChrMemoryType.ChrRam => ChrRam,
                ChrMemoryType.Vram => Vram,
                ChrMemoryType.Default => ChrRom.Length > 0 ? ChrRom : ChrRam,
                _ => null
            };

            if (array == null || array.Length == 0)
                return value;

            int sourceIndex = (sourcePage << 8) + pageIndex;
            sourceIndex %= array.Length;

            return array[sourceIndex];
        }

        protected virtual void MapperReadPpu(ushort address, ref byte dataBus) { }
        public void WritePpu(ushort address, byte value)
        {
            byte page = (byte)(address >> 8);
            byte pageIndex = (byte)(address & 0xFF);

            var type = _chrMemoryTypes[page];

            var access = _chrAccessTypes[page];
            if (access == MemoryAccessType.Default)
                access = type == ChrMemoryType.ChrRom ? MemoryAccessType.Read : MemoryAccessType.ReadWrite;

            if ((access & MemoryAccessType.Write) == 0)
                return;

            if (type == ChrMemoryType.Mapper)
            {
                MapperWritePpu(address, value);
                return;
            }

            if (type == ChrMemoryType.Default)
                type = ChrRom.Length > 0 ? ChrMemoryType.ChrRom : ChrMemoryType.ChrRam;

            if (type == ChrMemoryType.ChrRom)
                return;

            int sourcePage = _chrSourceOffsets[page];

            byte[] array = type switch
            {
                ChrMemoryType.ChrRom => ChrRom,
                ChrMemoryType.ChrRam => ChrRam,
                ChrMemoryType.Vram => Vram,
                _ => null
            };

            if (array == null || array.Length == 0)
                return;

            int sourceIndex = (sourcePage << 8) + pageIndex;
            sourceIndex %= array.Length;

            array[sourceIndex] = value;
        }
        protected virtual void MapperWritePpu(ushort address, byte value) { }

        public virtual void OnPpuAddressUpdate(ushort address) { }

        private void MapNametable(byte index, byte source)
        {
            ushort startAddress = (ushort)(0x2000 + index * 0x400);
            ushort endAddress = (ushort)(startAddress + 0x400 - 1);
            SetChrMemoryMapping(startAddress, endAddress, source * 0x400, ChrMemoryType.Vram);
            startAddress += 0x1000;
            endAddress += 0x1000;
            SetChrMemoryMapping(startAddress, endAddress, source * 0x400, ChrMemoryType.Vram);
        }
        private void MapNametables(byte nametableIndex0, byte nametableIndex1,  byte nametableIndex2, byte nametableIndex3)
        {
            MapNametable(0, nametableIndex0);
            MapNametable(1, nametableIndex1);
            MapNametable(2, nametableIndex2);
            MapNametable(3, nametableIndex3);
        }
        private void MapNametables()
        {
            switch(_mirrorType)
            {
                case NametableMirrorType.Horizontal:
                    MapNametables(0, 0, 1, 1);
                    break;
                case NametableMirrorType.Vertical:
                    MapNametables(0, 1, 0, 1);
                    break;
                case NametableMirrorType.OnlyScreenA:
                    MapNametables(0, 0, 0, 0);
                    break;
                case NametableMirrorType.OnlyScreenB:
                    MapNametables(1, 1, 1, 1);
                    break;
                case NametableMirrorType.FourScreen:
                    MapNametables(0, 1, 2, 3);
                    break;
            }
        }
    }
}
