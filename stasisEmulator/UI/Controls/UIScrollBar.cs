using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    public enum ScrollBarDirection
    {
        Horizontal,
        Vertical
    }

    public class UIScrollBar : UIControl
    {
        public UIScrollBar() { Init(); }
        public UIScrollBar(UIControl parent) : base(parent) { Init(); }

        private ScrollBarDirection _direction = ScrollBarDirection.Vertical;
        public ScrollBarDirection Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                FillDirectionLocked = false;
                FillDirection = _direction == ScrollBarDirection.Horizontal ? FillDirection.LeftToRight : FillDirection.TopToBottom;
                FillDirectionLocked = true;
                UpdateSize();
            }
        }

        private int _contentTotalSize = 0;
        public int ContentTotalSize 
        { 
            get => _contentTotalSize; 
            set
            {
                _contentTotalSize = value;
                UpdateSliderContentSize();
            }
        }
        private int _contentVisibleSize = 0;
        public int ContentVisibleSize 
        { 
            get => _contentVisibleSize; 
            set
            {
                _contentVisibleSize = value;
                UpdateSliderContentSize();
            }
        }

        public float Value { get => _slider.Value; set => _slider.Value = value; }
        public SliderRange Range { get => _slider.Range; }

        private const int ScrollClickAmount = 100;
        private const int ScrollHoldAmountPerSecond = 500;
        private const float ScrollHoldTimeThreshold = 0.25f;

        private int _thickness = 20;
        public int Thickness 
        { 
            get => _thickness; 
            set
            {
                _thickness = value;
                UpdateSize();
            }
        }
        private UISize _length = UISize.Grow();
        public UISize Length 
        {
            get => _length;
            set
            {
                _length = value;
                UpdateSize();
            }
        }

        public Color TrackColor 
        { 
            get => _slider.TrackActiveColor;
            set
            {
                _slider.TrackActiveColor = value;
                _slider.TrackInactiveColor = value;
            }
        }

        public Color ThumbIdleColor { get => _slider.ThumbIdleColor; set => _slider.ThumbIdleColor = value; }
        public Color ThumbHoverColor { get => _slider.ThumbHoverColor; set => _slider.ThumbHoverColor = value; }
        public Color ThumbDragColor { get => _slider.ThumbDragColor; set => _slider.ThumbDragColor = value; }

        public Color ButtonIdleColor 
        { 
            get => _buttonUp.IdleColor; 
            set
            {
                _buttonUp.IdleColor = value;
                _buttonDown.IdleColor = value;
            }
        }
        public Color ButtonHoverColor
        {
            get => _buttonUp.HoverColor;
            set
            {
                _buttonUp.HoverColor = value;
                _buttonDown.HoverColor = value;
            }
        }
        public Color ButtonPressColor
        {
            get => _buttonUp.PressColor;
            set
            {
                _buttonUp.PressColor = value;
                _buttonDown.PressColor = value;
            }
        }

        private bool _showButtons = true;
        public bool ShowButtons
        {
            get => _showButtons;
            set
            {
                _showButtons = value;
                UpdateSize();
            }
        }

        private UIButton _buttonUp;
        private float _buttonUpPressTimer;
        private UIButton _buttonDown;
        private float _buttonDownPressTimer;
        private UISlider _slider;

        //TODO: add button arrow icons

        private void Init()
        {
            _buttonDown = new();
            _buttonUp = new();
            _slider = new UISlider();

            _buttonUp.Parent = this;
            _slider.Parent = this;
            _buttonDown.Parent = this;

            _slider.TrackThickness = SliderSize.Relative(1);
            _slider.TrackExtents = SliderTrackExtents.ToEdge;

            TrackColor = Color.LightGray;
            ThumbIdleColor = Color.Gray;
            ThumbHoverColor = Color.DarkGray;
            ThumbDragColor = Color.DarkGray;

            ButtonIdleColor = Color.LightGray;
            ButtonHoverColor = Color.Gray;
            ButtonPressColor = Color.DarkGray;

            FillDirection = Direction == ScrollBarDirection.Horizontal ? FillDirection.LeftToRight : FillDirection.TopToBottom;

            ChildrenLocked = true;
            FillDirectionLocked = true;
            UpdateSize();
            UpdateSliderContentSize();
        }

        private void UpdateSize()
        {
            SizeLocked = false;

            _buttonUp.Enabled = _showButtons;
            _buttonDown.Enabled = _showButtons;

            if (ShowButtons)
            {
                _buttonUp.Width = UISize.Fixed(Thickness);
                _buttonUp.Height = UISize.Fixed(Thickness);
                _buttonDown.Width = UISize.Fixed(Thickness);
                _buttonDown.Height = UISize.Fixed(Thickness);
            }
            else
            {
                _buttonUp.Width = UISize.Fixed(0);
                _buttonUp.Height = UISize.Fixed(0);
                _buttonDown.Width = UISize.Fixed(0);
                _buttonDown.Height = UISize.Fixed(0);
            }

            if (Direction == ScrollBarDirection.Horizontal)
            {
                Width = Length;
                Height = UISize.Fixed(Thickness);
                _slider.Width = UISize.Grow();
                _slider.Height = UISize.Fixed(Thickness);
                _slider.SliderDirection = SliderDirection.LeftToRight;
            }
            else
            {
                Width = UISize.Fixed(Thickness);
                Height = Length;
                _slider.Width = UISize.Fixed(Thickness);
                _slider.Height = UISize.Grow();
                _slider.SliderDirection = SliderDirection.TopToBottom;
            }

            SizeLocked = true;
        }

        private void UpdateSliderContentSize()
        {
            if (ContentTotalSize <= ContentVisibleSize)
            {
                _slider.Enabled = false;
            }
            else
            {
                _slider.Enabled = true;
                _slider.ThumbSize = SliderSize.Relative((float)ContentVisibleSize / ContentTotalSize, min: Thickness / 2);
                _slider.Range = new SliderRange(0, ContentTotalSize - ContentVisibleSize);
            }
        }
        
        public void Scroll(int scrollAmount)
        {
            Value += scrollAmount;
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_buttonUp.MouseJustDown)
                Value -= ScrollClickAmount;
            if (_buttonDown.MouseJustDown)
                Value += ScrollClickAmount;

            if (_buttonUp.IsButtonPressed)
                _buttonUpPressTimer += elapsedSeconds;
            else
                _buttonUpPressTimer = 0;

            if (_buttonUpPressTimer > ScrollHoldTimeThreshold && _buttonUp.MouseDownOnButton)
                Value -= ScrollHoldAmountPerSecond * elapsedSeconds;
            if (_buttonDownPressTimer > ScrollHoldTimeThreshold && _buttonDown.MouseDownOnButton)
                Value += ScrollHoldAmountPerSecond * elapsedSeconds;

            if (_buttonDown.IsButtonPressed)
                _buttonDownPressTimer += elapsedSeconds;
            else
                _buttonDownPressTimer = 0;
        }
    }
}
