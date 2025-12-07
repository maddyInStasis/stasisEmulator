using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stasisEmulator.Input;
using System.Collections.Generic;
using System.Diagnostics;

namespace stasisEmulator.UI.Controls
{
    public class UIMenuItem : UIButton
    {
        private static readonly List<UIMenuItem> _activeMenuItems = [];
        private static bool _anyClicked = false;

        private static UIMenuItem _hovered;
        private static float _hoveredTimer = 0;
        private const float _hoveredTimerThreshold = 0.5f;

        public string Text { get => _label.Text; set => _label.Text = value; }
        public Color TextColor { get => _label.TextColor; set => _label.TextColor = value; }

        public Color DropdownBackgroundColor { get => _dropDownContainer.BackgroundColor; set => _dropDownContainer.BackgroundColor = value; }

        protected int Depth { get; set; } = 0;

        private bool _active = false;
        private bool Active
        {
            get => _active;
            set
            {
                _active = value;
                _dropDownContainer.Visible = value;
            }
        }

        private bool _initOwner = true;

        private UIMenuItem MenuItemParent { get; set; } = null;
        private readonly List<UIMenuItem> MenuItemChildren = [];

        private UIRectangle _dropDownContainer;
        private UITextLabel _label;

        public UIMenuItem() : base() { Init(); }
        public UIMenuItem(UIControl parent) : base(parent) { Init(); }
        public UIMenuItem(List<UIMenuItem> children) : base() 
        { 
            Init();
            foreach(var child in children)
            {
                AddMenuItem(child);
            }
        }
        public UIMenuItem(string text, List<UIMenuItem> children) : this(children)
        {
            Text = text;
        }
        public UIMenuItem(string text) : this(text, []) { }

        private void Init()
        {
            _label = new(this)
            {
                Width = UISize.Grow(),
                Height = UISize.Fit()
            };

            _dropDownContainer = new()
            {
                Width = UISize.Fit(),
                Height = UISize.Fit(),
                AutoLayoutPosition = false,
                FillDirection = FillDirection.TopToBottom,
                Visible = false
            };
            ChildrenLocked = true;
        }

        private static void ClearActive()
        {
            if (_activeMenuItems.Count > 0)
                _activeMenuItems[0].SetActiveState(false);
        }

        private void SetActiveState(bool active)
        {
            if (Active == active)
                return;

            if (!active)
            {
                for (int i = _activeMenuItems.Count - 1; i >= Depth; i--)
                {
                    var item = _activeMenuItems[i];
                    item.Active = false;
                    _activeMenuItems.RemoveAt(i);
                }

                return;
            }

            ClearActive();

            UIMenuItem top = this;
            while (top.MenuItemParent != null)
            {
                _activeMenuItems.Insert(0, top.MenuItemParent);
                top = top.MenuItemParent;
                top.Active = true;
            }

            _activeMenuItems.Add(this);
            Active = true;
        }

        public void AddMenuItem(UIMenuItem item)
        {
            item.MenuItemParent?.RemoveMenuItem(item);
            MenuItemChildren.Add(item);

            item.Parent = _dropDownContainer;
            item.MenuItemParent = this;
            item.ParentLocked = true;
            item.Width = UISize.Grow();
            item.Height = UISize.Fit();
            item.Depth = Depth + 1;
            item.Active = false;
        }

        public void RemoveMenuItem(UIMenuItem item)
        {
            MenuItemChildren.Remove(item);
            item.ParentLocked = false;
            item.Parent = null;
            item.MenuItemParent = null;
            item.Depth = 0;
            item.Active = true;
        }

        public void ClearMenuItems()
        {
            for (int i = MenuItemChildren.Count - 1; i >= 0; i--)
            {
                var item = MenuItemChildren[i];
                RemoveMenuItem(item);
            }
        }

        protected override void UpdateElementPreLayout(GameTime gameTime)
        {
            if (_initOwner)
                _dropDownContainer.Window = Window;

            base.UpdateElementPreLayout(gameTime);
        }

        protected override void UpdateElementPostLayout(GameTime gameTime)
        {
            base.UpdateElementPostLayout(gameTime);
            _dropDownContainer.Update(gameTime);

            if (MouseJustDown)
            {
                _anyClicked = true;

                if (Depth == 0)
                    SetActiveState(!Active);
                else
                    SetActiveState(true);
            }

            if (JustClicked && MenuItemChildren.Count == 0)
                ClearActive();

            if (IsButtonHovered)
            {
                if (_hovered != this)
                {
                    _hovered = this;
                    _hoveredTimer = 0;
                }
            }
            else if (_hovered == this)
            {
                _hovered = null;
                _hoveredTimer = 0;
            }
        }

        public static void StaticUpdate(GameTime gameTime)
        {
            if (!_anyClicked && InputManager.MouseJustPressed)
            {
                ClearActive();
            }

            _anyClicked = false;

            if (_activeMenuItems.Count == 0)
                return;

            if (_hovered == null)
                return;

            if (_hovered.Active)
                return;

            if (_hovered.MenuItemParent == null)
            {
                _hovered.SetActiveState(true);
                return;
            }

            _hoveredTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_hoveredTimer >= _hoveredTimerThreshold)
            {
                _hovered.SetActiveState(true);
            }
        }

        protected override void SetChildrenPositions()
        {
            if (Depth == 0)
            {
                _dropDownContainer.SetPosition(ComputedX, ComputedY + ComputedHeight);
            }
            else
            {
                _dropDownContainer.SetPosition(ComputedX + ComputedWidth, ComputedY);
            }
        }

        protected override void RenderElementOnTop(SpriteBatch spriteBatch)
        {
            _dropDownContainer.Render(spriteBatch);
        }
    }
}
