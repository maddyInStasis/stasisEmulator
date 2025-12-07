using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace stasisEmulator
{
    public enum ConsoleType
    {
        Nes,
    }

    public interface IEmulatorCore
    {
        ConsoleType ConsoleType { get; }
        
        Dictionary<string, Action> DebugOptions { get; }

        int ViewportWidth { get; }
        int ViewportHeight { get; }
        Color[] OutputBuffer { get; }

        double TotalFrameTime { get; }
        ulong FrameCount { get; }

        void LoadRom(string path);

        void RunFrame();
        void Power();
        void Reset();

        void LoadTas(string path);
        void RestartTas();
        void StopTas();

        void Unload();
    }
}
