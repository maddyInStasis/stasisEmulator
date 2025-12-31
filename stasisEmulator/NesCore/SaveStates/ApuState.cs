using stasisEmulator.NesCore.SaveStates.ApuComponentStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.SaveStates
{
    public class ApuState
    {
        public EnvelopeState Pulse1EnvelopeState;
        public SweepState Pulse1SweepState;
        public PulseSequencerState Pulse1SequencerState;
        public LengthCounterState Pulse1LengthCounterState;

        public EnvelopeState Pulse2EnvelopeState;
        public SweepState Pulse2SweepState;
        public PulseSequencerState Pulse2SequencerState;
        public LengthCounterState Pulse2LengthCounterState;

        public TriangleSequencerState TriangleSequencerState;
        public LinearCounterState TriangleLinearCounterState;
        public LengthCounterState TriangleLengthCounterState;

        public EnvelopeState NoiseEnvelopeState;
        public NoiseShiftRegisterState NoiseShiftRegisterState;
        public LengthCounterState NoiseLengthCounterState;

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
        public int _frameCounterResetTimer = -1;

        public bool _frameInterrupt = false;
        public bool _dmcInterruptFlag = false;

        public bool FrameInterruptSetThisCycle;
    }
}
