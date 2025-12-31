using stasisEmulator.NesCore.SaveStates.ApuComponentStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.ApuComponents
{
    public class NoiseShiftRegister
    {
        public bool Mode;

        private ushort Period;
        private ushort Timer;

        private readonly ushort[] TimerPeriods = [
            4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
        ];

        private ushort ShiftRegister;

        public void Power()
        {
            ShiftRegister = 0;
            Mode = false;
            Period = 0;
            Timer = 0;
        }

        public void LoadPeriod(byte index)
        {
            Period = (ushort)(TimerPeriods[index] / 2);
        }

        public void Clock()
        {
            if (Timer == 0)
            {
                if (ShiftRegister == 0)
                    ShiftRegister = 1;

                Timer = Period;
                bool feedback = ((ShiftRegister & 1) ^ ((ShiftRegister >> (Mode ? 6 : 1)) & 1)) != 0;
                ShiftRegister >>= 1;
                ShiftRegister |= (ushort)(feedback ? 0x4000 : 0);
            }
            else
            {
                Timer--;
            }
        }

        public bool GateOutput()
        {
            return (ShiftRegister & 1) != 0;
        }

        public NoiseShiftRegisterState SaveState()
        {
            return new()
            {
                Mode = Mode,

                Period = Period,
                Timer = Timer,

                ShiftRegister = ShiftRegister,
            };
        }

        public void LoadState(NoiseShiftRegisterState state)
        {
            Mode = state.Mode;

            Period = state.Period;
            Timer = state.Timer;

            ShiftRegister = state.ShiftRegister;
        }
    }
}
