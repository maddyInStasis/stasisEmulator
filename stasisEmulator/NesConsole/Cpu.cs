using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            NOP
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
            Instr.BRK    , Instr.ORA    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.NotImpl, Instr.PHP    , Instr.ORA    , Instr.ASL    , Instr.NotImpl, Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.NotImpl, //00
            Instr.BPL    , Instr.ORA    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.NotImpl, Instr.CLC    , Instr.ORA    , Instr.NOP    , Instr.NotImpl, Instr.NOP    , Instr.ORA    , Instr.ASL    , Instr.NotImpl, //10
            Instr.JSR    , Instr.AND    , Instr.HLT    , Instr.NotImpl, Instr.BIT    , Instr.AND    , Instr.ROL    , Instr.NotImpl, Instr.PLP    , Instr.AND    , Instr.ROL    , Instr.NotImpl, Instr.BIT    , Instr.AND    , Instr.ROL    , Instr.NotImpl, //20
            Instr.BMI    , Instr.AND    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.AND    , Instr.ROL    , Instr.NotImpl, Instr.SEC    , Instr.AND    , Instr.NOP    , Instr.NotImpl, Instr.NOP    , Instr.AND    , Instr.ROL    , Instr.NotImpl, //30
            Instr.RTI    , Instr.EOR    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.EOR    , Instr.LSR    , Instr.NotImpl, Instr.PHA    , Instr.EOR    , Instr.LSR    , Instr.NotImpl, Instr.JMP_abs, Instr.EOR    , Instr.LSR    , Instr.NotImpl, //40
            Instr.BVC    , Instr.EOR    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.EOR    , Instr.LSR    , Instr.NotImpl, Instr.CLI    , Instr.EOR    , Instr.NOP    , Instr.NotImpl, Instr.NOP    , Instr.EOR    , Instr.LSR    , Instr.NotImpl, //50
            Instr.RTS    , Instr.ADC    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.ADC    , Instr.ROR    , Instr.NotImpl, Instr.PLA    , Instr.ADC    , Instr.ROR    , Instr.NotImpl, Instr.JMP_ind, Instr.ADC    , Instr.ROR    , Instr.NotImpl, //60
            Instr.BVS    , Instr.ADC    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.ADC    , Instr.ROR    , Instr.NotImpl, Instr.SEI    , Instr.ADC    , Instr.NOP    , Instr.NotImpl, Instr.NOP    , Instr.ADC    , Instr.ROR    , Instr.NotImpl, //70
            Instr.NOP    , Instr.STA    , Instr.NOP    , Instr.NotImpl, Instr.STY    , Instr.STA    , Instr.STX    , Instr.NotImpl, Instr.DEY    , Instr.NOP    , Instr.TXA    , Instr.NotImpl, Instr.STY    , Instr.STA    , Instr.STX    , Instr.NotImpl, //80
            Instr.BCC    , Instr.STA    , Instr.HLT    , Instr.NotImpl, Instr.STY    , Instr.STA    , Instr.STX    , Instr.NotImpl, Instr.TYA    , Instr.STA    , Instr.TXS    , Instr.NotImpl, Instr.NotImpl, Instr.STA    , Instr.NotImpl, Instr.NotImpl, //90
            Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.NotImpl, Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.NotImpl, Instr.TAY    , Instr.LDA    , Instr.TAX    , Instr.NotImpl, Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.NotImpl, //A0
            Instr.BCS    , Instr.LDA    , Instr.HLT    , Instr.NotImpl, Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.NotImpl, Instr.CLV    , Instr.LDA    , Instr.TSX    , Instr.NotImpl, Instr.LDY    , Instr.LDA    , Instr.LDX    , Instr.NotImpl, //B0
            Instr.CPY    , Instr.CMP    , Instr.NOP    , Instr.NotImpl, Instr.CPY    , Instr.CMP    , Instr.DEC    , Instr.NotImpl, Instr.INY    , Instr.CMP    , Instr.DEX    , Instr.NotImpl, Instr.CPY    , Instr.CMP    , Instr.DEC    , Instr.NotImpl, //C0
            Instr.BNE    , Instr.CMP    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.CMP    , Instr.DEC    , Instr.NotImpl, Instr.CLD    , Instr.CMP    , Instr.NOP    , Instr.NotImpl, Instr.NOP    , Instr.CMP    , Instr.DEC    , Instr.NotImpl, //D0
            Instr.CPX    , Instr.SBC    , Instr.NOP    , Instr.NotImpl, Instr.CPX    , Instr.SBC    , Instr.INC    , Instr.NotImpl, Instr.INX    , Instr.SBC    , Instr.NOP    , Instr.SBC    , Instr.CPX    , Instr.SBC    , Instr.INC    , Instr.NotImpl, //E0
            Instr.BEQ    , Instr.SBC    , Instr.HLT    , Instr.NotImpl, Instr.NOP    , Instr.SBC    , Instr.INC    , Instr.NotImpl, Instr.SED    , Instr.SBC    , Instr.NOP    , Instr.NotImpl, Instr.NOP    , Instr.SBC    , Instr.INC    , Instr.NotImpl  //F0
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

        public byte A { get; private set; }
        public byte X { get; private set; }
        public byte Y { get; private set; }

        public ushort PC { get; private set; }
        public byte S { get; private set; }

        public bool Flag_Carry { get; private set; }
        public bool Flag_Zero { get; private set; }
        public bool Flag_InterruptDisable { get; private set; }
        public bool Flag_Decimal { get; private set; }
        public bool Flag_Overflow { get; private set; }
        public bool Flag_Negative { get; private set; }

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

        public bool DoNmi { get; set; }

        public readonly byte[] Ram = new byte[0x800];

        public ulong CycleCount { get; private set; }
        public ulong InstructionCount { get; private set; }
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
        private bool _break = false;

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
            Flag_InterruptDisable = true;

            _operationPhase = OperationPhase.Fetch;
            _operationCycle = 0;
            _operationPhaseComplete = false;

            CycleCount = 1;
            InstructionCount = 0;

            _doReset = true;
        }

        //TODO: remove temp controller stuff
        private byte Read(ushort address)
        {
            if (address < 0x2000)
                _dataBus = Ram[address & (0x800 - 1)];
            else if (address >= 0x4020)
                _nes.Cartridge?.ReadCartridgeCpu(address, ref _dataBus);
            else if (address < 0x4000)
                _nes.Ppu.ReadRegister(address, ref _dataBus);
            else if (address == 0x4016)
                _nes.Player1Controller?.RegisterRead(ref _dataBus);
            else if (address == 0x4017)
                _nes.Player2Controller?.RegisterRead(ref _dataBus);

            return _dataBus;
        }
        private void Write(ushort address, byte value)
        {
            _dataBus = value;

            //if (address == 0x600)
            //    _break = true;

            if (address < 0x2000)
                Ram[address & (0x800 - 1)] = value;
            else if (address >= 0x4020)
                _nes.Cartridge?.WriteCartridgeCpu(address, value);
            else if (address < 0x4000)
                _nes.Ppu.WriteRegister(address, value);
            else if (address == 0x4016)
            {
                _nes.Player1Controller?.RegisterWrite(value);
                _nes.Player2Controller?.RegisterWrite(value);
            }
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
        }

        public void Fetch()
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

            if (_doReset)
                DoNmi = false;

            //interrupts execute opcode 0, the behavior of which depends on the interrupt.
            //reset supposedly dummy reads from the stack 3 times and decrements each time, explaining the SP -= 3 on every reset
            if (_doReset || DoNmi)
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

        public void DoAddressing()
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
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + X) & 0x00FF));
                            _logEffectiveAddress = _addressBus;
                            PC++;

                            if (_addressBus == _correctIndexedAddress)
                                _operationPhaseComplete = true;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _logEffectiveAddress = _addressBus;
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
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + X) & 0x00FF));
                            PC++;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _logEffectiveAddress = _addressBus;
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
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));
                            _logEffectiveAddress = _addressBus;
                            PC++;

                            if (_addressBus == _correctIndexedAddress)
                                _operationPhaseComplete = true;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _logEffectiveAddress = _addressBus;
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
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));
                            PC++;

                            break;
                        case 2:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _logEffectiveAddress = _addressBus;
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
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));
                            _logEffectiveAddress = _addressBus;

                            if (_addressBus == _correctIndexedAddress)
                                _operationPhaseComplete = true;

                            break;
                        case 3:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _logEffectiveAddress = _addressBus;
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
                            _addressBus = (ushort)((_addressBus & 0xFF00) | ((_addressBus + Y) & 0x00FF));

                            break;
                        case 3:
                            Read(_addressBus);
                            _addressBus = _correctIndexedAddress;
                            _logEffectiveAddress = _addressBus;
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

        public void DoInstructionCycle()
        {
            switch (_currentInstr)
            {
                case Instr.BRK:
                    switch (_operationCycle)
                    {
                        case 0:
                            Read(PC);
                            break;
                        case 1:
                            if (_doReset)
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
                            if (_doReset)
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
                            if (_doReset)
                            {
                                Read((ushort)(0x100 + S));
                                S--;
                            }
                            else
                            {
                                if (DoNmi)
                                    Push(P);
                                else
                                    Push((byte)(P | Break));
                            }
                            break;
                        case 4:
                            ushort fetchAddr = (ushort)(_doReset ? 0xFFFC : (DoNmi ? 0xFFFA : 0xFFFE));
                            PC = Read(fetchAddr);
                            break;
                        case 5:
                            fetchAddr = (ushort)(_doReset ? 0xFFFD : (DoNmi ? 0xFFFB : 0xFFFF));
                            PC |= (ushort)(Read(fetchAddr) << 8);

                            _doReset = false;
                            DoNmi = false;

                            _operationPhaseComplete = true;
                            break;
                    }

                    break;
                case Instr.LDA:
                    A = Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.STA:
                    Write(_addressBus, A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.LDX:
                    X = Read(_addressBus);
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.STX:
                    Write(_addressBus, X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.LDY:
                    Y = Read(_addressBus);
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.STY:
                    Write(_addressBus, Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TAX:
                    X = A;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TXA:
                    A = X;
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TAY:
                    Y = A;
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.TYA:
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
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.ORA:
                    A |= Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.AND:
                    A &= Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.EOR:
                    A ^= Read(_addressBus);
                    SetZNFlags(A);
                    _operationPhaseComplete = true;
                    break;
                case Instr.BIT:
                    Read(_addressBus);
                    Flag_Zero = (A & _dataBus) == 0;
                    Flag_Negative = (_dataBus & 0x80) != 0;
                    Flag_Overflow = (_dataBus & 0x40) != 0;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CMP:
                    Read(_addressBus);
                    Flag_Carry = A >= _dataBus;
                    Flag_Zero = A == _dataBus;
                    Flag_Negative = (byte)(A - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CPX:
                    Read(_addressBus);
                    Flag_Carry = X >= _dataBus;
                    Flag_Zero = X == _dataBus;
                    Flag_Negative = (byte)(X - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CPY:
                    Read(_addressBus);
                    Flag_Carry = Y >= _dataBus;
                    Flag_Zero = Y == _dataBus;
                    Flag_Negative = (byte)(Y - _dataBus) > 127;
                    _operationPhaseComplete = true;
                    break;
                case Instr.ADC:
                    Read(_addressBus);
                    int sum = A + _dataBus + (Flag_Carry ? 1 : 0);
                    Flag_Carry = sum > 0xFF;
                    Flag_Overflow = ((sum ^ A) & (sum ^ _dataBus) & 0x80) != 0;
                    SetZNFlags((byte)sum);
                    A = (byte)sum;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SBC:
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
                            Write(_addressBus, _dataBus);
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.INX:
                    X++;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.DEX:
                    X--;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.INY:
                    Y++;
                    SetZNFlags(Y);
                    _operationPhaseComplete = true;
                    break;
                case Instr.DEY:
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
                            PC = (ushort)(Read((ushort)((_pointer & 0xFF00) | ((_pointer + 1) & 0x00FF))) << 8);
                            PC |= _addressBus;
                            _logEffectiveAddress = _addressBus;
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
                            A = Read((ushort)(0x100 + S));
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
                            P = Read((ushort)(0x100 + S));
                            _operationPhaseComplete = true;
                            break;
                    }
                    break;
                case Instr.TXS:
                    S = X;
                    _operationPhaseComplete = true;
                    break;
                case Instr.TSX:
                    X = S;
                    SetZNFlags(X);
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLC:
                    Flag_Carry = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SEC:
                    Flag_Carry = true;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLI:
                    Flag_InterruptDisable = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SEI:
                    Flag_InterruptDisable = true;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLD:
                    Flag_Decimal = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.SED:
                    Flag_Decimal = true;
                    _operationPhaseComplete = true;
                    break;
                case Instr.CLV:
                    Flag_Overflow = false;
                    _operationPhaseComplete = true;
                    break;
                case Instr.NOP:
                    if (_currentAddr != Addr.Imp)
                        Read(_addressBus);
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
                        new(_currentInstr, _currentAddr, _logArgument, _logEffectiveAddress),
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
                    PC = (ushort)((PC & 0xff00) | (byte)(PC + signedOffset));
                    
                    if (PC == _correctBranchPc)
                        _operationPhaseComplete = true;

                    break;
                case 2:
                    PC = (ushort)_correctBranchPc;
                    _operationPhaseComplete = true;
                    break;
            }
        }
    }
}
