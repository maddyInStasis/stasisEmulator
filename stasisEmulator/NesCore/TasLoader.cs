using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace stasisEmulator.NesCore
{
    public enum ControllerType
    {
        None,
        Standard,
        Zapper
    }

    public struct InputFrame
    {
        public bool Reset = false;
        public bool Power = false;

        public byte P0Inputs = 0;
        public byte P1Inputs = 0;

        public InputFrame() { }
    }

    public class Tas
    {
        public ControllerType P0Type;
        public ControllerType P1Type;

        public int FrameNumber;
        public bool Running;
        public bool Started;
        public bool Completed { get => FrameNumber >= Frames.Count; }

        public readonly List<InputFrame> Frames = [];
        public InputFrame CurrentFrame { get => FrameNumber < Frames.Count ? Frames[FrameNumber] : new(); }

        public void Restart() { Running = true; Started = false; FrameNumber = 0; }
        public void Stop() { Running = false; FrameNumber = 0; }
    }

    public static class TasLoader
    {
        private static readonly HashSet<int> SupportedFm2Versions = [3];

        private const string GamepadInputs = "RLDUTSBA";

        public static Tas LoadTas(string path)
        {
            StreamReader reader;

            try
            {
                reader = new(path);
            }
            catch
            {
                throw new FileNotFoundException(path);
            }

            Tas tas = new();

            string line = reader.ReadLine();
            string[] tokens = line.Split(' ');
            string key = tokens[0];
            string value = tokens[1];

            if (key != "version")
                throw new Exception("The first line of an fm2 file must be the version.");
            if (!int.TryParse(value, out int version))
                throw new Exception("Invalid version number.");
            if (!SupportedFm2Versions.Contains(version))
                throw new Exception($"Unsupported FM2 version: {version}");

            HashSet<string> remainingRequiredKeys = ["emuVersion", "port0", "port1", "port2", "romFilename", "guid", "romChecksum"];
            while (true)
            {
                int peek = reader.Peek();
                if (peek == -1)
                    break;

                string next = char.ConvertFromUtf32(peek);
                if (next == "|")
                    break;

                line = reader.ReadLine();
                tokens = line.Split(' ');

                if (tokens.Length < 2)
                    continue;

                key = tokens[0];
                value = tokens[1];

                switch (key)
                {
                    case "emuVersion":
                        break;
                    case "rerecordCount":
                        break;
                    case "palFlag":
                        if (!int.TryParse(value, out int isPal))
                            break;

                        if (isPal != 0)
                            throw new Exception("Pal not supported.");
                        break;

                    case "NewPPU":
                        break;
                    case "FDS":
                        if (!int.TryParse(value, out int isFds))
                            break;

                        if (isFds != 0)
                            throw new Exception("Famicom Disk System not supported.");

                        break;
                    case "fourscore":
                        if (!int.TryParse(value, out int hasFourscore))
                            break;

                        if (hasFourscore == 0)
                            break;

                        remainingRequiredKeys.Remove("port0");
                        remainingRequiredKeys.Remove("port1");

                        throw new Exception("Fourscore not supported.");
                    case "port0":
                        if (!int.TryParse(value, out int p0))
                            break;

                        if (p0 == 2)
                            throw new Exception("Zapper not supported.");

                        tas.P0Type = p0 switch
                        {
                            1 => ControllerType.Standard,
                            2 => ControllerType.Zapper,
                            _ => ControllerType.None
                        };

                        break;
                    case "port1":
                        if (!int.TryParse(value, out int p1))
                            break;

                        if (p1 == 2)
                            throw new Exception("Zapper not supported.");

                        tas.P1Type = p1 switch
                        {
                            1 => ControllerType.Standard,
                            2 => ControllerType.Zapper,
                            _ => ControllerType.None
                        };

                        break;
                    case "port2":
                        break;
                    case "binary":
                        if (!int.TryParse(value, out int isBinary))
                            break;

                        if (isBinary != 0)
                            throw new Exception("Binary format not supported.");

                        break;
                    case "length":
                        break;
                    case "romFilename":
                        break;
                    case "comment":
                        break;
                    case "subtitle":
                        break;
                    case "guid":
                        break;
                    case "romChecksum":
                        break;
                    case "savestate":
                        throw new Exception("Savestates not supported.");
                }

                remainingRequiredKeys.Remove(key);
            }

            if (remainingRequiredKeys.Count > 0)
            {
                string missingKeys = "";
                foreach (string missingKey in remainingRequiredKeys)
                {
                    if (missingKeys.Length > 0)
                        missingKeys += ", ";

                    missingKeys += missingKey;
                }

                throw new Exception($"Tas file missing required keys: {missingKeys}");
            }

            int expectedTokenCount = 1;
            if (tas.P0Type != ControllerType.None)
                expectedTokenCount++;
            if (tas.P1Type != ControllerType.None)
                expectedTokenCount++;

            while (true)
            {
                int peek = reader.Peek();
                if (peek == -1)
                    break;

                line = reader.ReadLine();
                tokens = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < expectedTokenCount)
                    continue;

                InputFrame frame = new();

                if (!byte.TryParse(tokens[0], out byte commands))
                    commands = 0;

                if ((commands & 1) != 0)
                    frame.Reset = true;
                if ((commands & 2) != 0)
                    frame.Power = true;

                if (tas.P0Type != ControllerType.None)
                {
                    string inputs = tokens[1];
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        char c = inputs[i];
                        if (c != GamepadInputs[i])
                            continue;

                        frame.P0Inputs |= (byte)(1 << (7 - i));
                    }
                }

                if (tas.P1Type != ControllerType.None)
                {
                    string inputs = tokens[2];
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        char c = inputs[i];
                        if (c != GamepadInputs[i])
                            continue;

                        frame.P1Inputs |= (byte)(1 << (7 - i));
                    }
                }

                tas.Frames.Add(frame);
            }

            return tas;
        }
    }
}
