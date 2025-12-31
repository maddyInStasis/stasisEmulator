using stasisEmulator.NesCore.SaveStates.ApuComponentStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.ApuComponents
{
    public class LengthCounter
    {
        private byte Length = 0;
        public bool Halt;

        private bool _enabled;
        public bool Enabled 
        { 
            get => _enabled;
            set
            {
                _enabled = value;
                if (!_enabled)
                    Length = 0;
            }
        }

        private readonly byte[] Lengths = [
            10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
            12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
        ];

        public void Power()
        {
            Enabled = false;
            Halt = false;
        }

        public void Load(byte index)
        {
            if (!Enabled)
                return;

            Length = Lengths[index];
        }

        public void Clock()
        {
            if (Length == 0)
                return;
            if (Halt)
                return;

            Length--;
        }

        public bool GateOutput()
        {
            return Length != 0;
        }

        public LengthCounterState SaveState()
        {
            return new()
            {
                Length = Length,
                Halt = Halt,
                _enabled = _enabled,
            };
        }

        public void LoadState(LengthCounterState state)
        {
            Length = state.Length;
            Halt = state.Halt;
            _enabled = state._enabled;
        }
    }
}
