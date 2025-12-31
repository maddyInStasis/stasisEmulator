using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.SaveStates
{
    public class CpuState
    {
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

        public bool NmiLine;
        public bool NmiPinsSignal;

        public bool DoNmi;

        public int IrqLine;
        public bool IrqLevel;
        public bool DoIrq;

        public Cpu.Interrupt _interruptToRun;

        public byte[] Ram = new byte[0x800];

        public ulong CycleCount;
        public ulong InstructionCount;

        public ushort _logPc = 0;

        public byte _logOperandA = 0;
        public byte _logOperandB = 0;
        public byte _logByteCodeLength = 1;

        public byte _logA = 0;
        public byte _logX = 0;
        public byte _logY = 0;
        public byte _logS = 0;
        public byte _logP = 0;

        public ushort _logArgument = 0;
        public ushort _logEffectiveAddress = 0;

        public ulong _logCycleCount;

        public bool _ignoreCurrentLog = false;

        public ushort _addressBus = 0;
        public ushort _addressLatch = 0;
        public byte _dataBus = 0;
        public byte _dataLatch = 0;

        public Cpu.CurrentOperation _operationPhase;
        public int _operationCycle;
        public bool _operationPhaseComplete;

        public byte _opcode;

        public Cpu.Instr _currentInstr;
        public Cpu.Addr _currentAddr;

        public bool _halt = false;
        public bool _break = false;

        public bool _isReadCycle = false;

        public bool _oamDma = false;
        public bool DmcDma = false;

        public bool _dmaTryHalt = false;
        public bool _dmaTryAlign = false;

        public bool _dmcDmaDummyCycleComplete = false;
        public ushort _dmcDmaAddress;

        public ushort _oamDmaPage;
        public byte _oamDmaIndex = 0;

        public bool _doReset = false;
    }
}
