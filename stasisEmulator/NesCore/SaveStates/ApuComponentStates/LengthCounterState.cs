using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.SaveStates.ApuComponentStates
{
    public class LengthCounterState
    {
        public byte Length = 0;
        public bool Halt;

        public bool _enabled;
    }
}
