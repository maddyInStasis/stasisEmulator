using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.Input;
using stasisEmulator.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    public enum SliderDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    public enum SliderTrackExtents
    {
        ToEdge,
        ThumbPadded
    }

    public enum SliderSizeType
    {
        Pixels,
        Relative
    }

    public struct SliderSize(float value, SliderSizeType type, int min, int max)
    {
        public float Value { get; private set; } = value;
        public SliderSizeType Type { get; private set; } = type;

        public int Min { get; private set; } = min;
        public int Max { get; private set; } = max;

        public static SliderSize Pixels(float value)
        {
            return new(value, SliderSizeType.Pixels, 0, 0);
        }

        public static SliderSize Relative(float value, int min = 0, int max = int.MaxValue)
        {
            return new(value, SliderSizeType.Relative, min, max);
        }

        public int GetPixelSize(int containerSize)
        {
            return Type == SliderSizeType.Pixels ? (int)Value : (int)Math.Clamp(Value * containerSize, Min, Max);
        }
    }

    public struct SliderRange(float min, float max)
    {
        public float Min { get; set; } = min;
        public float Max { get; set; } = max;
    }

    public class UISlider : UIControl
    {
        private MouseComponent _mainMouseComponent;
        private MouseComponent _thumbMouseComponent;

        public UISlider() { Init(); }
        public UISlider(UIControl parent) : base(parent) { Init(); }

        private float _sliderValue = 0;
        public float Value 
        { 
            get => _sliderValue; 
            set 
            { 
                float newValue = Math.Clamp(value, Range.Min, Range.Max);

                if (newValue != _sliderValue) 
                    ValueChanged?.Invoke(this, EventArgs.Empty); 

                _sliderValue = newValue;
            } 
        }
        public event EventHandler ValueChanged;

        public SliderRange _range = new(0, 1);
        public SliderRange Range 
        { 
            get => _range;
            set
            {
                _range = value;
                if (_range.Min > _range.Max)
                    throw new Exception("Slider range minimum must be greater than maximum.");
            }
        }

        private float _thumbValueMouseOffset = 0;

        public bool Enabled { get; set; } = true;

        public SliderDirection SliderDirection { get; set; } = SliderDirection.LeftToRight;
        private bool Horizontal { get => (SliderDirection == SliderDirection.LeftToRight || SliderDirection == SliderDirection.RightToLeft); }
        private bool Inverted { get => (SliderDirection == SliderDirection.RightToLeft || SliderDirection == SliderDirection.BottomToTop); }

        public SliderSize TrackThickness { get; set; } = SliderSize.Pixels(8);
        public SliderTrackExtents TrackExtents { get; set; } = SliderTrackExtents.ThumbPadded;

        public Color TrackInactiveColor { get; set; } = Color.LightGray;
        public Color TrackActiveColor { get; set; } = Color.Gray;
        public Color TrackDisabledColor { get; set; } = Color.LightGray;

        public SliderSize ThumbSize { get; set; } = SliderSize.Pixels(20);

        public Color ThumbIdleColor { get; set; } = Color.Gray;
        public Color ThumbHoverColor { get; set; } = Color.LightGray;
        public Color ThumbDragColor { get; set; } = Color.LightGray;
        public Color ThumbDisabledColor { get; set; } = Color.LightGray;

        private void Init()
        {
            _mainMouseComponent = new(this);
            _thumbMouseComponent = new(this);
        }

        private float GetNormalizedValue()
        {
            if (Range.Min == Range.Max)
                return 0;

            float drawValue = (Value - Range.Min) / (Range.Max - Range.Min);
            return drawValue;
        }

        private Rectangle GetThumbRect()
        {
            float drawValue = GetNormalizedValue();
            drawValue = Inverted ? 1 - drawValue : drawValue;

            int boundsAxisSize = Horizontal ? ComputedWidth : ComputedHeight;
            int thumbAxisSize = ThumbSize.GetPixelSize(boundsAxisSize);
            int thumbAxisPos = (int)((boundsAxisSize - thumbAxisSize) * drawValue);

            Rectangle thumbRect = Horizontal ?
                new(ComputedX + thumbAxisPos, ComputedY, thumbAxisSize, ComputedHeight) :
                new(ComputedX, ComputedY + thumbAxisPos, ComputedWidth, thumbAxisSize);

            return thumbRect;
        }

        private float GetValueFromMousePos(Rectangle trackRect)
        {
            var mousePos = _mainMouseComponent.GetNormalizedMousePositionInRectangle(trackRect);
            float newValue = Horizontal ? mousePos.X : mousePos.Y;
            if (Inverted)
                newValue = 1 - newValue;
            newValue = newValue * (Range.Max - Range.Min) + Range.Min;

            return newValue;
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            Rectangle thumbRect = GetThumbRect();
            _thumbMouseComponent.UpdateAsRectangle(thumbRect, Enabled && PropagatedVisibility);
            _mainMouseComponent.Update(Enabled && PropagatedVisibility);

            if (!Enabled || !PropagatedVisibility)
                return;

            int boundsAxisSize = Horizontal ? ComputedWidth : ComputedHeight;

            int thumbAxisSize = Horizontal ? thumbRect.Width : thumbRect.Height;
            int trackAxisInset = thumbAxisSize / 2;
            int trackAxisSize = boundsAxisSize - trackAxisInset * 2;

            Rectangle trackRect = Horizontal ?
                new(ComputedX + trackAxisInset, ComputedY, trackAxisSize, ComputedHeight) :
                new(ComputedX, ComputedY + trackAxisInset, ComputedWidth, trackAxisSize);

            if (_thumbMouseComponent.ElementMouseJustDown)
            {
                float clickedValue = GetValueFromMousePos(trackRect);
                _thumbValueMouseOffset = Value - clickedValue;
            }

            if (!_thumbMouseComponent.IsElementPressed)
            {
                if (!_mainMouseComponent.ElementMouseJustDown)
                    return;

                Value = GetValueFromMousePos(trackRect);
                _thumbMouseComponent.IsElementPressed = true;
                _thumbValueMouseOffset = 0;
                return;
            }

            Value = GetValueFromMousePos(trackRect) + _thumbValueMouseOffset;
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            float drawValue = GetNormalizedValue();
            drawValue = Inverted ? 1 - drawValue : drawValue;

            int boundsAxisSize = Horizontal ? ComputedWidth : ComputedHeight;
            int boundsCrossSize = Horizontal ? ComputedHeight : ComputedWidth;

            int thumbAxisSize = ThumbSize.GetPixelSize(boundsAxisSize);
            int trackAxisInset = TrackExtents == SliderTrackExtents.ThumbPadded ? thumbAxisSize / 2 : 0;
            int trackAxisSize = boundsAxisSize - trackAxisInset * 2;

            int trackThicknessPixels = TrackThickness.GetPixelSize(boundsCrossSize);
            int trackCrossInset = (boundsCrossSize - trackThicknessPixels) / 2;

            Rectangle trackRect = Horizontal ? 
                new(ComputedX + trackAxisInset, ComputedY + trackCrossInset, trackAxisSize, trackThicknessPixels) :
                new(ComputedX + trackCrossInset, ComputedY + trackAxisInset, trackThicknessPixels, trackAxisSize);

            int activeSize = (int)(trackAxisSize * drawValue);
            int inactiveSize = trackAxisSize - activeSize;

            Rectangle activeRect = trackRect;
            if (Horizontal)
                activeRect.Width = activeSize;
            else
                activeRect.Height = activeSize;

            Rectangle inactiveRect = trackRect;
            if (Horizontal)
            {
                inactiveRect.Width = inactiveSize;
                inactiveRect.X = ComputedX + trackAxisInset + activeSize;
            }
            else
            {
                inactiveRect.Height = inactiveSize;
                inactiveRect.Y = ComputedY + trackAxisInset + activeSize;
            }

            DrawRect(spriteBatch, Inverted ? inactiveRect : activeRect, Enabled ? TrackActiveColor : TrackDisabledColor);
            DrawRect(spriteBatch, Inverted ? activeRect : inactiveRect, Enabled ? TrackInactiveColor : TrackDisabledColor);

            Color thumbColor = _thumbMouseComponent.IsElementPressed ? ThumbDragColor : (_thumbMouseComponent.IsMouseHovered ? ThumbHoverColor : ThumbIdleColor);

            DrawRect(spriteBatch, GetThumbRect(), Enabled ? thumbColor : ThumbDisabledColor);
        }
    }
}
