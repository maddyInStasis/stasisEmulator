using stasisEmulator.NesCore.SaveStates.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.Input
{
    public abstract class InputDevice(Nes nes, int playerIndex)
    {
        protected readonly Nes _nes = nes;
        public int PlayerIndex = playerIndex;

        public abstract void RegisterWrite(byte value);
        public abstract void RegisterRead(ref byte dataBus);

        public abstract InputDeviceState SaveState();
        public abstract void LoadState(InputDeviceState state);
    }
}
