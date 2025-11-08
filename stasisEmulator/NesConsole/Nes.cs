#undef COMPONENT_TIME

using Microsoft.Xna.Framework.Input;
using stasisEmulator.NesConsole.Mappers;
using stasisEmulator.NesConsole.Input;
using System.Diagnostics;

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

        public readonly Stopwatch CpuWatch = new();
        public readonly Stopwatch PpuWatch = new();
        public readonly Stopwatch ApuWatch = new();

        private readonly Stopwatch FrameWatch = new();
        public double FrameElapsedTime
        {
            get
            {
#if COMPONENT_TIME
                return (CpuWatch.Elapsed + PpuWatch.Elapsed + ApuWatch.Elapsed).TotalMilliseconds;
#else
                return FrameWatch.Elapsed.TotalMilliseconds;
#endif
            }
        }

        public Mapper Cartridge { get; set; }

        public InputDevice Player1Controller { get; set; } = new StandardController();
        public InputDevice Player2Controller { get; set; }

        //warning: will overflow after 9 billion years
        public ulong FrameCount { get; private set; }
        public bool Paused { get; set; }

        private readonly InputBindingContext<EmulatorControl> _emulatorControls = new(new(){
            { EmulatorControl.Pause, new([Keys.Escape], null, null) },
            { EmulatorControl.Modifier, new([Keys.LeftControl], null, null) },
            { EmulatorControl.Reset, new([Keys.R], null, null) }
        });

        public Nes()
        {
            Cpu = new(this);
            Ppu = new(this);
            Apu = new(this);
            Power();
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

            Apu.ApuPreFrame();
            
            FrameWatch.Start();
            while (!Ppu.FrameComplete)
            {
                if (Paused)
                    break;
                
                //using directives because checking a bool every cycle would be pretty slow
                //and i can't be bothered to make a second version of this loop
                //idk why they keep force aligning to the left though, looks stupid
#if COMPONENT_TIME
                PpuWatch.Start();
#endif

                Ppu.RunCycle();
                Ppu.RunCycle();
                Ppu.RunCycle();

#if COMPONENT_TIME
                PpuWatch.Stop();
                CpuWatch.Start();
#endif
                Cpu.RunCycle();

#if COMPONENT_TIME
                CpuWatch.Stop();
                ApuWatch.Start();
#endif

                Apu.CpuClock();
                if (Cpu.CurrentCycleType == Cpu.CycleType.Put)
                    Apu.RunCycle();

#if COMPONENT_TIME
                ApuWatch.Stop();
#endif
            }
            FrameWatch.Stop();

            Apu.ApuPostFrame();

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
