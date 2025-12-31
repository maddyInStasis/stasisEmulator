using Microsoft.Xna.Framework.Audio;
using stasisEmulator.NesCore.ApuComponents;
using stasisEmulator.NesCore.SaveStates;
using System;
using System.Diagnostics;
using System.Threading;

namespace stasisEmulator.NesCore
{
    public class Apu
    {
        public readonly Envelope Pulse1Envelope = new();
        public Sweep Pulse1Sweep;
        public readonly PulseSequencer Pulse1Sequencer = new();
        public readonly LengthCounter Pulse1LengthCounter = new();

        public readonly Envelope Pulse2Envelope = new();
        public Sweep Pulse2Sweep;
        public readonly PulseSequencer Pulse2Sequencer = new();
        public readonly LengthCounter Pulse2LengthCounter = new();

        public TriangleSequencer TriangleSequencer;
        public LinearCounter TriangleLinearCounter = new();
        public readonly LengthCounter TriangleLengthCounter = new();

        public readonly Envelope NoiseEnvelope = new();
        public readonly NoiseShiftRegister NoiseShiftRegister = new();
        public readonly LengthCounter NoiseLengthCounter = new();

        //TODO: cleanup (create component classes)
        public bool DmcEnable;
        public bool DmcIrqEnabled;
        public bool DmcLoop;
        public ushort DmcPeriod;
        public ushort DmcTimer;
        public byte DmcOutputLevel;
        public bool DmcSilence;
        public byte DmcSampleBuffer;
        public bool SampleBufferLoaded;
        public byte DmcShiftRegister;
        public byte DmcSampleBitsRemaining;
        public ushort DmcSampleAddress;
        public ushort DmcCurrentAddress;
        public ushort DmcSampleLength;
        public ushort DmcBytesRemaining;

        public bool PutCycle;

        public int FrameCounter = 0;
        public bool FrameCounter5Step;
        public bool InterruptInhibit;
        private int _frameCounterResetTimer = -1;

        private bool _frameInterrupt = false;
        public bool FrameInterruptFlag
        {
            get => _frameInterrupt;
            set
            {
                if (_frameInterrupt == value)
                    return;
                _frameInterrupt = value;

                if (value)
                    _nes.Cpu.IrqLine++;
                else
                    _nes.Cpu.IrqLine--;
            }
        }
        private bool _dmcInterruptFlag = false;
        public bool DmcInterruptFlag
        {
            get => _dmcInterruptFlag;
            set
            {
                if (_dmcInterruptFlag == value)
                    return;
                _dmcInterruptFlag = value;

                if (value)
                    _nes.Cpu.IrqLine++;
                else
                    _nes.Cpu.IrqLine--;
            }
        }

        private bool FrameInterruptSetThisCycle;

        private readonly Nes _nes;

        public float OutputVolume = 0.5f;

        private const int PlaybackSampleRate = 44100;
        private readonly DynamicSoundEffectInstance _dynamicSoundEffectInstance = new(PlaybackSampleRate, AudioChannels.Mono);

        private const int BufferSize = PlaybackSampleRate / 60;
        private readonly byte[] _outputBuffer = new byte[BufferSize * 2];
        private readonly short[] _workingBuffer = new short[BufferSize];
        private readonly short[] _apuOutput = new short[341 * 262 / 6];
        private int _apuSampleCount = 0;

        public bool IsRunning = true;

        public Apu(Nes nes)
        {
            Pulse1Sweep = new(Pulse1Sequencer);
            Pulse2Sweep = new(Pulse2Sequencer) { TwosComplement = true };
            TriangleSequencer = new(TriangleLengthCounter, TriangleLinearCounter);

            _nes = nes;
            Thread t = new(() =>
            {
                while (IsRunning)
                {
                    Thread.Sleep(1);
                    if (_dynamicSoundEffectInstance.PendingBufferCount < 3)
                        SubmitBuffer();
                }
            });

            t.Start();
            _dynamicSoundEffectInstance.Play();
        }

        public void Power()
        {
            Pulse1Envelope.Power();
            Pulse1Sweep.Power();
            Pulse1Sequencer.Power();
            Pulse1LengthCounter.Power();

            Pulse2Envelope.Power();
            Pulse2Sweep.Power();
            Pulse2Sequencer.Power();
            Pulse2LengthCounter.Power();

            TriangleLinearCounter.Power();
            TriangleSequencer.Power();
            TriangleLengthCounter.Power();

            NoiseEnvelope.Power();
            NoiseShiftRegister.Power();
            NoiseLengthCounter.Power();

            DmcEnable = false;
            DmcIrqEnabled = false;
            DmcLoop = false;
            DmcPeriod = GetDmcPeriod(0);
            DmcTimer = 0;
            DmcOutputLevel = 0;
            DmcSilence = false;
            DmcSampleBuffer = 0;
            SampleBufferLoaded = false;
            DmcShiftRegister = 0;
            DmcSampleBitsRemaining = 0;
            DmcSampleAddress = 0;
            DmcCurrentAddress = 0;
            DmcSampleLength = 0;
            DmcBytesRemaining = 0;


        FrameCounter5Step = false;
            InterruptInhibit = false;

            DmcOutputLevel = 0;

            Reset();
        }

        public void Reset()
        {
            TriangleSequencer.SequencerPosition = 0;

            Pulse1LengthCounter.Enabled = false;
            Pulse2LengthCounter.Enabled = false;
            TriangleLengthCounter.Enabled = false;
            NoiseLengthCounter.Enabled = false;

            DmcOutputLevel &= 1;
        }

        public byte RegisterRead(ushort address, ref byte dataBus)
        {
            if (address == 0x4015)
            {
                byte status = 0;
                status |= (byte)(Pulse1LengthCounter.GateOutput() ? 1 : 0);
                status |= (byte)(Pulse2LengthCounter.GateOutput() ? 2 : 0);
                status |= (byte)(TriangleLengthCounter.GateOutput() ? 4 : 0);
                status |= (byte)(NoiseLengthCounter.GateOutput() ? 8 : 0);
                status |= (byte)(DmcBytesRemaining > 0 ? 16 : 0);
                status |= (byte)(FrameInterruptFlag ? 0x40 : 0);
                status |= (byte)(DmcInterruptFlag ? 0x80 : 0);
                dataBus = (byte)((dataBus & 0x20) | (status & (0xFF ^ 0x20)));

                if (!FrameInterruptSetThisCycle)
                    FrameInterruptFlag = false;
            }
            return dataBus;
        }

        public void RegisterWrite(ushort address, byte value)
        {
            switch (address)
            {
                case 0x4000:
                    Pulse1Sequencer.DutyCycle = (byte)(value >> 6);
                    Pulse1LengthCounter.Halt = (value & 0x20) != 0;
                    Pulse1Envelope.Loop = Pulse1LengthCounter.Halt;
                    Pulse1Envelope.ConstantVolume = (value & 0x10) != 0;
                    Pulse1Envelope.EnvelopeParameter = (byte)(value & 15);
                    break;
                case 0x4001:
                    Pulse1Sweep.ShiftCount = (byte)(value & 7);
                    Pulse1Sweep.Negate = (value & 8) != 0;
                    Pulse1Sweep.DividerPeriod = (byte)((value >> 4) & 7);
                    Pulse1Sweep.Enabled = (value & 0x80) != 0;
                    break;
                case 0x4002:
                    Pulse1Sequencer.TimerPeriod = (ushort)((Pulse1Sequencer.TimerPeriod & 0x700) | value);
                    break;
                case 0x4003:
                    Pulse1Sequencer.TimerPeriod = (ushort)(((value << 8) & 0x700) | (Pulse1Sequencer.TimerPeriod & 0x00FF));
                    Pulse1LengthCounter.Load((byte)(value >> 3));
                    Pulse1Sequencer.SequencerPosition = 0;
                    Pulse1Envelope.Start = true;
                    break;
                case 0x4004:
                    Pulse2Sequencer.DutyCycle = (byte)(value >> 6);
                    Pulse2LengthCounter.Halt = (value & 0x20) != 0;
                    Pulse2Envelope.Loop = Pulse2LengthCounter.Halt;
                    Pulse2Envelope.ConstantVolume = (value & 0x10) != 0;
                    Pulse2Envelope.EnvelopeParameter = (byte)(value & 15);
                    break;
                case 0x4005:
                    Pulse2Sweep.ShiftCount = (byte)(value & 7);
                    Pulse2Sweep.Negate = (value & 8) != 0;
                    Pulse2Sweep.DividerPeriod = (byte)((value >> 4) & 7);
                    Pulse2Sweep.Enabled = (value & 0x80) != 0;
                    break;
                case 0x4006:
                    Pulse2Sequencer.TimerPeriod = (ushort)((Pulse2Sequencer.TimerPeriod & 0x700) | value);
                    break;
                case 0x4007:
                    Pulse2Sequencer.TimerPeriod = (ushort)(((value << 8) & 0x700) | (Pulse2Sequencer.TimerPeriod & 0x00FF));
                    Pulse2LengthCounter.Load((byte)(value >> 3));
                    Pulse2Sequencer.SequencerPosition = 0;
                    Pulse2Envelope.Start = true;
                    break;
                case 0x4008:
                    TriangleLinearCounter.CounterReloadValue = (byte)(value & 127);
                    TriangleLinearCounter.ControlFlag = (value & 0x80) != 0;
                    TriangleLengthCounter.Halt = TriangleLinearCounter.ControlFlag;
                    break;
                case 0x400A:
                    TriangleSequencer.TimerPeriod = (ushort)((TriangleSequencer.TimerPeriod & 0x700) | (value));
                    break;
                case 0x400B:
                    TriangleSequencer.TimerPeriod = (ushort)(((value << 8) & 0x700) | (TriangleSequencer.TimerPeriod & 0x00FF));
                    TriangleLengthCounter.Load((byte)(value >> 3));
                    TriangleLinearCounter.CounterReloadFlag = true;
                    break;
                case 0x400C:
                    NoiseLengthCounter.Halt = (value & 0x20) != 0;
                    NoiseEnvelope.Loop = Pulse1LengthCounter.Halt;
                    NoiseEnvelope.ConstantVolume = (value & 0x10) != 0;
                    NoiseEnvelope.EnvelopeParameter = (byte)(value & 15);
                    break;
                case 0x400E:
                    NoiseShiftRegister.Mode = (value & 0x80) != 0;
                    NoiseShiftRegister.LoadPeriod((byte)(value & 15));
                    break;
                case 0x400F:
                    NoiseLengthCounter.Load((byte)(value >> 3));
                    NoiseEnvelope.Start = true;
                    break;
                case 0x4010:
                    DmcPeriod = GetDmcPeriod((byte)(value & 0x0F));
                    DmcLoop = (value & 0x40) != 0;
                    DmcIrqEnabled = (value & 0x80) != 0;
                    if (!DmcIrqEnabled)
                        DmcInterruptFlag = false;
                    break;
                case 0x4011:
                    DmcOutputLevel = (byte)(value & 0x7F);
                    break;
                case 0x4012:
                    DmcSampleAddress = (ushort)(0xC000 | (value << 6));
                    DmcCurrentAddress = DmcSampleAddress;
                    break;
                case 0x4013:
                    DmcSampleLength = (ushort)((value << 4) | 1);
                    break;
                case 0x4015:
                    Pulse1LengthCounter.Enabled = (value & 1) != 0;
                    Pulse2LengthCounter.Enabled = (value & 2) != 0;
                    TriangleLengthCounter.Enabled = (value & 4) != 0;
                    NoiseLengthCounter.Enabled = (value & 8) != 0;
                    DmcEnable = (value & 16) != 0;

                    DmcInterruptFlag = false;

                    if (!DmcEnable)
                        DmcBytesRemaining = 0;
                    else if (DmcBytesRemaining == 0)
                        DmcBytesRemaining = DmcSampleLength;
                    break;
                case 0x4017:
                    FrameCounter5Step = (value & 0x80) != 0;
                    InterruptInhibit = (value & 0x40) != 0;
                    if (InterruptInhibit)
                        FrameInterruptFlag = false;

                    _frameCounterResetTimer = 4;
                    break;
            }
        }

        private readonly ushort[] _dmcPeriods = [
            428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54
        ];
        public ushort GetDmcPeriod(byte index)
        {
            return _dmcPeriods[index];
        }

        public void RunCycle()
        {
            Pulse1Sequencer.Clock();
            Pulse2Sequencer.Clock();
            NoiseShiftRegister.Clock();

            if (DmcTimer == 0)
            {
                DmcTimer = DmcPeriod;

                if (!DmcSilence)
                {
                    bool add = (DmcShiftRegister & 1) != 0;
                    if (add && DmcOutputLevel <= 125)
                        DmcOutputLevel += 2;
                    if (!add && DmcOutputLevel >= 2)
                        DmcOutputLevel -= 2;
                }

                DmcShiftRegister >>= 1;
                DmcSampleBitsRemaining--;

                if (DmcSampleBitsRemaining == 0)
                {
                    DmcSampleBitsRemaining = 8;
                    if (!SampleBufferLoaded)
                    {
                        DmcSilence = true;
                    }
                    else
                    {
                        DmcSilence = false;
                        SampleBufferLoaded = false;
                        DmcShiftRegister = DmcSampleBuffer;
                    }
                }
            }
            DmcTimer -= 2;

            if (DmcEnable && !SampleBufferLoaded && DmcBytesRemaining > 0 && !_nes.Cpu.DmcDma)
            {
                _nes.Cpu.StartDmcDma(DmcCurrentAddress);
                DmcCurrentAddress++;
                if (DmcCurrentAddress == 0)
                    DmcCurrentAddress = 0x8000;
                DmcBytesRemaining--;
                if (DmcBytesRemaining == 0)
                {
                    if (DmcLoop)
                    {
                        DmcCurrentAddress = DmcSampleAddress;
                        DmcBytesRemaining = DmcSampleLength;
                    }
                    else if (DmcIrqEnabled)
                    {
                        DmcInterruptFlag = true;
                    }
                }
            }

            DoFrameCounter();
            OutputSample(GetMixerOutput());
        }

        public void CpuClock()
        {
            if (_frameCounterResetTimer > 0)
                _frameCounterResetTimer--;
            if (_frameCounterResetTimer == 0)
            {
                _frameCounterResetTimer = -1;
                FrameCounter = 0;
                if (FrameCounter5Step)
                {
                    QuarterFrameClock();
                    HalfFrameClock();
                }
            }

            FrameInterruptSetThisCycle = false;
            TriangleSequencer.Clock();
            if (FrameCounter == 14914 && !InterruptInhibit && !FrameCounter5Step)
            {
                if (!FrameInterruptFlag)
                    FrameInterruptSetThisCycle = true;

                FrameInterruptFlag = true;
            }
        }

        private void DoFrameCounter()
        {
            if (FrameCounter == 3728)
                QuarterFrameClock();

            if (FrameCounter == 7456)
            {
                QuarterFrameClock();
                HalfFrameClock();
            }

            if (FrameCounter == 11185)
                QuarterFrameClock();

            if ((FrameCounter == 14914 && !FrameCounter5Step) || FrameCounter == 18640)
            {
                QuarterFrameClock();
                HalfFrameClock();
            }

            FrameCounter++;

            if (FrameCounter == 14915 && !FrameCounter5Step)
            {
                FrameCounter = 0;
                if (!InterruptInhibit)
                {
                    if (!FrameInterruptFlag)
                        FrameInterruptSetThisCycle = true;

                    FrameInterruptFlag = true;
                }
            }
            if (FrameCounter == 18641)
                FrameCounter = 0;
        }

        private void QuarterFrameClock()
        {
            Pulse1Envelope.Clock();
            Pulse2Envelope.Clock();
            TriangleLinearCounter.Clock();
            NoiseEnvelope.Clock();
        }

        private void HalfFrameClock()
        {
            Pulse1LengthCounter.Clock();
            Pulse1Sweep.Clock();

            Pulse2LengthCounter.Clock();
            Pulse2Sweep.Clock();

            TriangleLengthCounter.Clock();

            NoiseLengthCounter.Clock();
        }

        private short GetMixerOutput()
        {
            bool pulse1Gate = Pulse1Sequencer.GateOutput() && Pulse1LengthCounter.GateOutput() && !Pulse1Sweep.Mute();
            byte pulse1Output = (byte)(pulse1Gate ? Pulse1Envelope.GetOutputVolume() : 0);

            bool pulse2Gate = Pulse2Sequencer.GateOutput() && Pulse2LengthCounter.GateOutput() && !Pulse2Sweep.Mute();
            byte pulse2Output = (byte)(pulse2Gate ? Pulse2Envelope.GetOutputVolume() : 0);

            byte triangleOutput = TriangleSequencer.GetOutputValue();

            bool noiseGate = NoiseShiftRegister.GateOutput() && NoiseLengthCounter.GateOutput();
            byte noiseOutput = (byte)(noiseGate ? NoiseEnvelope.GetOutputVolume() : 0);

            return MixChannels(pulse1Output, pulse2Output, triangleOutput, noiseOutput, DmcOutputLevel);
        }

        private short MixChannels(byte pulse1, byte pulse2, byte triangle, byte noise, byte dmc)
        {
            float pulseOut = pulse1 + pulse2 > 0 ? 95.88f / ((8128f / (pulse1 + pulse2)) + 100) : 0;
            float triangleOut = triangle / 8227f;
            float noiseOut = noise / 12241f;
            float dmcOut = dmc / 22638f;
            float tndOut = triangleOut + noiseOut + dmcOut > 0 ? 159.79f / ((1f / (triangleOut + noiseOut + dmcOut)) + 100) : 0;
            float output = pulseOut + tndOut;
            output *= OutputVolume;
            return (short)Math.Clamp(short.MaxValue * output, 0, short.MaxValue);
        }

        private void OutputSample(short sample)
        {
            if (_apuSampleCount >= _apuOutput.Length)
                return;

            _apuOutput[_apuSampleCount] = sample;
            _apuSampleCount++;
        }

        public void ApuPreFrame()
        {
            _apuSampleCount = 0;
        }

        public void ApuPostFrame()
        {
            float apuSamplesPerOutputSample = _apuSampleCount / (float)_workingBuffer.Length;
            float apuSamplePosition = 0;

            for (int i = 0; i < _workingBuffer.Length; i++)
            {
                int apuSampleIndex = (int)apuSamplePosition;
                apuSampleIndex = Math.Clamp(apuSampleIndex, 0, _apuOutput.Length - 1);

                _workingBuffer[i] = _apuOutput[apuSampleIndex];
                apuSamplePosition += apuSamplesPerOutputSample;
                apuSamplePosition = Math.Min(apuSamplePosition, _apuSampleCount - 1);
            }
        }

        private void SubmitBuffer()
        {
            if (_nes.Paused)
                return;

            ConvertBuffer(_workingBuffer, _outputBuffer);
            _dynamicSoundEffectInstance.SubmitBuffer(_outputBuffer);
        }

        private static void ConvertBuffer(short[] shortBuffer, byte[] outputBuffer)
        {
            if (outputBuffer.Length != shortBuffer.Length * 2)
                throw new Exception("Incorrect buffer size!");

            for (int i = 0; i < shortBuffer.Length; i++)
            {
                short shortSample = shortBuffer[i];

                if (!BitConverter.IsLittleEndian)
                {
                    outputBuffer[i * 2] = (byte)(shortSample >> 8);
                    outputBuffer[i * 2 + 1] = (byte)shortSample;
                }
                else
                {
                    outputBuffer[i * 2] = (byte)shortSample;
                    outputBuffer[i * 2 + 1] = (byte)(shortSample >> 8);
                }
            }
        }

        public ApuState SaveState()
        {
            return new ApuState()
            {
                Pulse1EnvelopeState = Pulse1Envelope.SaveState(),
                Pulse1SweepState = Pulse1Sweep.SaveState(),
                Pulse1SequencerState = Pulse1Sequencer.SaveState(),
                Pulse1LengthCounterState = Pulse1LengthCounter.SaveState(),

                Pulse2EnvelopeState = Pulse2Envelope.SaveState(),
                Pulse2SweepState = Pulse2Sweep.SaveState(),
                Pulse2SequencerState = Pulse2Sequencer.SaveState(),
                Pulse2LengthCounterState = Pulse2LengthCounter.SaveState(),

                TriangleSequencerState = TriangleSequencer.SaveState(),
                TriangleLinearCounterState = TriangleLinearCounter.SaveState(),
                TriangleLengthCounterState = TriangleLengthCounter.SaveState(),

                NoiseEnvelopeState = NoiseEnvelope.SaveState(),
                NoiseShiftRegisterState = NoiseShiftRegister.SaveState(),
                NoiseLengthCounterState = NoiseLengthCounter.SaveState(),

                DmcEnable = DmcEnable,
                DmcIrqEnabled = DmcIrqEnabled,
                DmcLoop = DmcLoop,
                DmcPeriod = DmcPeriod,
                DmcTimer = DmcTimer,
                DmcOutputLevel = DmcOutputLevel,
                DmcSilence = DmcSilence,
                DmcSampleBuffer = DmcSampleBuffer,
                SampleBufferLoaded = SampleBufferLoaded,
                DmcShiftRegister = DmcShiftRegister,
                DmcSampleBitsRemaining = DmcSampleBitsRemaining,
                DmcSampleAddress = DmcSampleAddress,
                DmcCurrentAddress = DmcCurrentAddress,
                DmcSampleLength = DmcSampleLength,
                DmcBytesRemaining = DmcBytesRemaining,

                PutCycle = PutCycle,

                FrameCounter = FrameCounter,
                FrameCounter5Step = FrameCounter5Step,
                InterruptInhibit = InterruptInhibit,
                _frameCounterResetTimer = _frameCounterResetTimer,

                _frameInterrupt = _frameInterrupt,
                _dmcInterruptFlag = _dmcInterruptFlag,

                FrameInterruptSetThisCycle = FrameInterruptSetThisCycle,
            };
        }

        public void LoadState(ApuState state)
        {
            Pulse1Envelope.LoadState(state.Pulse1EnvelopeState);
            Pulse1Sweep.LoadState(state.Pulse1SweepState);
            Pulse1Sequencer.LoadState(state.Pulse1SequencerState);
            Pulse1LengthCounter.LoadState(state.Pulse1LengthCounterState);

            Pulse2Envelope.LoadState(state.Pulse2EnvelopeState);
            Pulse2Sweep.LoadState(state.Pulse2SweepState);
            Pulse2Sequencer.LoadState(state.Pulse2SequencerState);
            Pulse2LengthCounter.LoadState(state.Pulse2LengthCounterState);

            TriangleSequencer.LoadState(state.TriangleSequencerState);
            TriangleLinearCounter.LoadState(state.TriangleLinearCounterState);
            TriangleLengthCounter.LoadState(state.TriangleLengthCounterState);

            NoiseEnvelope.LoadState(state.NoiseEnvelopeState);
            NoiseShiftRegister.LoadState(state.NoiseShiftRegisterState);
            NoiseLengthCounter.LoadState(state.NoiseLengthCounterState);

            DmcEnable = state.DmcEnable;
            DmcIrqEnabled = state.DmcIrqEnabled;
            DmcLoop = state.DmcLoop;
            DmcPeriod = state.DmcPeriod;
            DmcTimer = state.DmcTimer;
            DmcOutputLevel = state.DmcOutputLevel;
            DmcSilence = state.DmcSilence;
            DmcSampleBuffer = state.DmcSampleBuffer;
            SampleBufferLoaded = state.SampleBufferLoaded;
            DmcShiftRegister = state.DmcShiftRegister;
            DmcSampleBitsRemaining = state.DmcSampleBitsRemaining;
            DmcSampleAddress = state.DmcSampleAddress;
            DmcCurrentAddress = state.DmcCurrentAddress;
            DmcSampleLength = state.DmcSampleLength;
            DmcBytesRemaining = state.DmcBytesRemaining;

            PutCycle = state.PutCycle;

            FrameCounter = state.FrameCounter;
            FrameCounter5Step = state.FrameCounter5Step;
            InterruptInhibit = state.InterruptInhibit;
            _frameCounterResetTimer = state._frameCounterResetTimer;

            _frameInterrupt = state._frameInterrupt;
            _dmcInterruptFlag = state._dmcInterruptFlag;

            FrameInterruptSetThisCycle = state.FrameInterruptSetThisCycle;
        }
    }
}
