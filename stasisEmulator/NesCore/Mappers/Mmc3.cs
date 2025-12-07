using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Mappers
{
    public class Mmc3 : Mapper
    {
        protected override ushort PrgPageSize => 0x2000;
        protected override ushort ChrPageSize => 0x400;

        private bool _prgBankMode;
        private bool _chrBanksInverted;

        private readonly byte[] _banks = new byte[8];

        private bool _prgRamEnabled;
        private bool _prgRamWriteProtect;

        private byte _nextBankWrite;

        private bool _a12Set;
        private byte _irqReload;
        private byte _irqCounter;
        private bool _irqReloadRequest;
        private bool _irqEnabled;

        private bool _irqFlag;
        private bool IrqFlag
        {
            get => _irqFlag;
            set
            {
                if (_irqFlag == value)
                    return;
                _irqFlag = value;

                if (value)
                    _nes.Cpu.IrqLine++;
                else
                    _nes.Cpu.IrqLine--;
            }
        }

        public Mmc3(Rom rom, Nes nes) : base(rom, nes)
        {
            AddRegisterRange(0x8000, 0xFFFF, true);
            if (rom.AltNametable)
                MirrorType = NametableMirrorType.FourScreen;

            BankPrgRom();
            BankChr();
        }

        protected override void WriteRegisterCpu(ushort address, byte value)
        {
            if (address < 0xA000)
            {
                if ((address & 1) == 0)
                {
                    _nextBankWrite = (byte)(value & 7);
                    _prgBankMode = (value & 0x40) != 0;
                    _chrBanksInverted = (value & 0x80) != 0;
                    BankPrgRom();
                    BankChr();
                }
                else
                {
                    _banks[_nextBankWrite] = value;
                    if (_nextBankWrite < 6)
                        BankChr();
                    else
                        BankPrgRom();
                }
            }
            else if (address < 0xC000)
            {
                if ((address & 1) == 0)
                {
                    if (MirrorType != NametableMirrorType.FourScreen)
                        MirrorType = (value & 1) == 0 ? NametableMirrorType.Vertical : NametableMirrorType.Horizontal;
                }
                else
                {
                    _prgRamWriteProtect = (value & 0x40) != 0;
                    _prgRamEnabled = (value & 0x80) != 0;
                    MapPrgRam();
                }
            }
            else if (address < 0xE000)
            {
                if ((address & 1) == 0)
                    _irqReload = value;
                else
                    _irqReloadRequest = true;
            }
            else
            {
                _irqEnabled = (address & 1) != 0;
            }
        }

        private void BankPrgRom()
        {
            if (!_prgBankMode)
            {
                SelectPrgPage(0, _banks[6]);
                SelectPrgPage(2, -2);
            }
            else
            {
                SelectPrgPage(0, -2);
                SelectPrgPage(2, _banks[6]);
            }

            SelectPrgPage(1, _banks[7]);
            SelectPrgPage(3, -1);
        }

        private void BankChr()
        {
            byte destOffsetA = (byte)(_chrBanksInverted ? 4 : 0);
            byte destOffsetB = (byte)(_chrBanksInverted ? 0 : 4);

            SelectChrPage(destOffsetA, _banks[0] & 0xFE);
            SelectChrPage(++destOffsetA, (_banks[0] & 0xFE) + 1);
            SelectChrPage(++destOffsetA, _banks[1] & 0xFE);
            SelectChrPage(++destOffsetA, (_banks[1] & 0xFE) + 1);

            SelectChrPage(destOffsetB, _banks[2]);
            SelectChrPage(++destOffsetB, _banks[3]);
            SelectChrPage(++destOffsetB, _banks[4]);
            SelectChrPage(++destOffsetB, _banks[5]);
        }

        private void MapPrgRam()
        {
            SetPrgMemoryMapping(0x6000, 0x7FFF, 0, Rom.HasBattery ? PrgMemoryType.SaveRam : PrgMemoryType.WorkRam, _prgRamEnabled ? (_prgRamWriteProtect ? MemoryAccessType.Read : MemoryAccessType.ReadWrite) : MemoryAccessType.None);
        }

        const ushort A12 = 1 << 12;
        public override void OnPpuAddressUpdate(ushort address)
        {
            bool newA12 = (address & A12) != 0;
            if (!_a12Set && newA12)
            {
                if (_irqCounter == 0 || _irqReloadRequest)
                {
                    _irqCounter = _irqReload;
                    _irqReloadRequest = false;
                }
                else
                {
                    _irqCounter--;
                }

                IrqFlag = _irqCounter == 0 && _irqEnabled;
            }
            _a12Set = newA12;
        }
    }
}
