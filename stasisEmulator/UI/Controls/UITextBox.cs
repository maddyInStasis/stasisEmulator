using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stasisEmulator.Input;
using stasisEmulator.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace stasisEmulator.UI.Controls
{
    public class UITextBox : UIControl
    {
        public FontSystem Font { get; set; }

        //for some reason, FontStashSharp's font sizes are really small. a factor of 1.75 seems to scale to the correct size
        private float CorrectedFontSize { get => FontSize * 1.75f; }
        public float FontSize { get; set; } = 12;

        public Color BackgroundColor { get; set; } = Color.White;
        public Color DisabledBackgroundColor { get; set; } = Color.LightGray;
        public Color TextColor { get; set; } = Color.Black;
        public int BorderThickness { get; set; } = 1;
        public Color BorderColor { get; set; } = Color.Black;
        public Color DisabledBorderColor { get; set; } = Color.Gray;

        public bool Enabled { get; set; } = true;
        public bool Focused { get; set; } = false;

        public string Text { get; set; } = string.Empty;

        public bool SelectAllOnClick { get; set; } = false;

        private int _cursorPosition = 0;
        private float _cursorBlinkTimer = 0;
        private const float CursorBlinkPeriod = 1;

        private int _selectionStart = 0;
        private int _selectionEnd = 0;
        private bool HasSelection { get => _selectionEnd > _selectionStart; }

        private MouseComponent _mouseComponent;
        private WindowKeyboardContext _keyboard;
        private WindowKeyboardState _keyboardState;

        private RenderTarget2D _renderTarget;

        public UITextBox() : base() { Init(); }
        public UITextBox(UIControl parent) : base(parent) { Init(); }

        private void Init()
        {
            Padding = new(4, 2);
            Width = UISize.Fixed(100);
        }

        public override void InitializeControl()
        {
            _mouseComponent = new(this);
            ChildrenLocked = true;

            _keyboard = Window.Keyboard;
            _keyboard.KeyDown += KeyDown;
        }

        protected override void CalculateContentHeight()
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            ComputedHeight = (int)MeasureStringHeightCorrected(spriteFont, "A");
        }

        //get glyphs treats spaces as having a width of 0... why
        private List<Rectangle> GetGlyphRects(string text, Vector2 pos)
        {
            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            var glyphs = spriteFont.GetGlyphs(text + "a", pos);
            var rects = new List<Rectangle>();
            for (int i = 0; i < glyphs.Count - 1; i++)
            {
                if (text[i] != ' ')
                {
                    rects.Add(glyphs[i].Bounds);
                }
                else
                {
                    var glyph = glyphs[i].Bounds;
                    var glyphNext = glyphs[i + 1].Bounds;
                    rects.Add(new Rectangle(glyph.Left, glyph.Top, glyphNext.Left - glyph.Left, glyph.Height));
                }
            }

            return rects;
        }

        private static readonly HashSet<Keys> _alphabet = [
            Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P,
            Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,
        ];
        private static readonly Dictionary<Keys, string> _symbols = new()
        {
            { Keys.D1, "1" }, { Keys.D2, "2" }, { Keys.D3, "3" }, { Keys.D4, "4" }, { Keys.D5, "5" },
            { Keys.D6, "6" }, { Keys.D7, "7" }, { Keys.D8, "8" }, { Keys.D9, "9" }, { Keys.D0, "0" },
        };
        private static readonly Dictionary<Keys, string> _shiftSymbols = new()
        {
            { Keys.D1, "!" }, { Keys.D2, "@" }, { Keys.D3, "#" }, { Keys.D4, "$" }, { Keys.D5, "%" },
            { Keys.D6, "^" }, { Keys.D7, "&" }, { Keys.D8, "*" }, { Keys.D9, "(" }, { Keys.D0, ")" },
        };

        //TODO: this doesn't quite seem like the right way to do things. refactor maybe
        //keep in mind that we'll want to be able to filter the kind of input, like only numbers, only alphanumeric characters, only hex digits, custom, etc
        private void KeyDown(object sender, KeyboardEventArgs e)
        {
            if (!Focused)
                return;

            if (DoNonTextInput(e.Key))
                return;

            if (IsModifierHeld())
                return;

            if (HasSelection && Text.Length > 0)
                DeleteSelection();

            string textToInsert = string.Empty;
            bool shiftHeld = _keyboardState.AnyShiftHeld();

            var key = e.Key;
            if (_alphabet.Contains(key))
            {
                textToInsert = Enum.GetName(typeof(Keys), key);

                if (!(shiftHeld || _keyboardState.CapsLock))
                    textToInsert = textToInsert.ToLower();
            }
            else if (key == Keys.Space)
            {
                textToInsert = " ";
            }
            else if (!shiftHeld && _symbols.TryGetValue(key, out string symbol))
            {
                textToInsert = symbol;
            }
            else if (shiftHeld && _shiftSymbols.TryGetValue(key, out string shiftSymbol))
            {
                textToInsert = shiftSymbol;
            }

            Text = Text.Insert(_cursorPosition, textToInsert);
            _cursorPosition += textToInsert.Length;
            _cursorBlinkTimer = 0;
        }

        /// <summary>
        /// Handles keyboard input which does not correspond to typing a character
        /// </summary>
        /// <param name="key">The key which was just pressed.</param>
        /// <returns>Whether or not a non-text input was processed.</returns>
        private bool DoNonTextInput(Keys key)
        {
            //TODO: ctrl + backspace, needs to delete previous "word", see below definition
            if (key == Keys.Back)
            {
                if (Text.Length == 0)
                    return true;

                if (HasSelection)
                {
                    DeleteSelection();
                }
                else
                {
                    if (_cursorPosition == 0)
                        return true;

                    Text = Text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    _selectionStart = _cursorPosition;
                    _selectionEnd = _cursorPosition;
                }

                _cursorBlinkTimer = 0;
                return true;
            }

            //TODO: ctrl + left/right, needs to move cursor up until divider (as long as nothing is selected, otherwise behaves like ctrl isn't held)
            //TODO: shift + left/right, needs to expand selection left/right one character
            //TODO: ctrl + shift + left/right, needs to expand selection up until divider
            if (key == Keys.Left)
            {
                if (HasSelection)
                {
                    _cursorPosition = _selectionStart;
                    _selectionEnd = _selectionStart;
                }
                else if (_cursorPosition > 0)
                {
                    _cursorPosition--;
                }
                return true;
            }
            if (key == Keys.Right)
            {
                if (HasSelection)
                {
                    _cursorPosition = _selectionEnd;
                    _selectionStart = _selectionEnd;
                }
                else if (_cursorPosition < Text.Length)
                {
                    _cursorPosition++;
                }
                return true;
            }

            if (_keyboardState.AnyCtrlHeld() && key == Keys.A)
            {
                _selectionStart = 0;
                _selectionEnd = Text.Length;
                return true;
            }

            return false;
        }

        private void DeleteSelection()
        {
            Text = Text.Remove(_selectionStart, _selectionEnd - _selectionStart);
            _cursorPosition = _selectionStart;
            _selectionEnd = _selectionStart;
        }

        private bool IsModifierHeld()
        {
            return _keyboardState.AnyCtrlHeld() || _keyboardState.AnyAltHeld() || _keyboardState.AnyWinHeld();
        }

        //TODO: clicking on another window SHOULDN'T lose focus
        //another window being in focus just hides the cursor, and the text input only responds to its own window
        //TODO: double click to select words, double click and drag to select multiple words
        //based on word boundaries, depending on direction being selected from
        //"word" in this case is defined as a grouping of symbols or non-symbols, but not both, and symbols include spaces
        //however, if a space is the first character, select the next whole word, too
        //why is this so complicated...
        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            _mouseComponent.Update(Visible && Enabled);
            if (_mouseComponent.IsMouseHovered)
            {
                Window.Cursor = MouseCursor.IBeam;
            }
            _keyboardState = _keyboard.GetState();

            if (InputManager.MouseJustPressed)
            {
                bool prevFocused = Focused;
                Focused = _mouseComponent.ElementMouseJustDown;
                if (Focused)
                {
                    _cursorBlinkTimer = 0;
                    _cursorPosition = GetCursorPositionFromMouse();

                    if (prevFocused || !SelectAllOnClick)
                    {
                        _selectionStart = _cursorPosition;
                        _selectionEnd = _cursorPosition;
                    }
                    else
                    {
                        //TODO: technically this only happens on mouse up, otherwise just place cursor at end and act like selection
                        //if, on mouse up, the text cursor has not moved, then select all
                        //also prevents the text cursor from moving until the mouse cursor moves
                        _selectionStart = 0;
                        _selectionEnd = Text.Length;
                    }
                }
            }
            else if (_mouseComponent.IsElementPressed)
            {
                int prevCursorPosition = _cursorPosition;
                _cursorPosition = GetCursorPositionFromMouse();

                if (prevCursorPosition == _selectionStart)
                    _selectionStart = _cursorPosition;
                else if (prevCursorPosition == _selectionEnd)
                    _selectionEnd = _cursorPosition;
                else if (prevCursorPosition != _cursorPosition) //TODO: when fixing selecting all text, remove me
                    _selectionStart = _cursorPosition;

                if (_selectionStart > _selectionEnd)
                    (_selectionEnd, _selectionStart) = (_selectionStart, _selectionEnd);

                if (prevCursorPosition != _cursorPosition && _selectionStart == _selectionEnd)
                    _cursorBlinkTimer = 0;
            }

            _cursorBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _cursorBlinkTimer %= CursorBlinkPeriod;
        }

        private int GetCursorPositionFromMouse()
        {
            if (Text.Length == 0)
                return 0;

            int relativePos = _mouseComponent.MousePosition.X - Position.X;
            relativePos -= BorderThickness + Padding.Left;

            var glyphs = GetGlyphRects(Text, Vector2.Zero);
            static int glyphCenter(Rectangle glyph) { return glyph.Center.X; }

            if (relativePos <= glyphCenter(glyphs[0]))
                return 0;
            if (relativePos > glyphCenter(glyphs[^1]))
                return glyphs.Count;

            static int search(int value, List<Rectangle> g)
            {
                int low = 0;
                int high = g.Count - 1;

                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    int center = glyphCenter(g[mid]);
                    if (value < center)
                    {
                        high = mid - 1;
                    }
                    else if (value > center)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        return mid;
                    }
                }

                return (glyphCenter(g[low]) - value) < (value - glyphCenter(g[high])) ? low : high;
            }

            int closestIndex = search(relativePos, glyphs);
            int closestCenter = glyphCenter(glyphs[closestIndex]);

            return relativePos <= closestCenter ? closestIndex : closestIndex + 1;
        }

        private static int GetCursorPixelPosition(List<Rectangle> glyphs, int index, int cursorWidth)
        {
            int offset;
            if (index <= 0)
                offset = 0;
            else if (index >= glyphs.Count - 1)
                offset = glyphs[index - 1].Right - cursorWidth / 2;
            else
                offset = (glyphs[index - 1].Right + glyphs[index].Left) / 2 - cursorWidth / 2;

            return offset;
        }

        protected override void RenderElementContents(SpriteBatch spriteBatch)
        {
            int targetWidth = ComputedWidth - Padding.HorizontalTotal - BorderThickness * 2;
            int targetHeight = ComputedHeight - Padding.VerticalTotal - BorderThickness * 2;

            if (targetWidth <= 0 || targetHeight <= 0)
                return;

            GraphicsDevice graphicsDevice = spriteBatch.GraphicsDevice;
            if (_renderTarget == null || _renderTarget.Bounds.Size != new Point(targetWidth, targetHeight))
                _renderTarget = new(graphicsDevice, targetWidth, targetHeight);

            graphicsDevice.SetRenderTarget(_renderTarget);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin();

            var spriteFont = AssetManager.GetFont(Font, CorrectedFontSize);
            var glyphs = GetGlyphRects(Text, Vector2.Zero);
            var blank = GetBlankTexture(spriteBatch);

            if (Focused && _selectionStart < _selectionEnd && _selectionStart < glyphs.Count)
            {
                int left = GetCursorPixelPosition(glyphs, _selectionStart, 0);
                int right = GetCursorPixelPosition(glyphs, _selectionEnd, 0);
                spriteBatch.Draw(blank, new Rectangle(left, 0, right - left, spriteFont.LineHeight), Color.Cyan);
            }

            spriteBatch.DrawString(spriteFont, Text, new Vector2(), TextColor);

            if (Focused && _cursorBlinkTimer < CursorBlinkPeriod / 2 && _selectionStart >= _selectionEnd)
            {
                int cursorWidth = (int)Math.Max(CorrectedFontSize / 25, 1);
                spriteBatch.Draw(blank, new Rectangle(new(GetCursorPixelPosition(glyphs, _cursorPosition, cursorWidth), 0), new(cursorWidth, spriteFont.LineHeight)), TextColor);
            }

            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }

        protected override void RenderElementOutput(SpriteBatch spriteBatch)
        {
            DrawBoundsRect(spriteBatch, Enabled ? BackgroundColor : DisabledBackgroundColor);
            if (_renderTarget != null)
                spriteBatch.Draw(_renderTarget, new Rectangle(Position + new Point(Padding.Left + BorderThickness, Padding.Top + BorderThickness), _renderTarget.Bounds.Size), Color.White);
            DrawBoundsBorder(spriteBatch, BorderThickness, BorderType.Inside, Enabled ? BorderColor : DisabledBorderColor);

            /*
            var debugSpriteFont = AssetManager.GetFont(Font, CorrectedFontSize / 6);
            var glyphs = GetGlyphRects(Text, new Vector2(Position.X + Padding.Left + BorderThickness, Position.Y + Padding.Top + BorderThickness));

            foreach (var glyph in glyphs)
            {
                int center = glyph.Center.X;
                string text = (center - Position.X - Padding.Left - BorderThickness).ToString();
                int width = (int)debugSpriteFont.MeasureString(text).X;
                spriteBatch.DrawString(debugSpriteFont, text, new Vector2(center - width / 2, Position.Y - 40), TextColor);
                spriteBatch.Draw(GetBlankTexture(spriteBatch), new Rectangle(center - 1, Position.Y, 2, Size.Y), TextColor);
            }
            */
        }
    }
}
