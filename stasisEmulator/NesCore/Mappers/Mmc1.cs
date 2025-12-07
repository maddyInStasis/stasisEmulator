namespace stasisEmulator.NesCore.Mappers
{
    public class Mmc1 : Mapper
    {
        protected override ushort PrgPageSize => 0x4000;
        protected override ushort ChrPageSize => 0x1000;

        private readonly bool _isRevisionB;

        private byte _shiftRegister = InitialShift;
        private const byte InitialShift = 0b10000;

        private bool _prgRamEnabled;
        private bool PrgRamEnabled
        {
            get => _prgRamEnabled;
            set
            {
                if (_prgRamEnabled == value)
                    return;

                _prgRamEnabled = value;
                MapPrgRam();
            }
        }

        private byte Control
        {
            get
            {
                byte register = MirrorType switch
                {
                    NametableMirrorType.OnlyScreenA => 0,
                    NametableMirrorType.OnlyScreenB => 1,
                    NametableMirrorType.Vertical => 2,
                    NametableMirrorType.Horizontal => 3,
                    _ => 0
                };

                register |= (byte)(PrgRomBankMode << 2);
                register |= (byte)(ChrRom4KiBMode ? 0x10 : 0);

                return register;
            }
            set
            {
                MirrorType = (value & 3) switch
                {
                    0 => NametableMirrorType.OnlyScreenA,
                    1 => NametableMirrorType.OnlyScreenB,
                    2 => NametableMirrorType.Vertical,
                    3 => NametableMirrorType.Horizontal,
                    _ => NametableMirrorType.OnlyScreenA
                };

                PrgRomBankMode = (byte)((value >> 2) & 3);
                ChrRom4KiBMode = (value & 0x10) != 0;
            }
        }

        private byte _prgRomBankMode;
        private byte PrgRomBankMode
        {
            get => _prgRomBankMode;
            set
            {
                _prgRomBankMode = value;
                BankPrgRom();
            }
        }
        private bool _chrRom4KiBMode;
        private bool ChrRom4KiBMode
        {
            get => _chrRom4KiBMode;
            set
            {
                _chrRom4KiBMode = value;
                BankChrRom();
            }
        }

        private byte _prgBank;
        private byte PrgBank
        {
            get => _prgBank;
            set
            {
                _prgBank = value;
                BankPrgRom();
            }
        }
        private byte _chrBank0;
        private byte ChrBank0
        {
            get => _chrBank0;
            set
            {
                _chrBank0 = value;
                BankChrRom();
            }
        }
        private byte _chrBank1;
        private byte ChrBank1
        {
            get => _chrBank1;
            set
            {
                _chrBank1 = value;
                BankChrRom();
            }
        }

        public Mmc1(Rom rom, Nes nes, bool isRevisionB) : base(rom, nes)
        {
            _isRevisionB = isRevisionB;
            AddRegisterRange(0x8000, 0xFFFF, true);
            BankPrgRom();
            BankChrRom();
            PrgRamEnabled = true;
        }

        protected override void WriteRegisterCpu(ushort address, byte value)
        {
            if ((value & 0x80) != 0)
            {
                ResetShiftRegister();
                Control |= 0x0C;
                return;
            }

            bool full = (_shiftRegister & 1) != 0;

            _shiftRegister >>= 1;
            if ((value & 1) != 0)
                _shiftRegister |= InitialShift;

            if (!full)
                return;

            RegisterLoad(address, _shiftRegister);
            ResetShiftRegister();
        }

        private void RegisterLoad(ushort address, byte value)
        {
            switch ((address >> 13) & 3)
            {
                case 0:
                    Control = value;
                    break;
                case 1:
                    ChrBank0 = value;
                    break;
                case 2:
                    ChrBank1 = value;
                    break;
                case 3:
                    PrgBank = (byte)(value & 15);
                    if (_isRevisionB)
                        PrgRamEnabled = (value & 0x10) == 0;

                    break;
            }
        }

        private void BankPrgRom()
        {
            if (PrgRomBankMode < 2)
            {
                byte effectiveBank = (byte)(PrgBank & 14);
                SelectPrgPage(0, effectiveBank);
                SelectPrgPage(1, effectiveBank + 1);
                return;
            }

            if (PrgRomBankMode == 2)
            {
                SelectPrgPage(0, 0);
                SelectPrgPage(1, PrgBank);
            }
            else
            {
                SelectPrgPage(0, PrgBank);
                SelectPrgPage(1, -1);
            }
        }

        private void BankChrRom()
        {
            if (!_chrRom4KiBMode)
            {
                byte effectiveBank = (byte)(ChrBank0 & 30);
                SelectChrPage(0, effectiveBank);
                SelectChrPage(1, effectiveBank + 1);
                return;
            }

            SelectChrPage(0, ChrBank0);
            SelectChrPage(1, ChrBank1);
        }

        private void MapPrgRam()
        {
            SetPrgMemoryMapping(0x6000, 0x7FFF, 0, PrgRamEnabled ? (Rom.HasBattery ? PrgMemoryType.SaveRam : PrgMemoryType.WorkRam) : PrgMemoryType.None);
        }

        private void ResetShiftRegister()
        {
            _shiftRegister = InitialShift;
        }
    }
}
