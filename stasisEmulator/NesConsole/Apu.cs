using Microsoft.Xna.Framework.Audio;
using stasisEmulator.NesConsole.ApuComponents;
using System;
using System.Threading;

namespace stasisEmulator.NesConsole
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

        public int FrameCounter = 0;
        public bool FrameCounter5Step;
        public bool InterruptInhibit;
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

            FrameCounter5Step = false;
            InterruptInhibit = false;

            Reset();
        }

        public void Reset()
        {
            TriangleSequencer.SequencerPosition = 0;

            Pulse1LengthCounter.Enabled = false;
            Pulse2LengthCounter.Enabled = false;
            TriangleLengthCounter.Enabled = false;
            NoiseLengthCounter.Enabled = false;
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
                status |= (byte)(_nes.Cpu.IrqLine ? 0x40 : 0);
                dataBus = (byte)((dataBus & 0x20) | (status & (0xFF ^ 0x20)));

                if (!FrameInterruptSetThisCycle)
                    _nes.Cpu.IrqLine = false;
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
                case 0x4015:
                    Pulse1LengthCounter.Enabled = (value & 1) != 0;
                    Pulse2LengthCounter.Enabled = (value & 2) != 0;
                    TriangleLengthCounter.Enabled = (value & 4) != 0;
                    NoiseLengthCounter.Enabled = (value & 8) != 0;
                    break;
                case 0x4017:
                    FrameCounter5Step = (value & 0x80) != 0;
                    InterruptInhibit = (value & 0x40) != 0;
                    if (InterruptInhibit)
                        _nes.Cpu.IrqLine = false;

                    //TODO: this should happen "3 or 4 CPU cycles" later
                    FrameCounter = 0;
                    if (FrameCounter5Step)
                    {
                        QuarterFrameClock();
                        HalfFrameClock();
                    }
                    break;
            }
        }

        public void RunCycle()
        {
            Pulse1Sequencer.Clock();
            Pulse2Sequencer.Clock();
            NoiseShiftRegister.Clock();

            DoFrameCounter();

            OutputSample(GetMixerOutput());
        }

        public void CpuClock()
        {
            FrameInterruptSetThisCycle = false;
            TriangleSequencer.Clock();
            if (FrameCounter == 14914 && !InterruptInhibit && !FrameCounter5Step)
            {
                if (!_nes.Cpu.IrqLine)
                    FrameInterruptSetThisCycle = true;

                _nes.Cpu.IrqLine = true;
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
                    if (!_nes.Cpu.IrqLine)
                        FrameInterruptSetThisCycle = true;

                    _nes.Cpu.IrqLine = true;
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

            return MixChannels(pulse1Output, pulse2Output, triangleOutput, noiseOutput, 0);
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
    }
}
