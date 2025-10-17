using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stasisEmulator.UI.Controls
{
    internal class UIRectangle : UIControl
    {
        public UIRectangle() : base() { }
        public UIRectangle(UIControl parent) : base(parent) { }
        public UIRectangle(List<UIControl> children) : base(children) { }
    }
}
