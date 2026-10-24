using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace AutoClicker
{
    /// <summary>
    /// Управляет палитрой приложения. Все кисти живут в Application.Resources,
    /// а разметка обращается к ним через DynamicResource — поэтому смена темы
    /// применяется мгновенно, без перезапуска.
    /// </summary>
    public static class ThemeManager
    {
        public static ThemePreset Current { get; private set; }

        public static List<ThemePreset> CreatePresets()
        {
            return new List<ThemePreset>
            {
                new ThemePreset
                {
                    Name = "midnight", DisplayName = "Midnight", IsDark = true,
                    Background = Rgb(0x0B0F19), Surface = Rgb(0x111726), Card = Rgb(0x151D2F),
                    Control = Rgb(0x1B2437), Button = Rgb(0x233049), Foreground = Rgb(0xE8EEFA),
                    Muted = Rgb(0x93A3BD), Border = Rgb(0x26344C), Accent = Rgb(0x4C8DFF),
                    AccentSecondary = Rgb(0xA855F7), Success = Rgb(0x2DD4A7), Danger = Rgb(0xFF5C7A)
                },
                new ThemePreset
                {
                    Name = "graphite", DisplayName = "Graphite", IsDark = true,
                    Background = Rgb(0x101112), Surface = Rgb(0x17181A), Card = Rgb(0x1D1F22),
                    Control = Rgb(0x232629), Button = Rgb(0x2C3034), Foreground = Rgb(0xECEDEF),
                    Muted = Rgb(0x9A9DA3), Border = Rgb(0x2E3338), Accent = Rgb(0x8AB4F8),
                    AccentSecondary = Rgb(0xC58AF9), Success = Rgb(0x34D399), Danger = Rgb(0xF87171)
                },
                new ThemePreset
                {
                    Name = "nordic", DisplayName = "Nordic", IsDark = true,
                    Background = Rgb(0x21252B), Surface = Rgb(0x282C34), Card = Rgb(0x2F343D),
                    Control = Rgb(0x363B45), Button = Rgb(0x3E4551), Foreground = Rgb(0xE5E9F0),
                    Muted = Rgb(0x98A2B3), Border = Rgb(0x3B4252), Accent = Rgb(0x88C0D0),
                    AccentSecondary = Rgb(0xB48EAD), Success = Rgb(0xA3BE8C), Danger = Rgb(0xBF616A)
                },
                new ThemePreset
                {
                    Name = "dracula", DisplayName = "Dracula", IsDark = true,
                    Background = Rgb(0x1E1F29), Surface = Rgb(0x24252F), Card = Rgb(0x282A36),
                    Control = Rgb(0x2F313E), Button = Rgb(0x3A3C4E), Foreground = Rgb(0xF8F8F2),
                    Muted = Rgb(0x9AA0B5), Border = Rgb(0x3B3E4F), Accent = Rgb(0xBD93F9),
                    AccentSecondary = Rgb(0xFF79C6), Success = Rgb(0x50FA7B), Danger = Rgb(0xFF5555)
                },
                new ThemePreset
                {
                    Name = "neon", DisplayName = "Neon", IsDark = true,
                    Background = Rgb(0x06060B), Surface = Rgb(0x0B0B14), Card = Rgb(0x10101C),
                    Control = Rgb(0x161625), Button = Rgb(0x1E1E33), Foreground = Rgb(0xEAF6FF),
                    Muted = Rgb(0x7C8CA6), Border = Rgb(0x1C2333), Accent = Rgb(0x00E5FF),
                    AccentSecondary = Rgb(0xFF2E97), Success = Rgb(0x00FFB2), Danger = Rgb(0xFF3B5C)
                },
                new ThemePreset
                {
                    Name = "ember", DisplayName = "Ember", IsDark = true,
                    Background = Rgb(0x120D0B), Surface = Rgb(0x191210), Card = Rgb(0x1F1714),
                    Control = Rgb(0x271C18), Button = Rgb(0x33241E), Foreground = Rgb(0xFBEDE6),
                    Muted = Rgb(0xB49A8E), Border = Rgb(0x2E211C), Accent = Rgb(0xFF8A3D),
                    AccentSecondary = Rgb(0xFF4D6D), Success = Rgb(0x4ADE80), Danger = Rgb(0xFF4D4D)
                },
                new ThemePreset
                {
                    Name = "emerald", DisplayName = "Emerald", IsDark = true,
                    Background = Rgb(0x07120F), Surface = Rgb(0x0B1A16), Card = Rgb(0x0F221D),
                    Control = Rgb(0x132B24), Button = Rgb(0x19382F), Foreground = Rgb(0xE6FBF4),
                    Muted = Rgb(0x86A99C), Border = Rgb(0x16362D), Accent = Rgb(0x19D3A2),
                    AccentSecondary = Rgb(0x3B82F6), Success = Rgb(0x19D3A2), Danger = Rgb(0xFF6B6B)
                },
                new ThemePreset
                {
                    Name = "daylight", DisplayName = "Daylight", IsDark = false,
                    Background = Rgb(0xEEF2F9), Surface = Rgb(0xF7F9FD), Card = Rgb(0xFFFFFF),
                    Control = Rgb(0xEDF1F8), Button = Rgb(0xE2E8F3), Foreground = Rgb(0x0F172A),
                    Muted = Rgb(0x64748B), Border = Rgb(0xDCE3EF), Accent = Rgb(0x2F6FED),
                    AccentSecondary = Rgb(0x7C3AED), Success = Rgb(0x0EA472), Danger = Rgb(0xDC2626)
                },
                new ThemePreset
                {
                    Name = "sakura", DisplayName = "Sakura", IsDark = false,
                    Background = Rgb(0xFBF1F5), Surface = Rgb(0xFFF8FA), Card = Rgb(0xFFFFFF),
                    Control = Rgb(0xF7E9F0), Button = Rgb(0xEFD8E3), Foreground = Rgb(0x3B1426),
                    Muted = Rgb(0x9A7083), Border = Rgb(0xEAD3DE), Accent = Rgb(0xE0447E),
                    AccentSecondary = Rgb(0x8B5CF6), Success = Rgb(0x10B981), Danger = Rgb(0xE11D48)
                }
            };
        }

        public static void Apply(ThemePreset preset)
        {
            if (preset == null) return;
            Current = preset;

            var res = Application.Current?.Resources;
            if (res == null) return;

            Color bg = preset.Background;
            Color accent = preset.Accent;

            Set(res, "WindowBgBrush", Brush(bg));
            Set(res, "WindowBgGradientBrush", Gradient(bg, Mix(bg, accent, 0.09), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1)));
            Set(res, "TitleBarBrush", Brush(Mix(bg, preset.Surface, 0.55)));
            Set(res, "SurfaceBrush", Brush(preset.Surface));
            Set(res, "CardBrush", Brush(preset.Card));
            Set(res, "CardHoverBrush", Brush(Mix(preset.Card, preset.Foreground, 0.06)));
            Set(res, "ControlBgBrush", Brush(preset.Control));
            Set(res, "InputBgBrush", Brush(preset.Control));
            Set(res, "ButtonBgBrush", Brush(preset.Button));
            Set(res, "ForegroundBrush", Brush(preset.Foreground));
            Set(res, "MutedBrush", Brush(preset.Muted));
            Set(res, "BorderBrush", Brush(preset.Border));
            Set(res, "BorderStrongBrush", Brush(Mix(preset.Border, preset.Foreground, 0.18)));
            Set(res, "AccentBrush", Brush(accent));
            Set(res, "AccentSoftBrush", Brush(WithAlpha(accent, 0x26)));
            Set(res, "AccentFaintBrush", Brush(WithAlpha(accent, 0x12)));
            Set(res, "AccentGlowBrush", Brush(WithAlpha(accent, 0x66)));
            Set(res, "AccentSecondaryBrush", Brush(preset.AccentSecondary));
            Set(res, "AccentGradientBrush", Gradient(accent, preset.AccentSecondary, new System.Windows.Point(0, 0), new System.Windows.Point(1, 1)));
            Set(res, "SuccessBrush", Brush(preset.Success));
            Set(res, "DangerBrush", Brush(preset.Danger));
            Set(res, "NavHoverBrush", Brush(WithAlpha(preset.Foreground, 0x0D)));
            Set(res, "NavSelectedBrush", Brush(WithAlpha(accent, 0x1F)));
            Set(res, "ScrollThumbBrush", Brush(Mix(preset.Border, preset.Foreground, 0.22)));
            Set(res, "ScrollThumbHoverBrush", Brush(Mix(preset.Border, accent, 0.45)));
            Set(res, "OverlayBrush", Brush(WithAlpha(Mix(bg, Colors.Black, 0.35), 0xE6)));
            Set(res, "WhiteBrush", Brush(Colors.White));

            Set(res, "AccentColorValue", accent);
            Set(res, "AccentSecondaryColorValue", preset.AccentSecondary);
            Set(res, "SuccessColorValue", preset.Success);
            Set(res, "DangerColorValue", preset.Danger);
            Set(res, "BackgroundColorValue", bg);
            Set(res, "ForegroundColorValue", preset.Foreground);
            Set(res, "MutedColorValue", preset.Muted);
            Set(res, "BorderColorValue", preset.Border);
            Set(res, "CardColorValue", preset.Card);
            Set(res, "ShadowColorValue", preset.IsDark ? Colors.Black : Color.FromRgb(0x1E, 0x29, 0x3B));
        }

        /// <summary>Собирает «кастомную» тему из набора пользовательских цветов.</summary>
        public static ThemePreset BuildCustom(Color background, Color foreground, Color border,
            Color control, Color button, Color accent, Color surface, Color card,
            Color accentSecondary, Color success, Color danger)
        {
            return new ThemePreset
            {
                Name = "custom",
                DisplayName = "Custom",
                IsDark = Luminance(background) < 0.5,
                IsCustom = true,
                Background = background,
                Foreground = foreground,
                Border = border,
                Control = control,
                Button = button,
                Accent = accent,
                Surface = surface,
                Card = card,
                AccentSecondary = accentSecondary,
                Success = success,
                Danger = danger,
                Muted = Mix(foreground, background, 0.42)
            };
        }

        private static void Set(ResourceDictionary res, string key, object value) => res[key] = value;

        public static SolidColorBrush Brush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static LinearGradientBrush Gradient(Color from, Color to, System.Windows.Point start, System.Windows.Point end)
        {
            var brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
            brush.GradientStops.Add(new GradientStop(from, 0));
            brush.GradientStops.Add(new GradientStop(to, 1));
            brush.Freeze();
            return brush;
        }

        public static Color Rgb(int rgb)
            => Color.FromRgb((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));

        public static Color WithAlpha(Color color, byte alpha)
            => Color.FromArgb(alpha, color.R, color.G, color.B);

        public static Color Mix(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (byte)(a.A + (b.A - a.A) * t),
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        public static Color Lighten(Color color, double amount) => Mix(color, Colors.White, amount);
        public static Color Darken(Color color, double amount) => Mix(color, Colors.Black, amount);

        public static double Luminance(Color c)
            => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
    }
}
