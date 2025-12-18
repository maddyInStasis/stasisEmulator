using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.ApuComponents
{
    public class TriangleSequencer(LengthCounter lengthCounter, LinearCounter linearCounter)
    {
        private readonly LengthCounter _lengthCounter = lengthCounter;
        private readonly LinearCounter _linearCounter = linearCounter;

        public ushort TimerPeriod;
        private ushort Timer;

        public byte SequencerPosition;
        private readonly byte[] TriangleSequence = [
            15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
        ];

        public bool SilenceUltrasonicFrequencies = true;

        public void Power()
        {
            TimerPeriod = 0;
            Timer = 0;
        }

        public void Clock()
        {
            if (Timer == 0)
            {
                Timer = TimerPeriod;
                if (_linearCounter.GateOutput() && _lengthCounter.GateOutput())
                {
                    SequencerPosition++;
                    SequencerPosition &= 31;
                }
            }
            else
            {
                Timer--;
            }
        }

        public byte GetOutputValue()
        {
            if (TimerPeriod < 2 && SilenceUltrasonicFrequencies)
                return 0;

            return TriangleSequence[SequencerPosition];
        }
    }
}
