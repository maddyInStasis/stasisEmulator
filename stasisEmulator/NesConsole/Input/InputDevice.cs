using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole.Input
{
    public abstract class InputDevice(Nes nes)
    {
        protected readonly Nes _nes = nes;

        public abstract void RegisterWrite(byte value);
        public abstract void RegisterRead(ref byte dataBus);
    }
}
