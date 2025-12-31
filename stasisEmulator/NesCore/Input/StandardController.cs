using Microsoft.Xna.Framework.Input;
using stasisEmulator.Input;
using stasisEmulator.NesCore.SaveStates.Input;

namespace stasisEmulator.NesCore.Input
{
    public class StandardController(Nes nes, int playerIndex) : InputDevice(nes, playerIndex)
    {
        private enum NesButton
        {
            A,
            B,
            Select,
            Start,
            Up,
            Down,
            Left,
            Right
        }

        private readonly InputBindingContext<NesButton> _inputs = new(bindings: new()
        {
            { NesButton.A, new([Keys.S]) },
            { NesButton.B, new([Keys.A]) },
            { NesButton.Select, new([Keys.Q]) },
            { NesButton.Start, new([Keys.W]) },
            { NesButton.Up, new([Keys.Up]) },
            { NesButton.Down, new([Keys.Down]) },
            { NesButton.Left, new([Keys.Left]) },
            { NesButton.Right, new([Keys.Right]) }
        });

        bool _strobe = false;
        byte _shiftRegister = 0;

        public override void RegisterWrite(byte value)
        {
            _strobe = (value & 1) == 1;
            if (_strobe)
                FillShiftRegister();
        }

        private ulong _lastCycle;
        public override void RegisterRead(ref byte dataBus)
        {
            if (_strobe)
                FillShiftRegister();

            if (_nes.Cpu.CycleCount == _lastCycle + 1)
            {
                _lastCycle = _nes.Cpu.CycleCount;
                return;
            }

            dataBus = (byte)((dataBus & (0xFF << 3)) | (_shiftRegister & 1));

            _shiftRegister >>= 1;
            _shiftRegister |= 0x80;

            _lastCycle = _nes.Cpu.CycleCount; 
        }

        private void FillShiftRegister()
        {
            _inputs.UpdateInputStates();
            _shiftRegister = 0;

            for (int i = 0; i < 8; i++)
            {
                NesButton button = (NesButton)i;
                _shiftRegister |= (byte)((_inputs.IsBindPressed(button) ? 1 : 0) << i);
            }
        }

        public override InputDeviceState SaveState()
        {
            return new StandardControllerState()
            {
                _strobe = _strobe,
                _shiftRegister = _shiftRegister,

                _lastCycle = _lastCycle,
            };
        }

        public override void LoadState(InputDeviceState state)
        {
            if (state is not StandardControllerState)
                return;

            var controllerState = state as StandardControllerState;

            _strobe = controllerState._strobe;
            _shiftRegister = controllerState._shiftRegister;

            _lastCycle = controllerState._lastCycle;
        }
    }
}
