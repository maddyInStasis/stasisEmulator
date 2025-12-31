namespace stasisEmulator.NesCore.SaveStates.ApuComponentStates
{
    public class SweepState
    {
        public byte DividerPeriod;
        public byte Divider;

        public bool Enabled;
        public bool Reload;
        public bool Negate;

        public byte ShiftCount;
    }
}
