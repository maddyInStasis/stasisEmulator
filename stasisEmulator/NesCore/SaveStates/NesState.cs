using stasisEmulator.NesCore.SaveStates.Input;
using stasisEmulator.NesCore.SaveStates.MapperStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesCore.SaveStates
{
    public class NesState : SaveState
    {
        public int MasterClock;
        public int CpuClock;
        public int PpuClock;
        public int ApuClock;

        public CpuState CpuState;
        public PpuState PpuState;
        public ApuState ApuState;

        public MapperState MapperState;

        public InputDeviceState Player1ControllerState;
        public InputDeviceState Player2ControllerState;
    }
}
