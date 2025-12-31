using stasisEmulator.NesCore.SaveStates.ApuComponentStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.ApuComponents
{
    public class Sweep(PulseSequencer pulseSequencer)
    {
        private readonly PulseSequencer _pulseSequencer = pulseSequencer;

        public byte DividerPeriod;
        private byte Divider;

        public bool Enabled;
        public bool Reload;
        public bool Negate;
        public bool TwosComplement;

        public byte ShiftCount;

        private ushort TargetPeriod
        {
            get
            {
                short changeAmount = (short)(_pulseSequencer.TimerPeriod >> ShiftCount);
                if (Negate)
                {
                    if (TwosComplement)
                        changeAmount = (short)-changeAmount;
                    else
                        changeAmount = (short)(-changeAmount - 1);
                }
                return (ushort)Math.Max(_pulseSequencer.TimerPeriod + changeAmount, 0);
            }
        }

        public void Power()
        {
            DividerPeriod = 0;
            Enabled = false;
            Reload = false;
            Negate = false;
            ShiftCount = 0;
        }

        public void Clock()
        {
            if (Divider == 0 && Enabled && ShiftCount > 0 && !Mute())
                _pulseSequencer.TimerPeriod = (ushort)(0x7FF & TargetPeriod);

            if (Divider == 0 || Reload)
            {
                Reload = false;
                Divider = DividerPeriod;
            }
            else
            {
                Divider--;
            }
        }

        public bool Mute()
        {
            if (_pulseSequencer.TimerPeriod < 8)
                return true;

            if (TargetPeriod > 0x7FF)
                return true;

            return false;
        }

        public SweepState SaveState()
        {
            return new()
            {
                DividerPeriod = DividerPeriod,
                Divider = Divider,

                Enabled = Enabled,
                Reload = Reload,
                Negate = Negate,

                ShiftCount = ShiftCount,
            };
        }

        public void LoadState(SweepState state)
        {
            DividerPeriod = state.DividerPeriod;
            Divider = state.Divider;

            Enabled = state.Enabled;
            Reload = state.Reload;
            Negate = state.Negate;

            ShiftCount = state.ShiftCount;
        }
    }
}
