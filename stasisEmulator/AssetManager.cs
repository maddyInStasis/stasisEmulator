using FontStashSharp;
using System;
using System.Collections.Generic;

namespace stasisEmulator
{
    internal static class AssetManager
    {
        public static Dictionary<string, FontSystem> Fonts = [];

        public static FontSystem DefaultFont;

        public static SpriteFontBase GetFont(FontSystem font, float fontSize)
        {
            if (font == null)
            {
                if (DefaultFont == null)
                    return null;
                else
                    font = DefaultFont;
            }

            return font.GetFont(fontSize);
        }
    }
}
