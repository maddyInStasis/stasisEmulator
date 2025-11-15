using System;

namespace stasisEmulator.NesConsole
{
    public class Cpu(Nes nes)
    {
        private enum OperationPhase
        {
            Fetch,
            Address,
            Execute
        }

        public enum Instr
        {
            NotImpl, BRK, HLT,
            LDA, STA, LDX, STX, LDY, STY,
            TAX, TXA, TAY, TYA,
            ADC, SBC, INC, DEC, INX, DEX, INY, DEY,
            ASL, LSR, ROL, ROR,
            AND, ORA, EOR, BIT,
            CMP, CPX, CPY,
            BCC, BCS, BEQ, BNE, BPL, BMI, BVC, BVS,
            JMP_abs, JMP_ind, JSR, RTS, RTI,
            PHA, PLA, PHP, PLP, TXS, TSX,
            CLC, SEC, CLI, SEI, CLD, SED, CLV,
            NOP,

            //unofficial
            SLO,
            RLA,
            SRE,
            RRA,
            SAX,
            LAX,
            DCP,
            ISC,

            ANC,
            ASR,
            ARR,

            AXS
        }

        public enum Addr
        {
            NotImpl,
            Imp,
            Acc,
            Imm,
            Zero,
            ZeroX,
            ZeroY,
            Abs,
            AbsX,
            AbsXW,
            AbsY,
            AbsYW,
            IndX,
            IndY,
            IndYW,
            Rel,
            Other
        }

        public static readonly Instr[] Instructions = [
            //00           01             02             03             04             05             06             07             08             09             0A             0B             0C             0D             0E             0F
            Instr.BRK    , Instr.ORA    , Instr.HLT    , Instr.SLO    , Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.SLO    , Instr.PHP    , Instr.ORA    , Instr.ASL    , Instr.ANC    , Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.SLO    , //00
            Instr.BPL    , Instr.ORA    , Instr.HLT    , Instr.SLO    , Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.SLO    , Instr.CLC    , Instr.ORA    , Instr.NOP    , Instr.SLO    , Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.SLO    , //10
            Instr.JSR    , Instr.AND    , Instr.HLT    , Instr.RLA    , Instr.BIT    , Instr.AND    , Instr.ROL    , Instr.RLA    , Instr.PLP    , Instr.AND    , Instr.ROL    , Instr.ANC    , Instr.BIT    , Instr.AND    , Instr.ROL    , Instr.RLA    , //20
            Instr.BMI    , Instr.AND    , Instr.HLT    , Instr.RLA    , Instr.NOP    , Instr.AND    , Instr.ROL    , Instr.RLA    , Instr.SEC    , Instr.AND    , Instr.NOP    , Instr.RLA    , Instr.NOP    , Instr.AND    , Instr.ROL    , Instr.RLA    , //30
            Instr.RTI    , Instr.EOR    , Instr.HLT    , Instr.SRE    , Instr.NOP    , Instr.EOR    , Instr.LSR    , Instr.SRE    , Instr.PHA    , Instr.EOR    , Instr.LSR    , Instr.ASR    , Instr.JMP_abs, Instr.EOR    , Instr.LSR    , Instr.SRE    , //40
            Instr.BVC    , Instr.EOR    , Instr.HLT    , Instr.SRE    , Instr.NOP    , Instr.EOR    , Instr.LSR    , Instr.SRE    , Instr.CLI    , Instr.EOR    , Instr.NOP    , Instr.SRE    , Instr.NOP    , Instr.EOR    , Instr.LSR    , Instr.SRE    , //50
            Instr.RTS    , Instr.ADC    , Instr.HLT    , Instr.RRA    , Instr.NOP    , Instr.ADC    , Instr.ROR    , Instr.RRA    , Instr.PLA    , Instr.ADC    , Instr.ROR    , Instr.ARR    , Instr.JMP_ind, Instr.ADC    , Instr.ROR    , Instr.RRA    , //60
            Instr.BVS    , Instr.ADC    , Instr.HLT    , Instr.RRA    , Instr.NOP    , Instr.ADC    , Instr.ROR    , Instr.RRA    , Instr.SEI    , Instr.ADC    , Instr.NOP    , Instr.RRA    , Instr.NOP    , Instr.ADC    , Instr.ROR    , Instr.RRA    , //70
            Instr.NOP    , Instr.STA    , Instr.NOP    , Instr.SAX    , Instr.STY    , Instr.STA    , Instr.STX    , Instr.SAX    , Instr.DEY    , Instr.NOP    , Instr.TXA    , Instr.NotImpl, Instr.STY    , Instr.STA    , Instr.STX    , Instr.SAX    , //80
            Instr.BCC    , Instr.STA    , Instr.HLT    , Instr.NotImpl, Instr.STY    , Instr.STA    , Instr.STX    , Instr.SAX    , Instr.TYA    , Instr.STA    , Instr.TXS    , Instr.NotImpl, Instr.NotImpl, Instr.STA    , Instr.NotImpl, Instr.NotImpl, //90
            Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.LAX    , Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.LAX    , Instr.TAY    , Instr.LDA    , Instr.TAX    , Instr.NotImpl, Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.LAX    , //A0
            Instr.BCS    , Instr.LDA    , Instr.HLT    , Instr.LAX    , Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.LAX    , Instr.CLV    , Instr.LDA    , Instr.TSX    , Instr.NotImpl, Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.LAX    , //B0
            Instr.CPY    , Instr.CMP    , Instr.NOP    , Instr.DCP    , Instr.CPY    , Instr.CMP    , Instr.DEC    , Instr.DCP    , Instr.INY    , Instr.CMP    , Instr.DEX    , Instr.AXS    , Instr.CPY    , Instr.CMP    , Instr.DEC    , Instr.DCP    , //C0
            Instr.BNE    , Instr.CMP    , Instr.HLT    , Instr.DCP    , Instr.NOP    , Instr.CMP    , Instr.DEC    , Instr.DCP    , Instr.CLD    , Instr.CMP    , Instr.NOP    , Instr.DCP    , Instr.NOP    , Instr.CMP    , Instr.DEC    , Instr.DCP    , //D0
            Instr.CPX    , Instr.SBC    , Instr.NOP    , Instr.ISC    , Instr.CPX    , Instr.SBC    , Instr.INC    , Instr.ISC    , Instr.INX    , Instr.SBC    , Instr.NOP    , Instr.SBC    , Instr.CPX    , Instr.SBC    , Instr.INC    , Instr.ISC    , //E0
            Instr.BEQ    , Instr.SBC    , Instr.HLT    , Instr.ISC    , Instr.NOP    , Instr.SBC    , Instr.INC    , Instr.ISC    , Instr.SED    , Instr.SBC    , Instr.NOP    , Instr.ISC    , Instr.NOP    , Instr.SBC    , Instr.INC    , Instr.ISC      //F0
        ];

        public static readonly Addr[] AddressingModes = [
            //00          01            02            03            04            05            06            07            08            09            0A            0B            0C            0D            0E            0F
            Addr.Imp    , Addr.IndX   , Addr.Imp    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Acc    , Addr.Imm    , Addr.Abs    , Addr.Abs    , Addr.Abs    , Addr.Abs    , //00
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsYW  , Addr.AbsX   , Addr.AbsX   , Addr.AbsXW  , Addr.AbsXW  , //10
            Addr.Other  , Addr.IndX   , Addr.Imp    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Acc    , Addr.Imm    , Addr.Abs    , Addr.Abs    , Addr.Abs    , Addr.Abs    , //20
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsYW  , Addr.AbsX   , Addr.AbsX   , Addr.AbsXW  , Addr.AbsXW  , //30
            Addr.Other  , Addr.IndX   , Addr.Imp    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Acc    , Addr.Imm    , Addr.Other  , Addr.Abs    , Addr.Abs    , Addr.Abs    , //40
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsYW  , Addr.AbsX   , Addr.AbsX   , Addr.AbsXW  , Addr.AbsXW  , //50
            Addr.Imp    , Addr.IndX   , Addr.Imp    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Acc    , Addr.Imm    , Addr.Other  , Addr.Abs    , Addr.Abs    , Addr.Abs    , //60
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsYW  , Addr.AbsX   , Addr.AbsX   , Addr.AbsXW  , Addr.AbsXW  , //70
            Addr.Imm    , Addr.IndX   , Addr.Imm    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Imp    , Addr.Imm    , Addr.Abs    , Addr.Abs    , Addr.Abs    , Addr.Abs    , //80
            Addr.Rel    , Addr.IndYW  , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroY  , Addr.ZeroY  , Addr.Imp    , Addr.AbsYW  , Addr.Imp    , Addr.AbsYW  , Addr.AbsXW  , Addr.AbsXW  , Addr.AbsYW  , Addr.AbsYW  , //90
            Addr.Imm    , Addr.IndX   , Addr.Imm    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Imp    , Addr.Imm    , Addr.Abs    , Addr.Abs    , Addr.Abs    , Addr.Abs    , //A0
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndY   , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroY  , Addr.ZeroY  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsY   , Addr.AbsX   , Addr.AbsX   , Addr.AbsY   , Addr.AbsY   , //B0
            Addr.Imm    , Addr.IndX   , Addr.Imm    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Imp    , Addr.Imm    , Addr.Abs    , Addr.Abs    , Addr.Abs    , Addr.Abs    , //C0
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsYW  , Addr.AbsX   , Addr.AbsX   , Addr.AbsXW  , Addr.AbsXW  , //D0
            Addr.Imm    , Addr.IndX   , Addr.Imm    , Addr.IndX   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Zero   , Addr.Imp    , Addr.Imm    , Addr.Imp    , Addr.Imm    , Addr.Abs    , Addr.Abs    , Addr.Abs    , Addr.Abs    , //E0
            Addr.Rel    , Addr.IndY   , Addr.Imp    , Addr.IndYW  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.ZeroX  , Addr.Imp    , Addr.AbsY   , Addr.Imp    , Addr.AbsYW  , Addr.AbsX   , Addr.AbsX   , Addr.AbsXW  , Addr.AbsXW    //F0
        ];

        public byte A;
        public byte X;
        public byte Y;

        public ushort PC;
        public byte S;

        public bool Flag_Carry;
        public bool Flag_Zero;
        public bool Flag_InterruptDisable;
        public bool Flag_Decimal;
        public bool Flag_Overflow;
        public bool Flag_Negative;

        public const byte Break = 0b10000;

        public byte P { 
            get
            {
                byte p = 0b00100000;
                p |= (byte)(Flag_Carry ? 1 : 0);
                p |= (byte)(Flag_Zero ? 2 : 0);
                p |= (byte)(Flag_InterruptDisable ? 4 : 0);
                p |= (byte)(Flag_Decimal ? 8 : 0);
                p |= (byte)(Flag_Overflow ? 64 : 0);
                p |= (byte)(Flag_Negative ? 128 : 0);
                return p;
            } 
            set
            {
                Flag_Carry = (value & 1) != 0;
                Flag_Zero = (value & 2) != 0;
                Flag_InterruptDisable = (value & 4) != 0;
                Flag_Decimal = (value & 8) != 0;
                Flag_Overflow = (value & 64) != 0;
                Flag_Negative = (value & 128) != 0;
            }
        }

        public bool NmiLine;
        private bool NmiPinsSignal;

        private bool DoNmi;

        public int IrqLine;
        private bool DoIrq;

        private enum Interrupt
        {
            None,
            Reset,
            Nmi,
            Irq
        }
        private Interrupt _interruptToRun = Interrupt.None;

        public readonly byte[] Ram = new byte[0x800];

        public ulong CycleCount;

        public enum CycleType { Get, Put }
        public CycleType CurrentCycleType { get => (CycleCount & 1) == 0 ? CycleType.Get : CycleType.Put; }

        public ulong InstructionCount;
        public readonly TraceLogger TraceLogger = new(30000);

        private readonly Nes _nes = nes;

        private ushort _logPc = 0;

        private byte _logOperandA = 0;
        private byte _logOperandB = 0;
        private byte _logByteCodeLength = 1;

        private byte _logA = 0;
        private byte _logX = 0;
        private byte _logY = 0;
        private byte _logS = 0;
        private byte _logP = 0;

        private ushort _logArgument = 0;
        private ushort _logEffectiveAddress = 0;

        private ulong _logCycleCount;

        private bool _ignoreCurrentLog = false;

        private ushort _addressBus = 0;
        private byte _dataBus = 0;

        private OperationPhase _operationPhase;
        private int _operationCycle;
        private bool _operationPhaseComplete;

        private byte _opcode;

        private Instr _currentInstr;
        private Addr _currentAddr;

        private bool _halt = false;

        public bool PauseOnBrk { get; set; } = false;
        private bool _break = false;

        private bool _isReadCycle = false;

        private bool _dmaTryHalt = false;
        private bool _dmaTryAlign = false;
        private bool _inDma = false;
        private ushort _dmaPage;
        private byte _dmaIndex = 0;

        private bool _doReset = false;

        public void Power()
        {
            A = 0;
            X = 0;
            Y = 0;

            S = 0;

            Flag_Carry = false;
            Flag_Zero = false;
            Flag_Decimal = false;
            Flag_Overflow = false;
            Flag_Negative = false;

            Reset();
        }

        public void Reset()
        {
            _halt = false;

            Flag_InterruptDisable = true;

            _operationPhase = OperationPhase.Fetch;
            _operationCycle = 0;
            _operationPhaseComplete = false;

            CycleCount = 1;
            InstructionCount = 0;

            _doReset = true;
            DoNmi = false;
            DoIrq = false;
        }

        private byte Read(ushort address)
        {
            if (address < 0x2000)
                _dataBus = Ram[address & (0x800 - 1)];
            else if (address >= 0x4020)
                _nes.Cartridge?.ReadCpu(address, ref _dataBus);
            else if (address < 0x4000)
                _nes.Ppu.ReadRegister(address, ref _dataBus);
            else if (address == 0x4016)
                _nes.Player1Controller?.RegisterRead(ref _dataBus);
            else if (address == 0x4017)
                _nes.Player2Controller?.RegisterRead(ref _dataBus);
            else if (address < 0x4018)
                _nes.Apu.RegisterRead(address, ref _dataBus);

            _isReadCycle = true;

            return _dataBus;
        }
        public byte DebugRead(ushort address)
        {
            byte value = (byte)(address >> 8);

            if (address < 0x2000)
                value = Ram[address & (0x800 - 1)];
            else if (address >= 0x4020 && _nes.Cartridge != null)
                value = _nes.Cartridge.DebugReadCpu(address);

            return value;
        }

        private void Write(ushort address, byte value)
        {
            _dataBus = value;

            if (address < 0x2000)
                Ram[address & (0x800 - 1)] = value;
            else if (address >= 0x4020)
                _nes.Cartridge?.WriteCpu(address, value);
            else if (address < 0x4000)
                _nes.Ppu.WriteRegister(address, value);
            else if (address == 0x4014)
            {
                _dmaTryHalt = true;
                _dmaPage = (ushort)(value << 8);
                _dmaIndex = 0;
            }
            else if (address == 0x4016)
            {
                _nes.Player1Controller?.RegisterWrite(value);
                _nes.Player2Controller?.RegisterWrite(value);
            }
            else if (address < 0x4018)
                _nes.Apu.RegisterWrite(address, _dataBus);

            _isReadCycle = false;
        }

        private void Push(byte value)
        {
            Write((ushort)(0x100 + S), value);
            S--;
        }

        public void RunCycle()
        {
            if (_halt)
                return;

            if (_inDma)
            {
                DoDma();
                CycleCount++;
                return;
            }

            switch (_operationPhase)
            {
                case OperationPhase.Fetch:
                    Fetch();
                    break;
                case OperationPhase.Address:
                    DoAddressing();
                    break;
                case OperationPhase.Execute:
                    DoInstructionCycle();
                    break;
            }

            CycleCount++;

            if (_dmaTryHalt && _isReadCycle)
            {
                _inDma = true;
                _dmaTryAlign = true;
                _dmaTryHalt = false;
            }
        }

        private void DoDma()
        {
            bool isGetCycle = CurrentCycleType == CycleType.Get;

            if (_dmaTryAlign && !isGetCycle)
                return;

            _dmaTryAlign = false;

            if (isGetCycle)
            {
                Read((ushort)(_dmaPage + _dmaIndex));
            }
            else
            {
                Write(Ppu.OAMDATA, _dataBus);

                if (_dmaIndex == 0xFF)
                    _inDma = false;

                _dmaIndex++;
            }
        }

        private void Fetch()
        {
            _logPc = PC;

            _logA = A;
            _logX = X;
            _logY = Y;

            _logS = S;
            _logP = P;

            _logByteCodeLength = 1;
            _logOperandA = 0;
            _logOperandB = 0;

            _logArgument = 0;
            _logEffectiveAddress = 0;

            _logCycleCount = CycleCount;

            _ignoreCurrentLog = false;

            _opcode = Read(PC);

            _interruptToRun = Interrupt.None;

            if (_doReset)
                _interruptToRun = Interrupt.Reset;
            else if (DoNmi)
                _interruptToRun = Interrupt.Nmi;
            else if (DoIrq)
                _interruptToRun = Interrupt.Irq;

            _doReset = false;
            DoNmi = false;
            DoIrq = false;

            //interrupts execute opcode 0, the behavior of which depends on the interrupt.
            if (_interruptToRun != Interrupt.None)
            {
                _ignoreCurrentLog = true;
                _opcode = 0;
            }
            else
            {
                PC++;
            }

            _currentInstr = Instructions[_opcode];
            _currentAddr = AddressingModes[_opcode];

            _addressBus = PC;

            //ew. idk how else to do it though :(
            if (_currentAddr == Addr.Imm)
                PC++;

            //this is getting ugly...
            //a lot of addressing modes need to go straight to the execution step though
            //TODO: fix
            if (_currentAddr == Addr.Imp || _currentAddr == Addr.Acc || _currentAddr == Addr.Imm || _currentAddr == Addr.Rel || _currentAddr == Addr.Other)
                _operationPhase = OperationPhase.Execute;
            else
                _operationPhase = OperationPhase.Address;
        }

        private ushort _correctIndexedAddress = 0;
        private ushort _pointer = 0;

        private void DoAddressing()
        {
            switch (_currentAddr)
            {
                case Addr.Zero:
                    _addressBus = Read(PC);
                    _logOperandA = _dataBus;
                    _logByteCodeLength = 2;
                    _logArgument = _addressBus;
                    PC++;
                    _operationPhaseComplete = true;
                    break;
                case Addr.Abs:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            PC++;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.ZeroX:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            _logByteCodeLength = 2;
                            _logArgument = _addressBus;
                            PC++;
                            break;
                        case 1:
                            Read(_addressBus);
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + X) & 0x00FF));
                            _logEffectiveAddress = _addressBus;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.ZeroY:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            _logByteCodeLength = 2;
                            _logArgument = _addressBus;
                            PC++;
                            break;
                        case 1:
                            Read(_addressBus);
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));
                            _logEffectiveAddress = _addressBus;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.AbsX:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            _correctIndexedAddress = (ushort)(_addressBus + X);
                            _logEffectiveAddress = _correctIndexedAddress;
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + X) & 0x00FF));
                            PC++;

                            if (_addressBus == _correctIndexedAddress)
                                _operationPhaseComplete = true;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.AbsXW:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            _correctIndexedAddress = (ushort)(_addressBus + X);
                            _logEffectiveAddress = _correctIndexedAddress;
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + X) & 0x00FF));
                            PC++;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.AbsY:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            _correctIndexedAddress = (ushort)(_addressBus + Y);
                            _logEffectiveAddress = _correctIndexedAddress;
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));
                            PC++;

                            if (_addressBus == _correctIndexedAddress)
                                _operationPhaseComplete = true;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.AbsYW:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            _correctIndexedAddress = (ushort)(_addressBus + Y);
                            _logEffectiveAddress = _correctIndexedAddress;
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));
                            PC++;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.IndX:
                    switch (_operationCycle)
                    {
                        case 0:
                            _pointer = Read(PC);
                            _logOperandA = _dataBus;
                            _logByteCodeLength = 2;
                            _logArgument = _pointer;
                            PC++;
                            break;
                        case 1:
                            Read(_pointer);
                            _pointer += X;
                            _pointer &= 0xFF;
                            break;
                        case 2:
                            _addressBus = Read(_pointer);
                            break;
                        case 3:
                            _addressBus |= (ushort)(Read((byte)(_pointer + 1)) << 8);
                            _logEffectiveAddress = _addressBus;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.IndY:
                    switch (_operationCycle)
                    {
                        case 0:
                            _pointer = Read(PC);
                            _logOperandA = _dataBus;
                            _logByteCodeLength = 2;
                            _logArgument = _pointer;
                            PC++;
                            break;
                        case 1:
                            _addressBus = Read(_pointer);
                            break;
                        case 2:
                            _addressBus |= (ushort)(Read((byte)(_pointer + 1)) << 8);
                            _correctIndexedAddress = (ushort)(_addressBus + Y);
                            _logEffectiveAddress = _correctIndexedAddress;
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));

                            if (_addressBus == _correctIndexedAddress)
                                _operationPhaseComplete = true;

                            break;
                        case 3:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Addr.IndYW:
                    switch (_operationCycle)
                    {
                        case 0:
                            _pointer = Read(PC);
                            _logOperandA = _dataBus;
                            _logByteCodeLength = 2;
                            _logArgument = _pointer;
                            PC++;
                            break;
                        case 1:
                            _addressBus = Read(_pointer);
                            break;
                        case 2:
                            _addressBus |= (ushort)(Read((byte)(_pointer + 1)) << 8);
                            _correctIndexedAddress = (ushort)(_addressBus + Y);
                            _logEffectiveAddress = _correctIndexedAddress;
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));

                            break;
                        case 3:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                default:
                    break;
            }

            _operationCycle++;
            if (_operationPhaseComplete)
            {
                _operationCycle = 0;
                _operationPhase = OperationPhase.Execute;
                _operationPhaseComplete = false;
            }
        }

        private void DoInstructionCycle()
        {
            switch (_currentInstr)
            {
                case Instr.BRK:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            if (_interruptToRun == Interrupt.None)
                            {
                                if (PauseOnBrk)
                                    _break = true;
                                PC++;
                            }
                            break;
                        case 1:
                            if (_interruptToRun == Interrupt.Reset)
                            {
                                Read((ushort)(0x100 + S));
                                S--;
                            }
                            else
                            {
                                Push((byte)(PC >> 8));
                            }
                            break;
                        case 2:
                            if (_interruptToRun == Interrupt.Reset)
                            {
                                Read((ushort)(0x100 + S));
                                S--;
                            }
                            else
                            {
                                Push((byte)PC);
                            }
                            break;
                        case 3:
                            if (_interruptToRun == Interrupt.Reset)
                            {
                                Read((ushort)(0x100 + S));
                                S--;
                            }
                            else
                            {
                                if (_interruptToRun != Interrupt.None)
                                    Push(P);
                                else
                                    Push((byte)(P | Break));
                            }
                            PollInterrupts(true);
                            if (DoNmi)
                                _interruptToRun = Interrupt.Nmi;
                            else if (DoIrq)
                                _interruptToRun = Interrupt.Irq;
                            break;
                        case 4:
                            Flag_InterruptDisable = true;
                            ushort fetchAddr = _interruptToRun switch
                            {
                                Interrupt.Nmi => 0xFFFA,
                                Interrupt.Reset => 0xFFFC,
                                _ => 0xFFFE
                            };
                            PC = Read(fetchAddr);
                            
                            break;
                        case 5:
                            fetchAddr = _interruptToRun switch
                            {
                                Interrupt.Nmi => 0xFFFB,
                                Interrupt.Reset => 0xFFFD,
                                _ => 0xFFFF
                            };

                            PC |= (ushort)(Read(fetchAddr) << 8);

                            DoNmi = false;
                            DoIrq = false;
                            _doReset = false;

                            _operationPhaseComplete = true;
                            break;
                    }

                    break;
                case Instr.LDA:
                    PollInterrupts(true);
                    A = Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.STA:
                    PollInterrupts(true);
                    Write(_addressBus, A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.LDX:
                    PollInterrupts(true);
                    X = Read(_addressBus);
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.STX:
                    PollInterrupts(true);
                    Write(_addressBus, X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.LDY:
                    PollInterrupts(true);
                    Y = Read(_addressBus);
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.STY:
                    PollInterrupts(true);
                    Write(_addressBus, Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TAX:
                    PollInterrupts(true);
                    X = A;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TXA:
                    PollInterrupts(true);
                    A = X;
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TAY:
                    PollInterrupts(true);
                    Y = A;
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TYA:
                    PollInterrupts(true);
                    A = Y;
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.ASL:
                    switch (_operationCycle)
                    {
                        case 0:
                            if (_currentAddr != Addr.Acc)
                            {
                                Read(_addressBus);
                                break;
                            }
                            Flag_Carry = A > 127;
                            A <<= 1;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            Flag_Carry = _dataBus > 127;
                            _dataBus <<= 1;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.LSR:
                    switch (_operationCycle)
                    {
                        case 0:
                            if (_currentAddr != Addr.Acc)
                            {
                                Read(_addressBus);
                                break;
                            }
                            Flag_Carry = (A & 1) != 0;
                            A >>= 1;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            Flag_Carry = (_dataBus & 1) != 0;
                            _dataBus >>= 1;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.ROL:
                    switch (_operationCycle)
                    {
                        case 0:
                            if (_currentAddr != Addr.Acc)
                            {
                                Read(_addressBus);
                                break;
                            }
                            bool newCarry = A > 127;
                            A <<= 1;
                            if (Flag_Carry)
                                A |= 1;
                            Flag_Carry = newCarry;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            newCarry = _dataBus > 127;
                            _dataBus <<= 1;
                            if (Flag_Carry)
                                _dataBus |= 1;
                            Flag_Carry = newCarry;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.ROR:
                    switch (_operationCycle)
                    {
                        case 0:
                            if (_currentAddr != Addr.Acc)
                            {
                                Read(_addressBus);
                                break;
                            }
                            bool newCarry = (A & 1) != 0;
                            A >>= 1;
                            if (Flag_Carry)
                                A |= 128;
                            Flag_Carry = newCarry;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            newCarry = (_dataBus & 1) != 0;
                            _dataBus >>= 1;
                            if (Flag_Carry)
                                _dataBus |= 128;
                            Flag_Carry = newCarry;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.ORA:
                    PollInterrupts(true);
                    A |= Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.AND:
                    PollInterrupts(true);
                    A &= Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.EOR:
                    PollInterrupts(true);
                    A ^= Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.BIT:
                    PollInterrupts(true);
                    Read(_addressBus);
                    Flag_Zero = (A & _dataBus) == 0;
                    Flag_Negative = (_dataBus & 0x80) != 0;
                    Flag_Overflow = (_dataBus & 0x40) != 0;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CMP:
                    PollInterrupts(true);
                    Read(_addressBus);
                    Flag_Carry = A >= _dataBus;
                    Flag_Zero = A == _dataBus;
                    Flag_Negative = (byte)(A - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CPX:
                    PollInterrupts(true);
                    Read(_addressBus);
                    Flag_Carry = X >= _dataBus;
                    Flag_Zero = X == _dataBus;
                    Flag_Negative = (byte)(X - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CPY:
                    PollInterrupts(true);
                    Read(_addressBus);
                    Flag_Carry = Y >= _dataBus;
                    Flag_Zero = Y == _dataBus;
                    Flag_Negative = (byte)(Y - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.ADC:
                    PollInterrupts(true);
                    Read(_addressBus);
                    int sum = A + _dataBus + (Flag_Carry ? 1 : 0);
                    Flag_Carry = sum > 0xFF;
                    Flag_Overflow = ((sum ^ A) & (sum ^ _dataBus) & 0x80) != 0;
                    SetZNFlags((byte)sum);
                    A = (byte)sum;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SBC:
                    PollInterrupts(true);
                    Read(_addressBus);
                    int difference = A - _dataBus - (Flag_Carry ? 0 : 1);
                    Flag_Carry = !(difference < 0);
                    Flag_Overflow = ((difference ^ A) & (difference ^ (~_dataBus)) & 0x80) != 0;
                    SetZNFlags((byte)difference);
                    A = (byte)difference;
                    _operationPhaseComplete = true;
                    break;
                case Instr.INC:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            _dataBus++;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.DEC:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            _dataBus--;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.INX:
                    PollInterrupts(true);
                    X++;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.DEX:
                    PollInterrupts(true);
                    X--;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.INY:
                    PollInterrupts(true);
                    Y++;
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.DEY:
                    PollInterrupts(true);
                    Y--;
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.BCC:
                    Branch(!Flag_Carry);
                    break;
                case Instr.BCS:
                    Branch(Flag_Carry);
                    break;
                case Instr.BEQ:
                    Branch(Flag_Zero);
                    break;
                case Instr.BNE:
                    Branch(!Flag_Zero);
                    break;
                case Instr.BPL:
                    Branch(!Flag_Negative);
                    break;
                case Instr.BMI:
                    Branch(Flag_Negative);
                    break;
                case Instr.BVC:
                    Branch(!Flag_Overflow);
                    break;
                case Instr.BVS:
                    Branch(Flag_Overflow);
                    break;
                case Instr.JMP_abs:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            PollInterrupts(true);
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            PC = _addressBus;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.JMP_ind:
                    switch(_operationCycle)
                    {
                        case 0:
                            _pointer = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            _pointer |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _pointer;
                            break;
                        case 2:
                            _addressBus = Read(_pointer);
                            break;
                        case 3:
                            PollInterrupts(true);
                            PC = (ushort)(Read((ushort)((_pointer & 0xFF00) | ((_pointer + 1) & 0x00FF))) << 8);
                            PC |= _addressBus;
                            _logEffectiveAddress = PC;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.JSR:
                    switch (_operationCycle)
                    {
                        case 0:
                            _addressBus = Read(PC);
                            _logOperandA = _dataBus;
                            PC++;
                            break;
                        case 1:
                            break;
                        case 2:
                            Push((byte)(PC >> 8));
                            break;
                        case 3:
                            Push((byte)PC);
                            break;
                        case 4:
                            PollInterrupts(true);
                            _addressBus |= (ushort)(Read(PC) << 8);
                            _logOperandB = _dataBus;
                            _logByteCodeLength = 3;
                            _logArgument = _addressBus;
                            PC = _addressBus;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.RTS:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            S++;
                            break;
                        case 2:
                            PC = Read((ushort)(0x100 + S));
                            S++;
                            break;
                        case 3:
                            PC |= (ushort)(Read((ushort)(0x100 + S)) << 8);
                            break;
                        case 4:
                            PollInterrupts(true);
                            PC++;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.RTI:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            S++;
                            break;
                        case 2:
                            P = Read((ushort)(0x100 + S));
                            S++;
                            break;
                        case 3:
                            PC = Read((ushort)(0x100 + S));
                            S++;
                            break;
                        case 4:
                            PollInterrupts(true);
                            PC |= (ushort)(Read((ushort)(0x100 + S)) << 8);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.PHA:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            PollInterrupts(true);
                            Push(A);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.PLA:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            S++;
                            break;
                        case 2:
                            PollInterrupts(true);
                            A = Read((ushort)(0x100 + S));
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.PHP:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            PollInterrupts(true);
                            Push((byte)(P | Break));
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.PLP:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            S++;
                            break;
                        case 2:
                            PollInterrupts(true);
                            P = Read((ushort)(0x100 + S));
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.TXS:
                    PollInterrupts(true);
                    S = X;
                    _operationPhaseComplete = true;
                    break;
                case Instr.TSX:
                    PollInterrupts(true);
                    X = S;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLC:
                    PollInterrupts(true);
                    Flag_Carry = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SEC:
                    PollInterrupts(true);
                    Flag_Carry = true;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLI:
                    PollInterrupts(true);
                    Flag_InterruptDisable = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SEI:
                    PollInterrupts(true);
                    Flag_InterruptDisable = true;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLD:
                    PollInterrupts(true);
                    Flag_Decimal = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SED:
                    PollInterrupts(true);
                    Flag_Decimal = true;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLV:
                    PollInterrupts(true);
                    Flag_Overflow = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.NOP:
                    PollInterrupts(true);
                    if (_currentAddr != Addr.Imp)
                        Read(_addressBus);
                    _operationPhaseComplete = true;
                    break;
                //unofficial
                case Instr.SLO:
                    switch (_operationCycle)
                    {
                        case 1:
                            Read(_addressBus);
                            break;
                        case 2:
                            Write(_addressBus, _dataBus);
                            Flag_Carry = _dataBus > 127;
                            _dataBus <<= 1;
                            break;
                        case 3:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            A |= _dataBus;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.RLA:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            bool newCarry = _dataBus > 127;
                            _dataBus <<= 1;
                            if (Flag_Carry)
                                _dataBus |= 1;
                            Flag_Carry = newCarry;
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            A &= _dataBus;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.SRE:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            Flag_Carry = (_dataBus & 1) != 0;
                            _dataBus >>= 1;
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            A ^= _dataBus;
                            SetZNFlags(A);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.RRA:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            bool newCarry = (_dataBus & 1) != 0;
                            _dataBus >>= 1;
                            if (Flag_Carry)
                                _dataBus |= 128;
                            Flag_Carry = newCarry;
                            SetZNFlags(_dataBus);
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            sum = A + _dataBus + (Flag_Carry ? 1 : 0);
                            Flag_Carry = sum > 0xFF;
                            Flag_Overflow = ((sum ^ A) & (sum ^ _dataBus) & 0x80) != 0;
                            SetZNFlags((byte)sum);
                            A = (byte)sum;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.SAX:
                    PollInterrupts(true);
                    Write(_addressBus, (byte)(A & X));
                    _operationPhaseComplete = true;
                    break;
                case Instr.LAX:
                    PollInterrupts(true);
                    A = Read(_addressBus);
                    X = A;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.DCP:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            _dataBus--;
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            Flag_Carry = A >= _dataBus;
                            Flag_Zero = A == _dataBus;
                            Flag_Negative = (byte)(A - _dataBus) > 127;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.ISC:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(_addressBus);
                            break;
                        case 1:
                            Write(_addressBus, _dataBus);
                            _dataBus++;
                            break;
                        case 2:
                            PollInterrupts(true);
                            Write(_addressBus, _dataBus);
                            difference = A - _dataBus - (Flag_Carry ? 0 : 1);
                            Flag_Carry = !(difference < 0);
                            Flag_Overflow = ((difference ^ A) & (difference ^ (~_dataBus)) & 0x80) != 0;
                            SetZNFlags((byte)difference);
                            A = (byte)difference;
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.ANC:
                    PollInterrupts(true);
                    A &= Read(_addressBus);
                    SetZNFlags(A);
                    Flag_Carry = Flag_Negative;
                    _operationPhaseComplete = true;
                    break;
                case Instr.ASR:
                    PollInterrupts(true);
                    A &= Read(_addressBus);
                    Flag_Carry = (A & 1) != 0;
                    A >>= 1;
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.ARR:
                    PollInterrupts(true);
                    A &= Read(_addressBus);
                    A >>= 1;
                    if (Flag_Carry)
                        A |= 128;
                    SetZNFlags(A);
                    Flag_Carry = (A & 0x40) != 0;
                    Flag_Overflow = ((A & 0x40) != 0) ^ ((A & 0x20) != 0);
                    _operationPhaseComplete = true;
                    break;
                case Instr.AXS:
                    PollInterrupts(true);
                    Read(_addressBus);
                    byte and = (byte)(A & X);
                    difference = (byte)(and - _dataBus);
                    X = (byte)difference;
                    Flag_Carry = and >= _dataBus;
                    Flag_Zero = and == _dataBus;
                    Flag_Negative = (byte)(and - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.HLT:
                    _halt = true;
                    _operationPhaseComplete = true;
                    break;
                default:
                    _operationPhaseComplete = true;
                    break;
            }

            if (_currentAddr == Addr.Imm && _operationCycle == 0)
            {
                _logOperandA = _dataBus;
                _logArgument = _dataBus;
                _logByteCodeLength = 2;
            }

            _operationCycle++;
            if (_operationPhaseComplete)
            {
                _operationCycle = 0;
                _operationPhase = OperationPhase.Fetch;
                _operationPhaseComplete = false;

                if (!_ignoreCurrentLog)
                {
                    TraceLogger.Add(new(
                        _logPc,
                        _logByteCodeLength switch
                        {
                            1 => new(_opcode),
                            2 => new(_opcode, _logOperandA),
                            3 => new(_opcode, _logOperandA, _logOperandB),
                            _ => throw new Exception($"{nameof(_logByteCodeLength)} was not a value from 1 to 3.")
                        },
                        new(_currentInstr, _currentAddr, _logArgument, _logEffectiveAddress, !_isReadCycle),
                        new(_logA, _logX, _logY, _logS, _logP),
                        _logCycleCount
                    ));
                    InstructionCount++;
                }

                if (_break)
                {
                    _nes.Paused = true;
                    _break = false;
                }
            }
        }

        private void SetZNFlags(byte value)
        {
            Flag_Zero = value == 0;
            Flag_Negative = value > 127;
        }

        private ushort _correctBranchPc;
        private int signedOffset;

        private void Branch(bool takeBranch)
        {
            switch (_operationCycle)
            {
                case 0:
                    PollInterrupts(true);
                    Read(PC);
                    _logOperandA = _dataBus;
                    _logByteCodeLength = 2;

                    PC++;

                    signedOffset = _dataBus < 128 ? _dataBus : _dataBus - 256;
                    _correctBranchPc = (ushort)(PC + signedOffset);
                    _logArgument = _correctBranchPc;

                    if (!takeBranch)
                        _operationPhaseComplete = true;
                    break;
                case 1:
                    //only add to PCL, preserve PCH
                    PC = (ushort)((PC & 0xFF00) | (byte)(PC + signedOffset));
                    
                    if (PC == _correctBranchPc)
                        _operationPhaseComplete = true;

                    break;
                case 2:
                    PollInterrupts(false);
                    PC = _correctBranchPc;
                    _operationPhaseComplete = true;
                    break;
            }
        }

        private void PollInterrupts(bool canDisableIrq)
        {
            if (NmiLine && !NmiPinsSignal)
                DoNmi = true;
            NmiPinsSignal = NmiLine;

            if (!DoIrq || canDisableIrq)
                DoIrq = IrqLine > 0 && !Flag_InterruptDisable;
        }
    }
}
