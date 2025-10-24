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

    public class UISlider : UIControl
    {
        private MouseComponent _mainMouseComponent;
        private MouseComponent _thumbMouseComponent;

        public UISlider() { Init(); }
        public UISlider(UIControl parent) : base(parent) { Init(); }

        private float _sliderValue = 0;
        public float Value { get => _sliderValue; 
            set 
            { 
                if (value != _sliderValue) 
                    ValueChanged?.Invoke(this, EventArgs.Empty); 

                _sliderValue = Math.Clamp(value, 0, 1); 
            } 
        }
        public event EventHandler ValueChanged;

        private float _thumbValueMouseOffset = 0;

        public bool Enabled { get; set; } = true;

        public SliderDirection SliderDirection { get; set; } = SliderDirection.LeftToRight;

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

        private Rectangle GetThumbRect()
        {
            bool horizontal = SliderDirection == SliderDirection.LeftToRight || SliderDirection == SliderDirection.RightToLeft;
            bool inverted = SliderDirection == SliderDirection.RightToLeft || SliderDirection == SliderDirection.TopToBottom;
            float drawValue = inverted ? 1 - Value : Value;

            int boundsAxisSize = horizontal ? ComputedWidth : ComputedHeight;
            int thumbAxisSize = ThumbSize.GetPixelSize(boundsAxisSize);
            int thumbAxisPos = (int)((boundsAxisSize - thumbAxisSize) * drawValue);

            Rectangle thumbRect = horizontal ?
                new(ComputedX + thumbAxisPos, ComputedY, thumbAxisSize, ComputedHeight) :
                new(ComputedX, ComputedY + thumbAxisPos, ComputedWidth, thumbAxisSize);

            return thumbRect;
        }

        private float GetValueFromMousePos(Rectangle trackRect)
        {
            bool horizontal = SliderDirection == SliderDirection.LeftToRight || SliderDirection == SliderDirection.RightToLeft;
            bool inverted = SliderDirection == SliderDirection.RightToLeft || SliderDirection == SliderDirection.TopToBottom;

            var mousePos = MouseComponent.GetNormalizedMousePositionInRectangle(trackRect);
            float newValue = horizontal ? mousePos.X : mousePos.Y;
            if (inverted)
                newValue = 1 - newValue;

            return newValue;
        }

        protected override void UpdateElementPostLayout()
        {
            Rectangle thumbRect = GetThumbRect();
            _thumbMouseComponent.UpdateAsRectangle(thumbRect);
            _mainMouseComponent.Update();

            if (!Enabled)
                return;

            bool horizontal = SliderDirection == SliderDirection.LeftToRight || SliderDirection == SliderDirection.RightToLeft;

            int boundsAxisSize = horizontal ? ComputedWidth : ComputedHeight;

            int thumbAxisSize = horizontal ? thumbRect.Width : thumbRect.Height;
            int trackAxisInset = thumbAxisSize / 2;
            int trackAxisSize = boundsAxisSize - trackAxisInset * 2;

            Rectangle trackRect = horizontal ?
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

        protected override void RenderElement(SpriteBatch spriteBatch)
        {
            bool horizontal = SliderDirection == SliderDirection.LeftToRight || SliderDirection == SliderDirection.RightToLeft;
            bool inverted = SliderDirection == SliderDirection.RightToLeft || SliderDirection == SliderDirection.TopToBottom;
            float drawValue = inverted ? 1 - Value : Value;

            int boundsAxisSize = horizontal ? ComputedWidth : ComputedHeight;
            int boundsCrossSize = horizontal ? ComputedHeight : ComputedWidth;

            int thumbAxisSize = ThumbSize.GetPixelSize(boundsAxisSize);
            int trackAxisInset = TrackExtents == SliderTrackExtents.ThumbPadded ? thumbAxisSize / 2 : 0;
            int trackAxisSize = boundsAxisSize - trackAxisInset * 2;

            int trackThicknessPixels = TrackThickness.GetPixelSize(boundsCrossSize);
            int trackCrossInset = (boundsCrossSize - trackThicknessPixels) / 2;

            Rectangle trackRect = horizontal ? 
                new(ComputedX + trackAxisInset, ComputedY + trackCrossInset, trackAxisSize, trackThicknessPixels) :
                new(ComputedX + trackCrossInset, ComputedY + trackAxisInset, trackThicknessPixels, trackAxisSize);

            int activeSize = (int)(trackAxisSize * drawValue);
            int inactiveSize = trackAxisSize - activeSize;

            Rectangle activeRect = trackRect;
            if (horizontal)
                activeRect.Width = activeSize;
            else
                activeRect.Height = activeSize;

            Rectangle inactiveRect = trackRect;
            if (horizontal)
            {
                inactiveRect.Width = inactiveSize;
                inactiveRect.X = ComputedX + trackAxisInset + activeSize;
            }
            else
            {
                inactiveRect.Height = inactiveSize;
                inactiveRect.Y = ComputedY + trackAxisInset + activeSize;
            }

            DrawRect(spriteBatch, inverted ? inactiveRect : activeRect, Enabled ? TrackActiveColor : TrackDisabledColor);
            DrawRect(spriteBatch, inverted ? activeRect : inactiveRect, Enabled ? TrackInactiveColor : TrackDisabledColor);

            Color thumbColor = _thumbMouseComponent.IsElementPressed ? ThumbDragColor : (_thumbMouseComponent.IsMouseHovered ? ThumbHoverColor : ThumbIdleColor);

            DrawRect(spriteBatch, GetThumbRect(), Enabled ? thumbColor : ThumbDisabledColor);
        }
    }
}
