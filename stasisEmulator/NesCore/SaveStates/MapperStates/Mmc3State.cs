namespace stasisEmulator.NesCore.SaveStates.MapperStates
{
    public class Mmc3State : MapperState
    {
        public bool _prgBankMode;
        public bool _chrBanksInverted;

        public byte[] _banks = new byte[8];

        public bool _prgRamEnabled;
        public bool _prgRamWriteProtect;

        public byte _nextBankWrite;

        public bool _a12Set;
        public byte _irqReload;
        public byte _irqCounter;
        public bool _irqReloadRequest;
        public bool _irqEnabled;

        public bool _irqFlag;
    }
}
