using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.ApuComponents
{
    public class PulseSequencer
    {
        public ushort TimerPeriod;
        private ushort Timer;

        public byte DutyCycle;

        private readonly byte[] Sequences = [
            0b00000001,
            0b00000011,
            0b00001111,
            0b11111100
        ];

        public byte SequencerPosition;

        public void Power()
        {
            TimerPeriod = 0;
            DutyCycle = 0;
            SequencerPosition = 0;
        }

        public void Clock()
        {
            if (Timer == 0)
            {
                Timer = TimerPeriod;
                SequencerPosition--;
                SequencerPosition &= 7;
            }
            else
            {
                Timer--;
            }
        }

        public bool GateOutput()
        {
            return ((Sequences[DutyCycle] << SequencerPosition) & 0x80) != 0;
        }
    }
}
