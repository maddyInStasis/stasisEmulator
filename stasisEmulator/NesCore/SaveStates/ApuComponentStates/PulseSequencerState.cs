namespace stasisEmulator.NesCore.SaveStates.ApuComponentStates
{
    public class PulseSequencerState
    {
        public ushort TimerPeriod;
        public ushort Timer;

        public byte DutyCycle;
        public byte SequencerPosition;
    }
}
