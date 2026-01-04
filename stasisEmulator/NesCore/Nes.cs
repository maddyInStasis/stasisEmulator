using Microsoft.Xna.Framework.Input;
using stasisEmulator.NesCore.Mappers;
using stasisEmulator.NesCore.Input;
using System.Diagnostics;
using stasisEmulator.Input;
using System;
using System.Collections.Generic;
using stasisEmulator.UI.Windows;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using stasisEmulator.NesCore.SaveStates;

namespace stasisEmulator.NesCore
{
    public class Nes : IEmulatorCore
    {
        public enum AdvanceType
        {
            Instructions,
            Cycles,
            VBlank
        }

        public ConsoleType ConsoleType { get => ConsoleType.Nes; }

        public Dictionary<string, Action> DebugOptions { get => _debugOptions; }
        private readonly Dictionary<string, Action> _debugOptions = [];

        private readonly GraphicsDevice _graphicsDevice;

        private TraceLogWindow _traceLogger;
        private MemoryViewerWindow _memoryViewerWindow;
        private PatternViewerWindow _patternViewerWindow;
        private NametableViewerWindow _nametableViewerWindow;

        public int MasterClock;
        public int CpuClock;
        public int PpuClock;
        public int ApuClock;

        public readonly Cpu Cpu;
        public readonly Ppu Ppu;
        public readonly Apu Apu;

        public AudioOutputManager AudioOutput { get => Apu.AudioOutput; }

        public int ViewportWidth { get => 256; }
        public int ViewportHeight { get => 240; }
        public Color[] OutputBuffer { get => Ppu.OutputBuffer; }

        private readonly Stopwatch FrameWatch = new();
        public double TotalFrameTime { get => FrameWatch.Elapsed.TotalMilliseconds; }

        public Mapper Mapper { get; set; }

        public InputDevice Player1Controller { get; set; }
        public InputDevice Player2Controller { get; set; }

        //warning: will overflow after 9 billion years
        public ulong FrameCount { get; private set; }
        public bool Paused { get; set; } = false;

        private bool _advance = false;
        private AdvanceType _advanceType;
        private ulong _advanceTarget = 0;
        private bool _prevVblank = false;

        public Nes(GraphicsDevice graphicsDevice)
        {
            Cpu = new(this);
            Ppu = new(this);
            Apu = new(this);
            Player1Controller = new StandardController(this, 0);

            _graphicsDevice = graphicsDevice;
            _debugOptions.Add("Trace Logger", new(ShowTraceLogger));
            _debugOptions.Add("Memory Viewer", new(ShowMemoryViewer));
            _debugOptions.Add("Pattern Viewer", new(ShowPatternViewer));
            _debugOptions.Add("Nametable Viewer", new(ShowNametableViewer));

            Power();
        }

        public void RunFrame()
        {
            if (Paused && !_advance)
                return;

            Apu.ApuPreFrame();
            
            FrameWatch.Start();
            while (!Ppu.FrameComplete)
            {
                if (Paused && !_advance)
                    break;

                Clock();

                if (_advance)
                {
                    if (_advanceType switch
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

                    if (_advanceType == AdvanceType.VBlank && !_prevVblank && Ppu.VBlank)
                    {
                        _advance = false;
                        Paused = true;
                    }
                }
                _prevVblank = Ppu.VBlank;
            }
            FrameWatch.Stop();

            Apu.ApuPostFrame();

            Ppu.FrameComplete = false;
            FrameCount++;
        }

        private void Clock()
        {
            //this loop is just how tricnes does it. i was trying to get nmi timing tests to work and was just trying stuff
            //TODO: uhhhhhhhhhhh idk but this just feels like stealing

            MasterClock++;
            if (MasterClock == 24)
            {
                MasterClock = 0;
            }

            if (CpuClock == 0)
            {
                Cpu.RunCycle();
                CpuClock = 12;
            }
            if (CpuClock == 8)
            {
                Cpu.NmiLine = Ppu.EnableNMI && Ppu.VBlankDelayed;
            }
            if (PpuClock == 0)
            {
                Ppu.RunCycle();
                PpuClock = 4;
            }
            if (CpuClock == 5)
            {
                Cpu.IrqLevel = Cpu.IrqLine > 0;
            }
            if (ApuClock == 0)
            {
                Apu.PutCycle = !Apu.PutCycle;
                Apu.CpuClock();
                if (Apu.PutCycle)
                    Apu.RunCycle();

                ApuClock = 12;
            }

            CpuClock--;
            PpuClock--;
            ApuClock--;
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

        public void TogglePause()
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

        public void LoadRom(string path)
        {
            var rom = RomLoader.LoadRom(path);
            var mapper = MapperFactory.CreateMapper(rom, this);
            Mapper = mapper;
            Power();
        }

        private void ShowTraceLogger()
        {
            if (_traceLogger != null)
            {
                _traceLogger.Focus();
                return;
            }

            _traceLogger = new(this, _graphicsDevice, 1280, 720);
            _traceLogger.WindowClosed += (sender, e) => { _traceLogger = null; };
        }
        private void ShowMemoryViewer()
        {
            if (_memoryViewerWindow != null)
            {
                _memoryViewerWindow.Focus();
                return;
            }

            _memoryViewerWindow = new(this, _graphicsDevice, 640, 720);
            _memoryViewerWindow.WindowClosed += (sender, e) => { _memoryViewerWindow = null; };
        }
        private void ShowPatternViewer()
        {
            if (_patternViewerWindow != null)
            {
                _patternViewerWindow.Focus();
                return;
            }

            _patternViewerWindow = new(this, _graphicsDevice, 1280, 720);
            _patternViewerWindow.WindowClosed += (sender, e) => { _patternViewerWindow = null; };
        }
        private void ShowNametableViewer()
        {
            if (_nametableViewerWindow != null)
            {
                _nametableViewerWindow.Focus();
                return;
            }

            _nametableViewerWindow = new(this, _graphicsDevice, 1080, 1080);
            _nametableViewerWindow.WindowClosed += (sender, e) => { _nametableViewerWindow = null; };
        }

        public void Unload()
        {
            Apu.IsRunning = false;
        }

        public SaveState SaveState()
        {
            NesState state = new()
            {
                MasterClock = MasterClock,
                CpuClock = CpuClock,
                PpuClock = PpuClock,
                ApuClock = ApuClock,

                CpuState = Cpu.SaveState(),
                PpuState = Ppu.SaveState(),
                ApuState = Apu.SaveState(),

                MapperState = Mapper?.SaveState(),

                Player1ControllerState = Player1Controller?.SaveState(),
                Player2ControllerState = Player2Controller?.SaveState(),
            };

            return state;
        }

        public void LoadState(SaveState state)
        {
            if (state is not NesState)
                return;

            var nesState = state as NesState;

            MasterClock = nesState.MasterClock;
            CpuClock = nesState.CpuClock;
            PpuClock = nesState.PpuClock;
            ApuClock = nesState.ApuClock;

            Cpu.LoadState(nesState.CpuState);
            Ppu.LoadState(nesState.PpuState);
            Apu.LoadState(nesState.ApuState);

            Mapper?.LoadState(nesState.MapperState);

            Player1Controller?.LoadState(nesState.Player1ControllerState);
            Player2Controller?.LoadState(nesState.Player2ControllerState);

            _advance = false;
        }
    }
}
