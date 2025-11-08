using System;

namespace stasisEmulator.NesConsole
{
    //since we only overwrite data rather than freeing it, this shouldn't annoy the gc (as much)
    public class TraceLogColumn<T>(int capacity)
    {
        //changing the capacity requires extra logic that i don't wanna make rn
        public int Capacity { get; private set; } = capacity;
        public int Count { get; private set; } = 0;

        private readonly T[] values = new T[capacity];
        private int _startIndex = 0;
        
        public T this[int i]
        {
            get
            {
                if (i < 0 || i >= Capacity)
                    throw new IndexOutOfRangeException($"Index was outside the bounds of the {nameof(TraceLogColumn<T>)}");

                return values[(i + _startIndex) % Capacity];
            }
            set
            {
                if (i < 0 || i >= Capacity)
                    throw new IndexOutOfRangeException($"Index was outside the bounds of the {nameof(TraceLogColumn<T>)}");

                values[(i + _startIndex) % Capacity] = value;
            }
        }

        public void Add(T value)
        {
            int index = (_startIndex + Count) % Capacity;
            Count++;
            //if we're over capacity, start sliding the window rather than incrementing _count, overwriting the oldest elements
            if (Count > Capacity)
            {
                Count = Capacity;
                _startIndex++;
                _startIndex %= Capacity;
            }
            values[index] = value;
        }

        public void Clear()
        {
            Count = 0;
            _startIndex = 0;
        }
    }

    public readonly struct ByteCode
    {
        public readonly byte Opcode;
        public readonly byte OperandA;
        public readonly byte OperandB;

        public readonly byte Length;

        public ByteCode(byte opCode)
        {
            Opcode = opCode;
            Length = 1;
        }

        public ByteCode(byte opCode, byte operand)
        {
            Opcode = opCode;
            OperandA = operand;
            Length = 2;
        }

        public ByteCode(byte opCode, byte operandA, byte operandB)
        {
            Opcode = opCode;
            OperandA = operandA;
            OperandB = operandB;
            Length = 3;
        }
    }

    public readonly struct Disassembly(Cpu.Instr instruction, Cpu.Addr addressingMode, ushort argument, ushort indirectAddress, bool writeInstruction)
    {
        public readonly Cpu.Instr Instruction = instruction;
        public readonly Cpu.Addr AddressingMode = addressingMode;
        public readonly ushort Argument = argument;
        public readonly ushort EffectiveAddress = indirectAddress;
        public readonly bool WriteInstruction = writeInstruction;
    }

    public readonly struct Registers(byte a, byte x, byte y, byte s, byte p)
    {
        public readonly byte A = a;
        public readonly byte X = x;
        public readonly byte Y = y;

        public readonly byte S = s;
        public readonly byte P = p;
    }

    public readonly struct TraceLoggerRow(ushort pc, ByteCode byteCode, Disassembly disassembly, Registers registers, ulong cycleCount)
    {
        public readonly ushort PC = pc;
        public readonly ByteCode ByteCode = byteCode;
        public readonly Disassembly Disassembly = disassembly;
        public readonly Registers Registers = registers;
        public readonly ulong CycleCount = cycleCount;
    }

    public class TraceLogger(int capacity)
    {
        private readonly TraceLogColumn<ushort> _pc = new(capacity);
        private readonly TraceLogColumn<ByteCode> _byteCode = new(capacity);
        private readonly TraceLogColumn<Disassembly> _disassembly = new(capacity);
        private readonly TraceLogColumn<Registers> _registers = new(capacity);
        private readonly TraceLogColumn<ulong> _cycleCount = new(capacity);

        public int Capacity { get; private set; } = capacity;

        //don't quite like that there are 4 different counts to pick from, but we should only add to all 4 at once anyways, so any works
        public int Count { get { return _pc.Count; } }

        public TraceLoggerRow this[int index]
        {
            get
            {
                return new TraceLoggerRow(_pc[index], _byteCode[index], _disassembly[index], _registers[index], _cycleCount[index]);
            }
            set
            {
                _pc[index] = value.PC;
                _byteCode[index] = value.ByteCode;
                _disassembly[index] = value.Disassembly;
                _registers[index] = value.Registers;
                _cycleCount[index] = value.CycleCount;
            }
        }

        public void Add(TraceLoggerRow row)
        {
            _pc.Add(row.PC);
            _byteCode.Add(row.ByteCode);
            _disassembly.Add(row.Disassembly);
            _registers.Add(row.Registers);
            _cycleCount.Add(row.CycleCount);
        }

        public void Clear()
        {
            _pc.Clear();
            _byteCode.Clear();
            _disassembly.Clear();
            _registers.Clear();
            _cycleCount.Clear();
        }
    }
}
