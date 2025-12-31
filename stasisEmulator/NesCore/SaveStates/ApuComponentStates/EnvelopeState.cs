namespace stasisEmulator.NesCore.SaveStates.ApuComponentStates
{
    public class EnvelopeState
    {
        public bool Start;

        public byte EnvelopeParameter;
        public byte Divider;

        public byte DecayLevel;

        public bool Loop;
        public bool ConstantVolume;
    }
}
