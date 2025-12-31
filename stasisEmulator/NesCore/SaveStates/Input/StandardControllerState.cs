namespace stasisEmulator.NesCore.SaveStates.Input
{
    public class StandardControllerState : InputDeviceState
    {
        public bool _strobe = false;
        public byte _shiftRegister = 0;

        public ulong _lastCycle;
    }
}
