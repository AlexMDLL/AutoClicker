using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace AutoClicker
{
    /// <summary>Человекочитаемые имена виртуальных клавиш.</summary>
    public static class KeyNames
    {
        private static readonly Dictionary<int, string> Special = new Dictionary<int, string>
        {
            [0x08] = "Backspace",
            [0x09] = "Tab",
            [0x0D] = "Enter",
            [0x1B] = "Esc",
            [0x20] = "Space",
            [0x13] = "Pause",
            [0x14] = "CapsLock",
            [0x21] = "PageUp",
            [0x22] = "PageDown",
            [0x23] = "End",
            [0x24] = "Home",
            [0x25] = "Left",
            [0x26] = "Up",
            [0x27] = "Right",
            [0x28] = "Down",
            [0x2C] = "PrintScreen",
            [0x2D] = "Insert",
            [0x2E] = "Delete",
            [0x5B] = "Win",
            [0x5C] = "Win",
            [0x5D] = "Menu",
            [0x6A] = "Num *",
            [0x6B] = "Num +",
            [0x6D] = "Num -",
            [0x6E] = "Num .",
            [0x6F] = "Num /",
            [0x90] = "NumLock",
            [0x91] = "ScrollLock"
        };

        private static readonly Dictionary<string, string> Oem = new Dictionary<string, string>
        {
            ["OemPlus"] = "+",
            ["OemMinus"] = "-",
            ["OemComma"] = ",",
            ["OemPeriod"] = ".",
            ["OemQuestion"] = "/",
            ["OemSemicolon"] = ";",
            ["OemQuotes"] = "'",
            ["OemOpenBrackets"] = "[",
            ["OemCloseBrackets"] = "]",
            ["OemPipe"] = "\\",
            ["OemTilde"] = "`",
            ["OemBackslash"] = "\\"
        };

        public static string Name(int virtualKey)
        {
            if (Special.TryGetValue(virtualKey, out string special)) return special;

            Key key = KeyInterop.KeyFromVirtualKey(virtualKey);
            if (key >= Key.D0 && key <= Key.D9)
                return ((int)(key - Key.D0)).ToString();

            string text = key.ToString();
            if (Oem.TryGetValue(text, out string oem)) return oem;
            if (text.StartsWith("NumPad", StringComparison.Ordinal))
                return "Num " + text.Substring(6);
            return text;
        }

        public static string Format(bool ctrl, bool shift, bool alt, bool win, int virtualKey)
        {
            var parts = new List<string>(5);
            if (ctrl) parts.Add("Ctrl");
            if (shift) parts.Add("Shift");
            if (alt) parts.Add("Alt");
            if (win) parts.Add("Win");
            parts.Add(Name(virtualKey));
            return string.Join(" + ", parts);
        }

        public static string Format(KeyBindingEntry entry)
            => entry == null ? string.Empty : Format(entry.Ctrl, entry.Shift, entry.Alt, entry.Win, entry.VirtualKey);
    }
}
