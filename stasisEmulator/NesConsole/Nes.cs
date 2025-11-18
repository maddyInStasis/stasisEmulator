#undef COMPONENT_TIME

using Microsoft.Xna.Framework.Input;
using stasisEmulator.NesConsole.Mappers;
using stasisEmulator.NesConsole.Input;
using System.Diagnostics;
using stasisEmulator.Input;
using System;

namespace stasisEmulator.NesConsole
{
    public class Nes
    {
        public enum AdvanceType
        {
            Instructions,
            Cycles,
            VBlank
        }

        private enum EmulatorControl
        {
            Pause,
            Modifier,
            Reset,
            InstructionAdvance
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

        public InputDevice Player1Controller { get; set; }
        public InputDevice Player2Controller { get; set; }

        //warning: will overflow after 9 billion years
        public ulong FrameCount { get; private set; }
        public bool Paused { get; set; } = false;

        private bool _advance = false;
        private AdvanceType _advanceType;
        private ulong _advanceTarget = 0;
        private bool _prevVblank = false;

        private readonly InputBindingContext<EmulatorControl> _emulatorControls = new(bindings: new(){
            { EmulatorControl.Pause, new([Keys.Escape]) },
            { EmulatorControl.Modifier, new([Keys.LeftControl]) },
            { EmulatorControl.Reset, new([Keys.R]) },
            { EmulatorControl.InstructionAdvance, new([Keys.T])},
        }, null);

        public Nes()
        {
            Cpu = new(this);
            Ppu = new(this);
            Apu = new(this);
            Player1Controller = new StandardController(this);
            Power();
        }

        public void RunFrame()
        {
            _emulatorControls.UpdateInputStates();

            if (_emulatorControls.WasBindJustPressed(EmulatorControl.Pause))
            {
                if (_advance)
                {
                    _advance = false;
                    Paused = true;
                }
                else
                {
                    Paused = !Paused;
                }
            }

            if (_emulatorControls.IsBindPressed(EmulatorControl.Modifier) && _emulatorControls.WasBindJustPressed(EmulatorControl.Reset))
                Reset();

            if (Paused && !_advance)
                return;

            Apu.ApuPreFrame();
            
            FrameWatch.Start();
            while (!Ppu.FrameComplete)
            {
                if (Paused && !_advance)
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
                if (_advance && _advanceType switch 
                { 
                    AdvanceType.Instructions => Cpu.InstructionCount,
                    AdvanceType.Cycles => Cpu.CycleCount,
                    AdvanceType.VBlank => 0,
                    _ => throw new Exception($"Advance type not implemented: {_advanceType}")
                } >= _advanceTarget)
                {
                    _advance = false;
                    Paused = true;
                }

                if (_advance && _advanceType == AdvanceType.VBlank && !_prevVblank && Ppu.VBlank)
                {
                    _advance = false;
                    Paused = true;
                }
                _prevVblank = Ppu.VBlank;
            }
            FrameWatch.Stop();

            Apu.ApuPostFrame();

            Ppu.FrameComplete = false;
            FrameCount++;
        }

        public void Advance(AdvanceType advanceType, ulong count = 0)
        {
            _advance = true;
            _advanceType = advanceType;
            _advanceTarget = advanceType switch
            {
                AdvanceType.Instructions => Cpu.InstructionCount + count,
                AdvanceType.Cycles => Cpu.CycleCount + count,
                AdvanceType.VBlank => ulong.MaxValue,
                _ => throw new Exception($"Advance type not implemented: {advanceType}")
            };
            _prevVblank = Ppu.VBlank;
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
