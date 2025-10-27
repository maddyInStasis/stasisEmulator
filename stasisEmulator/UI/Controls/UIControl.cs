using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace stasisEmulator.UI.Controls
{
    public struct Padding
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;

        public int HorizontalTotal { get => Left + Right; }
        public int VerticalTotal { get => Top + Bottom; }

        public Padding()
        {

        }

        public Padding(int amount)
        {
            Left = amount;
            Right = amount;
            Top = amount;
            Bottom = amount;
        }

        public Padding(int horizontal, int vertical)
        {
            Left = horizontal;
            Right = horizontal;
            Top = vertical;
            Bottom = vertical;
        }

        public Padding(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }

    public enum UISizeType
    {
        Fit,
        Fixed,
        Grow
    }

    public struct UISize
    {
        public int Value { get; set;  }
        public UISizeType Type { get; set; }

        public int Min { get; set; }
        public int Max { get; set; }

        public UISize() { Max = int.MaxValue; }
        public UISize(int value, UISizeType type) { Value = value; Type = type; }

        public static UISize Fixed(int value)
        { 
            return new(value, UISizeType.Fixed); 
        }
        public static UISize Fit(int min = 0, int max = int.MaxValue) 
        { 
            return new(0, UISizeType.Fit) { Min = min, Max = max }; 
        }
        public static UISize Grow(int min = 0, int max = int.MaxValue)
        {
            return new(0, UISizeType.Grow) { Min = min, Max = max };
        }
    }

    public enum FillDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }
    
    /// <summary>
    /// The base class for GUI elements in a heirarchy structure.
    /// </summary>
    public abstract class UIControl 
    {
        private UISize _width = new();
        /// <summary>
        /// The preferred width of this element in the layout.
        /// </summary>
        public UISize Width 
        { 
            get => _width; 
            set 
            {
                if (SizeLocked)
                    return;

                _width = value;
                if (value.Type == UISizeType.Fixed)
                {
                    ComputedWidth = value.Value;
                    ComputedMinimumWidth = value.Value;
                }
            } 
        }

        private UISize _height = new();
        /// <summary>
        /// The preferred height of this element in the layout.
        /// </summary>
        public UISize Height 
        { 
            get => _height; 
            set 
            {
                if (SizeLocked)
                    return;

                _height = value;
                if (value.Type == UISizeType.Fixed)
                {
                    ComputedHeight = value.Value;
                    ComputedMinimumHeight = value.Value;
                }
            }
        }

        /// <summary>
        /// When <c>true</c>, prevent this control from having its Width and Height properties modified.
        /// <para>Used to lock outside code from modifying this element's size definition, such as component elements in a larger compound element. 
        /// Does not prevent the final size from being affected by the layout. For a static pixel size, use UISize.Fixed.</para>
        /// </summary>
        protected bool SizeLocked { get; set; } = false;

        /// <summary>
        /// The spacing between the boundaries of this control and its contents.
        /// </summary>
        public Padding Padding { get; set; }

        /// <summary>
        /// The spacing between contained child elements.
        /// </summary>
        public int ChildMargin { get; set; }

        private FillDirection _fillDirection = FillDirection.LeftToRight;
        /// <summary>
        /// The direction in which child elements fill this container element.
        /// </summary>
        public FillDirection FillDirection 
        { 
            get => _fillDirection; 
            set 
            {
                if (FillDirectionLocked)
                    return;

                _fillDirection = value;
            } 
        }
        protected bool FillDirectionLocked { get; set; } = false;
        protected bool IsHorizontalFill { get => (FillDirection == FillDirection.LeftToRight) || (FillDirection == FillDirection.RightToLeft); }

        /// <summary>
        /// The horizontal alignment of content and/or child controls within this element.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>
        /// The vertical alignment of content and/or child controls within this element.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment { get; set; } = VerticalAlignment.Top;

        /// <summary>
        /// Determines the container for this control. When null, assume this control is the root.
        /// </summary>
        public UIControl Parent { get => _parent; set => SetParent(value); }
        private UIControl _parent;

        /// <summary>
        /// When <c>true</c>, prevent this control from having its parent set.
        /// <para>Used for controls which should always be the root, such as such as UIWindow, or who's parent should not be changed, 
        /// such as a component of a larger control.</para>
        /// </summary>
        protected bool ParentLocked { get; set; } = false;

        private readonly List<UIControl> _children = [];

        /// <summary>
        /// When <c>true</c>, prevent other controls from setting this control as their parent.
        /// <para>Used for controls which should not act as containers, such as image/text displays or scroll bars.</para>
        /// </summary>
        protected bool ChildrenLocked { get; set; } = false;

        protected int ComputedX { get; private set; }
        protected int ComputedY { get; private set; }
        protected int ComputedWidth { get; set; }
        protected int ComputedHeight { get; set; }

        public Rectangle Bounds { get => new(ComputedX, ComputedY, ComputedWidth, ComputedHeight); }

        protected int ComputedMinimumWidth { get; set; }
        protected int ComputedMinimumHeight { get; set; }
        protected int ComputedMaximumWidth { get; set; }
        protected int ComputedMaximumHeight { get; set; }

        private static Texture2D blank;

        private static readonly List<char> TextSeparators = [' '];

        public UIControl() { }
        
        public UIControl(UIControl parent)
        {
            Parent = parent;
        }

        public UIControl(List<UIControl> children)
        {
            foreach (var child in children)
            {
                child.Parent = this;
            }
        }

        public bool HasDescendant(UIControl control)
        {
            foreach (UIControl child in _children)
            {
                if (child == control) return true;
                if (child.HasDescendant(control)) return true;
            }

            return false;
        }

        private void SetParent(UIControl parent)
        {
            if (ParentLocked)
                throw new ArgumentException($"Could not set parent. {nameof(ParentLocked)} of child is true.");

            //Detach node from the tree
            _parent?._children.Remove(this);
            _parent = null;

            if (parent.ChildrenLocked)
                throw new ArgumentException($"Could not set parent. {nameof(parent.ChildrenLocked)} of parent is true.");

            //Disallow circular dependencies
            if (parent == this)
                throw new ArgumentException("Control cannot be its own parent.");
            if (HasDescendant(parent))
                throw new ArgumentException("Control cannot be its own ancestor. (New parent was either a child or descendant of this control.)");

            //Attach node to new parent
            _parent = parent;
            _parent?._children.Add(this);
        }

        public void Update(GameTime gameTime)
        {
            PreUpdateUpTree(gameTime);
            SolveLayout();
            PostUpdateUpTree(gameTime);
        }

        private void PreUpdateUpTree(GameTime gameTime)
        {
            //maybe not the best place to put this, but idk where else it fits, and if an element sets these values in UpdateElement, they shouldn't be overridden
            ComputedMaximumWidth = int.MaxValue;
            ComputedMaximumHeight = int.MaxValue;

            foreach (var child in _children)
            {
                child.PreUpdateUpTree(gameTime);
            }

            UpdateElementPreLayout(gameTime);
        }

        private void PostUpdateUpTree(GameTime gameTime)
        {
            foreach (var child in _children)
            {
                child.PostUpdateUpTree(gameTime);
            }

            UpdateElementPostLayout(gameTime);
        }

        /// <summary>
        /// Overridable update method which updates from the bottom up (depth-first), before the layout pass. Is empty by default.
        /// </summary>
        protected virtual void UpdateElementPreLayout(GameTime gameTime) { }

        /// <summary>
        /// Overridable update method which updates from the bottom up (depth-first), after the layout pass. Is empty by default.
        /// </summary>
        protected virtual void UpdateElementPostLayout(GameTime gameTime) { }

        private void SolveLayout()
        {
            FitWidthUpTree();
            if (Width.Type == UISizeType.Grow)
                ComputedWidth = ComputedMaximumWidth;
            GrowShrinkWidthDownTree();
            WrapContentsUpTree();
            FitHeightUpTree();
            if (Height.Type == UISizeType.Grow)
                ComputedHeight = ComputedMaximumHeight;
            GrowShrinkHeightDownTree();
            SolvePositionsDownTree();
        }

        private void FitWidthUpTree()
        {
            foreach (var child in _children)
            {
                child.FitWidthUpTree();
            }

            if (Width.Type != UISizeType.Fixed)
                FitWidth();
        }

        protected virtual void CalculateContentWidth() { }

        protected void FitWidth()
        {
            ComputedWidth = 0;
            ComputedMinimumWidth = 0;

            CalculateContentWidth();

            int childrenWidth = 0;
            int childrenMinimumWidth = 0;

            foreach (var child in _children)
            {
                if (IsHorizontalFill)
                {
                    childrenWidth += child.ComputedWidth;
                    childrenMinimumWidth += child.ComputedMinimumWidth;
                }
                else
                {
                    childrenWidth = Math.Max(childrenWidth, child.ComputedWidth);
                    childrenMinimumWidth = Math.Max(childrenMinimumWidth, child.ComputedMinimumWidth);
                }
            }

            if (IsHorizontalFill)
            {
                childrenWidth += (_children.Count - 1) * ChildMargin;
                childrenMinimumWidth += (_children.Count - 1) * ChildMargin;
            }

            ComputedWidth = Math.Max(ComputedWidth, childrenWidth);
            ComputedMinimumWidth = Math.Max(ComputedMinimumWidth, childrenMinimumWidth);

            ComputedWidth += Padding.HorizontalTotal;
            ComputedMinimumWidth += Padding.HorizontalTotal;

            ComputedMinimumWidth = Math.Max(ComputedMinimumWidth, Width.Min);
            ComputedMaximumWidth = Math.Min(ComputedMaximumWidth, Width.Max);
            ComputedWidth = Math.Max(ComputedWidth, ComputedMinimumWidth);
            ComputedWidth = Math.Min(ComputedWidth, ComputedMaximumWidth);
        }

        private void DoGrowShrink(int remainingSize, bool horizontal)
        {
            int dir = Math.Sign(remainingSize);
            if (dir == 0)
                return;
            bool grow = dir == 1;

            int GetSize(UIControl child)
            {
                return horizontal ? child.ComputedWidth : child.ComputedHeight;
            }
            void SetSize(UIControl child, int value)
            {
                if (horizontal)
                    child.ComputedWidth = value;
                else
                    child.ComputedHeight = value;
            }
            int GetSizeConstraint(UIControl child)
            {
                if (horizontal)
                {
                    if (grow)
                        return child.Width.Max;
                    else
                        return child.ComputedMinimumWidth;
                }
                else
                {
                    if (grow)
                        return child.Height.Max;
                    else
                        return child.ComputedMinimumHeight;
                }    
            }

            bool IsSizable(UIControl child)
            {
                int dimension = GetSize(child);
                if (grow)
                {
                    int maxDimension = horizontal ? child.Width.Max : child.Height.Max;
                    if (dimension < maxDimension)
                        return true;
                }
                else
                {
                    int minDimension = horizontal ? child.ComputedMinimumWidth : child.ComputedMinimumHeight;
                    if (dimension > minDimension)
                        return true;
                }

                return false;
            }

            List<UIControl> sizableElements = [];
            foreach (var child in _children)
            {
                if (IsSizable(child) && (remainingSize < 0 || ((horizontal ? child.Width.Type : child.Height.Type) == UISizeType.Grow)))
                    sizableElements.Add(child);
            }

            while (remainingSize != 0 && sizableElements.Count > 0)
            {
                int mostSizable = GetSize(sizableElements[0]);
                int mostSizableCount = 0;
                int secondMostSizable = grow ? int.MaxValue : 0;
                int sizeToAdd = remainingSize;
                foreach (var child in sizableElements)
                {
                    int childSize = GetSize(child);

                    if (childSize == mostSizable)
                        mostSizableCount++;

                    if (grow ? childSize < mostSizable : childSize > mostSizable)
                    {
                        secondMostSizable = mostSizable;
                        mostSizable = childSize;
                        mostSizableCount = 1;
                    }
                    if (grow ? childSize > mostSizable : childSize < mostSizable)
                    {
                        secondMostSizable = grow ? Math.Min(secondMostSizable, childSize) : Math.Max(secondMostSizable, childSize);
                    }

                    sizeToAdd = secondMostSizable - mostSizable;
                }

                sizeToAdd = grow ? Math.Min(sizeToAdd, remainingSize / mostSizableCount) : Math.Max(sizeToAdd, remainingSize / mostSizableCount);
                sizeToAdd = grow ? Math.Max(sizeToAdd, 1) : Math.Min(sizeToAdd, -1);

                for (int i = sizableElements.Count - 1; i >= 0; i--)
                {
                    var child = sizableElements[i];
                    int childSize = GetSize(child);
                    int previousWidth = childSize;

                    if (childSize == mostSizable)
                    {
                        SetSize(child, childSize + sizeToAdd);
                        childSize = GetSize(child);
                        int constraint = GetSizeConstraint(child);
                        if (grow ? childSize > constraint : childSize < constraint)
                        {
                            SetSize(child, constraint);
                            childSize = GetSize(child);
                            sizableElements.RemoveAt(i);
                        }
                        remainingSize -= (childSize - previousWidth);

                        if (remainingSize == 0)
                            break;
                    }
                }
            }
        }

        private void GrowShrinkWidthDownTree()
        {
            int remainingWidth = ComputedWidth;
            remainingWidth -= Padding.HorizontalTotal;

            if (IsHorizontalFill)
            {
                foreach (var child in _children)
                {
                    remainingWidth -= child.ComputedWidth;
                }
                remainingWidth -= (_children.Count - 1) * ChildMargin;

                DoGrowShrink(remainingWidth, true);
            }
            else
            {
                foreach (var child in _children)
                {
                    if (child.Width.Type == UISizeType.Grow)
                    {
                        child.ComputedWidth += remainingWidth - child.ComputedWidth;
                        child.ComputedWidth = Math.Min(child.ComputedWidth, child.Width.Max);
                    }
                }
            }

            foreach (var child in _children)
            {
                child.GrowShrinkWidthDownTree();
            }
        }

        private void WrapContentsUpTree()
        {
            foreach (var child in _children)
            {
                child.WrapContentsUpTree();
            }

            WrapContents();
        }

        /// <summary>
        /// Wrap contents of controls to fit maximum width. Assumes content flows horizontally and wraps vertically.
        /// </summary>
        protected virtual void WrapContents() { }

        /// <summary>
        /// Inserts newlines into text to fit an available width (in pixels). Splits at spaces if possible.
        /// </summary>
        /// <param name="spriteFont">The font for the text to be displayed in.</param>
        /// <param name="text">The text to be wrapped.</param>
        /// <param name="availableWidth">The available width (in pixels) for the text to take up.</param>
        /// <returns>Text <paramref name="text"/>, wrapped using newlines to fit in <paramref name="availableWidth"/> when displayed with <paramref name="spriteFont"/>.</returns>
        protected static string WrapText(SpriteFontBase spriteFont, string text, int availableWidth)
        {
            List<string> lines = [];
            int startPos = 0;
            int endPos = 0;
            int lastSeparator = -1;

            string currentLine = string.Empty;

            while (endPos < text.Length)
            {
                currentLine = text.Substring(startPos, endPos - startPos + 1);
                char currentChar = text[endPos];
                int currentWidth = (int)spriteFont.MeasureString(currentLine).X;

                if (currentWidth > availableWidth && currentLine.Length > 1)
                {
                    endPos--;
                    if (lastSeparator > -1)
                        endPos = lastSeparator;

                    lines.Add(text.Substring(startPos, endPos - startPos + 1));
                    currentLine = string.Empty;
                    endPos++;
                    startPos = endPos;
                    lastSeparator = -1;
                }
                else
                {
                    if (TextSeparators.Contains(currentChar))
                        lastSeparator = endPos;

                    endPos++;
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine);

            string output = string.Empty;
            foreach (string line in lines)
            {
                string outputLine = line.TrimEnd();
                if (output.Length > 0)
                    output += '\n';
                output += outputLine;
            }

            return output;
        }

        //in SpriteFontBase.MeasureString, the height depends on the lowest descender of the last line of characters being used
        //this function replaces the last line with all ASCII characters to correct this
        //looks really silly though
        protected static float MeasureStringHeightCorrected(SpriteFontBase spriteFont, string text, Vector2? scale = null, float characterSpacing = 0, float lineSpacing = 0, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0)
        {
            string stringToMeasure = text;
            int lastLineStart = text.LastIndexOf('\n');
            if (lastLineStart > -1)
                stringToMeasure = stringToMeasure[..(lastLineStart + 1)];
            else
                stringToMeasure = string.Empty;
            //lmao
            stringToMeasure += "QWERTYUIOPASDFGHJKLZXCVBNM1234567890qwertyuiopasdfghjklzxcvbnm!@#$%^&*()-=_+[{]}\\|;:'\",<.>/?`~ ";

            return (int)spriteFont.MeasureString(stringToMeasure, scale, characterSpacing, lineSpacing, effect, effectAmount).Y;
        }

        private void FitHeightUpTree()
        {
            foreach (var child in _children)
            {
                child.FitHeightUpTree();
            }

            if (Height.Type != UISizeType.Fixed)
                FitHeight();
        }

        protected virtual void CalculateContentHeight() { }

        protected void FitHeight()
        {
            ComputedHeight = 0;
            ComputedMinimumHeight = 0;

            CalculateContentHeight();

            int childrenHeight = 0;
            int childrenMinimumHeight = 0;

            foreach (var child in _children)
            {
                if (!IsHorizontalFill)
                {
                    childrenHeight += child.ComputedHeight;
                    childrenMinimumHeight += child.ComputedMinimumHeight;
                }
                else
                {
                    childrenHeight = Math.Max(childrenHeight, child.ComputedHeight);
                    childrenMinimumHeight = Math.Max(childrenMinimumHeight, child.ComputedMinimumHeight);
                }
            }

            if (!IsHorizontalFill)
            {
                childrenHeight += (_children.Count - 1) * ChildMargin;
                childrenMinimumHeight += (_children.Count - 1) * ChildMargin;
            }

            ComputedHeight = Math.Max(ComputedHeight, childrenHeight);
            ComputedMinimumHeight = Math.Max(ComputedMinimumHeight, childrenMinimumHeight);

            ComputedHeight += Padding.VerticalTotal;
            ComputedMinimumHeight += Padding.VerticalTotal;

            ComputedMinimumHeight = Math.Max(ComputedMinimumHeight, Height.Min);
            ComputedMaximumHeight = Math.Min(ComputedMaximumHeight, Height.Max);
            ComputedHeight = Math.Max(ComputedHeight, ComputedMinimumHeight);
            ComputedHeight = Math.Min(ComputedHeight, ComputedMaximumHeight);
        }

        private void GrowShrinkHeightDownTree()
        {
            int remainingHeight = ComputedHeight;
            remainingHeight -= Padding.VerticalTotal;

            if (!IsHorizontalFill)
            {
                foreach (var child in _children)
                {
                    remainingHeight -= child.ComputedHeight;
                }
                remainingHeight -= (_children.Count - 1) * ChildMargin;

                DoGrowShrink(remainingHeight, false);
            }
            else
            {
                foreach (var child in _children)
                {
                    if (child.Height.Type == UISizeType.Grow)
                    {
                        child.ComputedHeight += remainingHeight - child.ComputedHeight;
                        child.ComputedHeight = Math.Min(child.ComputedHeight, child.Height.Max);
                    }
                }
            }

            foreach (var child in _children)
            {
                child.GrowShrinkHeightDownTree();
            }
        }

        private void SolvePositionsDownTree()
        {
            int totalChildrenAxisSize = 0;
            foreach (var child in _children)
            {
                if (IsHorizontalFill)
                    totalChildrenAxisSize += child.ComputedWidth;
                else
                    totalChildrenAxisSize += child.ComputedHeight;
            }
            totalChildrenAxisSize += ChildMargin * (_children.Count - 1);
            int remainingAxisSize = (IsHorizontalFill ? (ComputedWidth - Padding.HorizontalTotal) : (ComputedHeight - Padding.VerticalTotal)) - totalChildrenAxisSize;
            
            int axisOffset = FillDirection switch
            {
                FillDirection.LeftToRight => Padding.Left,
                FillDirection.RightToLeft => Padding.Left + totalChildrenAxisSize,
                FillDirection.TopToBottom => Padding.Top,
                FillDirection.BottomToTop => Padding.Top + totalChildrenAxisSize,
                _ => 0
            };

            if (IsHorizontalFill)
            {
                if (HorizontalContentAlignment == HorizontalAlignment.Right)
                    axisOffset += remainingAxisSize;
                else if (HorizontalContentAlignment == HorizontalAlignment.Center)
                    axisOffset += remainingAxisSize / 2;
            }
            else
            {
                if (VerticalContentAlignment == VerticalAlignment.Bottom)
                    axisOffset += remainingAxisSize;
                else if (VerticalContentAlignment == VerticalAlignment.Center)
                    axisOffset += remainingAxisSize / 2;
            }

            foreach (var child in _children)
            {
                if (IsHorizontalFill)
                {
                    int remainingCrossAxisSize = ComputedHeight - Padding.VerticalTotal - child.ComputedHeight;
                    int crossAxisOffset = ComputedY + Padding.Top;
                    child.ComputedY = VerticalContentAlignment switch
                    {
                        VerticalAlignment.Bottom => crossAxisOffset + remainingCrossAxisSize,
                        VerticalAlignment.Center => crossAxisOffset + remainingCrossAxisSize / 2,
                        _ => crossAxisOffset
                    };

                    if (FillDirection == FillDirection.LeftToRight)
                    {
                        child.ComputedX = ComputedX + axisOffset;
                        axisOffset += ChildMargin + child.ComputedWidth;
                    }
                    else
                    {
                        child.ComputedX = ComputedX + axisOffset - child.ComputedWidth;
                        axisOffset -= ChildMargin + child.ComputedWidth;
                    }
                }
                else
                {
                    int remainingCrossAxisSize = ComputedWidth - Padding.HorizontalTotal - child.ComputedWidth;
                    int crossAxisOffset = ComputedX + Padding.Left;
                    child.ComputedX = HorizontalContentAlignment switch
                    {
                        HorizontalAlignment.Right => crossAxisOffset + remainingCrossAxisSize,
                        HorizontalAlignment.Center => crossAxisOffset + remainingCrossAxisSize / 2,
                        _ => crossAxisOffset
                    };

                    if (FillDirection == FillDirection.TopToBottom)
                    {
                        child.ComputedY = ComputedY + axisOffset;
                        axisOffset += ChildMargin + child.ComputedHeight;
                    }
                    else
                    {
                        child.ComputedY = ComputedY + axisOffset - child.ComputedHeight;
                        axisOffset -= ChildMargin + child.ComputedHeight;
                    }
                }

                child.SolvePositionsDownTree();
            }
        }

        protected static Rectangle FitRectangle(Rectangle source, Rectangle container)
        {
            float sourceAspect = (float)source.Width / source.Height;
            float containerAspect = (float)container.Width / container.Height;

            Rectangle output = new();

            if (containerAspect > sourceAspect)
            {
                float scaleAmount = (float)container.Height / source.Height;
                output.Height = container.Height;
                output.Width = (int)(source.Width * scaleAmount);
                output.Y = container.Y;
                output.X = container.X + (container.Width - output.Width) / 2;
            }
            else
            {
                float scaleAmount = (float)container.Width / source.Width;
                output.Width = container.Width;
                output.Height = (int)(source.Height * scaleAmount);
                output.X = container.X;
                output.Y = container.Y + (container.Height - output.Height) / 2;
            }

            return output;
        }

        //TODO: Figure out if a SpriteEffect would work better here, and also see if that would help make rounded corners
        protected static Texture2D GetBlankTexture(SpriteBatch spriteBatch)
        {
            if (blank != null)
                return blank;

            blank = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            blank.SetData([Color.White]);
            return blank;
        }

        protected static void DrawRect(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
        {
            spriteBatch.Draw(GetBlankTexture(spriteBatch), rectangle, color);
        }

        protected void DrawBoundsRect(SpriteBatch spriteBatch, Color color)
        {
            DrawRect(spriteBatch, Bounds, color);
        }

        protected static void DrawTextWithAlignment(SpriteBatch spriteBatch, SpriteFontBase spriteFont, string text, Vector2 position, Color color, 
            HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, int contentWidth, int contentHeight, float rotation = 0, 
            Vector2 origin = default, Vector2? scale = null, float layerDepth = 0, float characterSpacing = 0, float lineSpacing = 0, 
            TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0)
        {
            int remainingHeight = contentHeight - (int)spriteFont.MeasureString(text).Y;
            int yOffset = verticalAlignment switch
            {
                VerticalAlignment.Bottom => remainingHeight,
                VerticalAlignment.Center => remainingHeight / 2,
                _ => 0
            };

            //here, we draw each line one at a time to allow for horizontal alignment
            //preceeding newlines are used to still use the font's vertical spacing
            var lines = text.Split('\n');
            string preceedingNewLines = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                int remainingWidth = contentWidth - (int)spriteFont.MeasureString(line).X;
                int xOffset = horizontalAlignment switch
                {
                    HorizontalAlignment.Right => remainingWidth,
                    HorizontalAlignment.Center => remainingWidth / 2,
                    _ => 0
                };

                spriteBatch.DrawString(spriteFont, preceedingNewLines + line, new Vector2(position.X + xOffset, position.Y + yOffset), color,
                    rotation: rotation, origin: origin, scale: scale, layerDepth: layerDepth, characterSpacing: characterSpacing, lineSpacing: lineSpacing,
                    textStyle: textStyle, effect: effect, effectAmount: effectAmount);

                preceedingNewLines += '\n';
            }
        }

        public void Render(SpriteBatch spriteBatch)
        {
            RenderContents(spriteBatch);
            spriteBatch.Begin();
            RenderOutput(spriteBatch);
            spriteBatch.End();
        }

        public void RenderContents(SpriteBatch spriteBatch)
        {
            foreach (var child in _children)
            {
                child.RenderContents(spriteBatch);
            }

            RenderElementContents(spriteBatch);
        }

        public void RenderOutput(SpriteBatch spriteBatch)
        {
            RenderElementOutput(spriteBatch);

            foreach (var child in _children)
            {
                child.RenderOutput(spriteBatch);
            }
        }

        /// <summary>
        /// Used to draw an element's contents to a texture. Expects <c>SpriteBatch.Begin</c> and <c>SpriteBatch.End</c> to be called in the method.
        /// </summary>
        /// <param name="spriteBatch"></param>
        protected virtual void RenderElementContents(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Used to output the final content of the element to the screen. Called after <c>RenderElementContents</c>. <c>SpriteBatch.Begin</c> and <c>SpriteBatch.End</c> have already been called.
        /// </summary>
        /// <param name="spriteBatch"></param>
        protected virtual void RenderElementOutput(SpriteBatch spriteBatch) { }
    }
}
