using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole
{
    public class Apu(Nes nes)
    {
        private readonly Nes _nes = nes;

        public void Power()
        {
            Reset();
        }

        public void Reset()
        {

        }
    }
}
