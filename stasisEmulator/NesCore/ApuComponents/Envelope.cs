using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.ApuComponents
{
    public class Envelope
    {
        public bool Start;

        public byte EnvelopeParameter;
        private byte Divider;

        public byte DecayLevel;

        public bool Loop;
        public bool ConstantVolume;

        public void Power()
        {
            ConstantVolume = false;
            Loop = false;
            EnvelopeParameter = 0;
            Start = false;
        }

        public void Clock()
        {
            if (!Start)
            {
                if (Divider == 0)
                {
                    Divider = EnvelopeParameter;

                    if (DecayLevel > 0)
                        DecayLevel--;
                    else if (Loop)
                        DecayLevel = 15;
                }
                else
                {
                    Divider--;
                }
            }
            else
            {
                Start = false;
                DecayLevel = 15;
                Divider = EnvelopeParameter;
            }
        }

        public byte GetOutputVolume()
        {
            if (ConstantVolume)
                return EnvelopeParameter;
            else
                return DecayLevel;
        }
    }
}
