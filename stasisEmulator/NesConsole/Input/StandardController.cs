using Microsoft.Xna.Framework.Input;

namespace stasisEmulator.NesConsole.Input
{
    public class StandardController : InputDevice
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

        private readonly InputBindingContext<NesButton> _inputs = new(new()
        {
            { NesButton.A, new([Keys.S], null, null) },
            { NesButton.B, new([Keys.A], null, null) },
            { NesButton.Select, new([Keys.Q], null, null) },
            { NesButton.Start, new([Keys.W], null, null) },
            { NesButton.Up, new([Keys.Up], null, null) },
            { NesButton.Down, new([Keys.Down], null, null) },
            { NesButton.Left, new([Keys.Left], null, null) },
            { NesButton.Right, new([Keys.Right], null, null) }
        });

        bool _strobe = false;
        byte _shiftRegister = 0;

        public override void RegisterWrite(byte value)
        {
            _strobe = (value & 1) == 1;
            if (_strobe)
                FillShiftRegister();
        }

        public override void RegisterRead(ref byte dataBus)
        {
            if (_strobe)
                FillShiftRegister();

            dataBus = (byte)((dataBus & (0xFF << 3)) | (_shiftRegister & 1));

            _shiftRegister >>= 1;
            _shiftRegister |= 0x80;
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
    }
}
