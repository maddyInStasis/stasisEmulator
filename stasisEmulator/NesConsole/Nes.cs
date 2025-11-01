using Microsoft.Xna.Framework.Input;
using stasisEmulator.NesConsole.Cartridges;
using stasisEmulator.NesConsole.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.NesConsole
{
    public class Nes
    {
        private enum EmulatorControl
        {
            Pause,
            Modifier,
            Reset
        }

        public readonly Cpu Cpu;
        public readonly Apu Apu;
        public readonly Ppu Ppu;

        public Cartridge Cartridge { get; set; }

        public InputDevice Player1Controller { get; set; } = new StandardController();
        public InputDevice Player2Controller { get; set; }

        private const int _masterClockSpeed = 236250000 / 11;
        private const int _cpuCyclesPerSecond = _masterClockSpeed / 12;

        public int FrameCount { get; private set; }
        public bool Paused { get; set; }

        private InputBindingContext<EmulatorControl> _emulatorControls = new(new(){
            { EmulatorControl.Pause, new([Keys.Escape], null, null) },
            { EmulatorControl.Modifier, new([Keys.LeftControl], null, null) },
            { EmulatorControl.Reset, new([Keys.R], null, null) }
        });

        public Nes()
        {
            Cpu = new(this);
            Ppu = new(this);
            Apu = new(this);
            Cpu.Power();
            Ppu.Power();
            Apu.Power();
        }

        public void RunFrame()
        {
            _emulatorControls.UpdateInputStates();

            if (_emulatorControls.WasBindJustPressed(EmulatorControl.Pause))
                Paused = !Paused;

            if (_emulatorControls.IsBindPressed(EmulatorControl.Modifier) && _emulatorControls.WasBindJustPressed(EmulatorControl.Reset))
                Reset();

            if (Paused)
                return;

            while (!Ppu.FrameComplete)
            {
                if (Paused)
                    break;

                Ppu.RunCycle();
                Ppu.RunCycle();
                Ppu.RunCycle();
                Cpu.RunCycle();
            }

            Ppu.FrameComplete = false;
            FrameCount++;
        }

        public void Power()
        {
            Cpu.Power();
            Ppu.Power();
            Apu.Power();
        }

        public void Reset()
        {
            Cpu.Reset();
            Ppu.Reset();
            Apu.Reset();
        }
    }
}
