namespace stasisEmulator.NesCore.SaveStates.MapperStates
{
    public class Mmc1State : MapperState
    {
        public byte _shiftRegister;

        public bool _prgRamEnabled;

        public byte _prgRomBankMode;
        public bool _chrRom4KiBMode;

        public byte _prgBank;
        public byte _chrBank0;
        public byte _chrBank1;
    }
}
