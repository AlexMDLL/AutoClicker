using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace AutoClicker
{
    /// <summary>
    /// Глобальные горячие клавиши. Раньше регистрация жила в окне и ломалась:
    /// команда из ViewModel вообще не включала режим перехвата, а старая клавиша
    /// не снималась. Теперь всё в одном месте, с проверкой результата.
    /// </summary>
    public static class HotkeyManager
    {
        public const int HotkeyId = 0xB001;
        private const int ProbeId = 0xB002;

        /// <summary>
        /// Проверяет, свободна ли комбинация, не трогая текущую регистрацию.
        /// Регистрируем «пробный» id и сразу снимаем его.
        /// </summary>
        public static bool IsAvailable(IntPtr hwnd, uint modifiers, uint vk, out string error)
        {
            error = null;
            if (hwnd == IntPtr.Zero) { error = "no window handle"; return false; }
            if (vk == 0) { error = "empty key"; return false; }

            uint flags = modifiers | NativeMethods.MOD_NOREPEAT;
            if (NativeMethods.RegisterHotKey(hwnd, ProbeId, flags, vk))
            {
                NativeMethods.UnregisterHotKey(hwnd, ProbeId);
                return true;
            }

            int code = Marshal.GetLastWin32Error();
            error = code == 1409 ? "already registered" : $"win32 error {code}";
            return false;
        }

        /// <summary>Регистрирует комбинацию. Возвращает false, если клавиша занята.</summary>
        public static bool TryRegister(IntPtr hwnd, uint modifiers, uint vk, out string error)
        {
            error = null;
            if (hwnd == IntPtr.Zero) { error = "no window handle"; return false; }
            if (vk == 0) { error = "empty key"; return false; }

            Unregister(hwnd);

            // MOD_NOREPEAT защищает от «залипания» при удержании клавиши.
            uint flags = modifiers | NativeMethods.MOD_NOREPEAT;
            if (NativeMethods.RegisterHotKey(hwnd, HotkeyId, flags, vk)) return true;

            int code = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            error = code == 1409 ? "already registered" : $"win32 error {code}";
            return false;
        }

        public static void Unregister(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            try { NativeMethods.UnregisterHotKey(hwnd, HotkeyId); } catch { }
        }

        public static uint FromModifierKeys(ModifierKeys keys)
        {
            uint result = 0;
            if ((keys & ModifierKeys.Control) != 0) result |= NativeMethods.MOD_CONTROL;
            if ((keys & ModifierKeys.Alt) != 0) result |= NativeMethods.MOD_ALT;
            if ((keys & ModifierKeys.Shift) != 0) result |= NativeMethods.MOD_SHIFT;
            if ((keys & ModifierKeys.Windows) != 0) result |= NativeMethods.MOD_WIN;
            return result;
        }

        public static bool IsModifierKey(Key key)
            => key == Key.LeftCtrl || key == Key.RightCtrl
            || key == Key.LeftAlt || key == Key.RightAlt
            || key == Key.LeftShift || key == Key.RightShift
            || key == Key.LWin || key == Key.RWin
            || key == Key.System || key == Key.None;

        public static string Format(uint modifiers, uint vk)
        {
            if (vk == 0) return "—";

            var sb = new StringBuilder();
            if ((modifiers & NativeMethods.MOD_CONTROL) != 0) sb.Append("Ctrl+");
            if ((modifiers & NativeMethods.MOD_ALT) != 0) sb.Append("Alt+");
            if ((modifiers & NativeMethods.MOD_SHIFT) != 0) sb.Append("Shift+");
            if ((modifiers & NativeMethods.MOD_WIN) != 0) sb.Append("Win+");
            sb.Append(PrettyKeyName(vk));
            return sb.ToString();
        }

        public static string PrettyKeyName(uint vk)
        {
            try
            {
                Key key = KeyInterop.KeyFromVirtualKey((int)vk);
                return PrettyKeyName(key);
            }
            catch { return "Key"; }
        }

        public static string PrettyKeyName(Key key)
        {
            if (key >= Key.A && key <= Key.Z) return key.ToString();
            if (key >= Key.D0 && key <= Key.D9) return ((int)(key - Key.D0)).ToString();
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return "Num " + (int)(key - Key.NumPad0);
            if (key >= Key.F1 && key <= Key.F24) return key.ToString();

            switch (key)
            {
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.Escape: return "Esc";
                case Key.Tab: return "Tab";
                case Key.Back: return "Backspace";
                case Key.Delete: return "Delete";
                case Key.Insert: return "Insert";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "PageUp";
                case Key.PageDown: return "PageDown";
                case Key.Up: return "↑";
                case Key.Down: return "↓";
                case Key.Left: return "←";
                case Key.Right: return "→";
                case Key.OemPlus: return "+";
                case Key.OemMinus: return "-";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                case Key.OemQuestion: return "/";
                case Key.OemSemicolon: return ";";
                case Key.OemQuotes: return "'";
                case Key.OemOpenBrackets: return "[";
                case Key.OemCloseBrackets: return "]";
                case Key.OemPipe: return "\\";
                case Key.OemTilde: return "`";
                case Key.OemBackslash: return "\\";
                case Key.PrintScreen: return "PrintScreen";
                case Key.Pause: return "Pause";
                case Key.CapsLock: return "CapsLock";
                case Key.NumLock: return "NumLock";
                case Key.Scroll: return "ScrollLock";
                case Key.Multiply: return "*";
                case Key.Add: return "+";
                case Key.Subtract: return "-";
                case Key.Divide: return "/";
                case Key.Decimal: return ".";
                case Key.VolumeUp: return "Vol+";
                case Key.VolumeDown: return "Vol-";
                case Key.VolumeMute: return "Mute";
                case Key.MediaPlayPause: return "Play";
                case Key.MediaNextTrack: return "Next";
                case Key.MediaPreviousTrack: return "Prev";
                case Key.MediaStop: return "Stop";
                default: return key.ToString();
            }
        }
    }
}
