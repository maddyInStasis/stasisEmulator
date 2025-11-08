using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.ApuComponents
{
    public class LinearCounter
    {
        public byte Counter;
        public byte CounterReloadValue;

        public bool ControlFlag;
        public bool CounterReloadFlag;

        public void Power()
        {
            CounterReloadValue = 0;
            CounterReloadFlag = false;
            Counter = 0;
            ControlFlag = false;
        }

        public void Clock()
        {
            if (CounterReloadFlag)
                Counter = CounterReloadValue;
            else if (Counter > 0)
                Counter--;

            if (!ControlFlag)
                CounterReloadFlag = false;
        }

        public bool GateOutput()
        {
            return Counter > 0;
        }
    }
}
