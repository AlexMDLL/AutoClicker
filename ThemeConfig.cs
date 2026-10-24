using System.Windows.Media;

namespace AutoClicker
{
    /// <summary>
    /// Сохранённая пользовательская тема. Хранится отдельным JSON-файлом
    /// в папке theme_configs.
    /// </summary>
    public class ThemeConfig
    {
        public string Name { get; set; } = "My theme";

        public Color Background { get; set; } = Color.FromRgb(0x0B, 0x0F, 0x19);
        public Color Surface { get; set; } = Color.FromRgb(0x10, 0x16, 0x24);
        public Color Card { get; set; } = Color.FromRgb(0x14, 0x1B, 0x2B);
        public Color Control { get; set; } = Color.FromRgb(0x1A, 0x23, 0x35);
        public Color Button { get; set; } = Color.FromRgb(0x21, 0x2C, 0x42);
        public Color Foreground { get; set; } = Color.FromRgb(0xE8, 0xEE, 0xFA);
        public Color Muted { get; set; } = Color.FromRgb(0x8A, 0x9A, 0xB5);
        public Color Border { get; set; } = Color.FromRgb(0x22, 0x30, 0x49);
        public Color Accent { get; set; } = Color.FromRgb(0x4C, 0x8D, 0xFF);
        public Color AccentSecondary { get; set; } = Color.FromRgb(0xA8, 0x55, 0xF7);
        public Color Success { get; set; } = Color.FromRgb(0x2D, 0xD4, 0xA7);
        public Color Danger { get; set; } = Color.FromRgb(0xFF, 0x5C, 0x7A);

        // Какие цветовые слоты пользователь переопределил
        public bool UseBackground { get; set; } = true;
        public bool UseForeground { get; set; } = true;
        public bool UseBorder { get; set; } = true;
        public bool UseControlBg { get; set; } = true;
        public bool UseButtonBg { get; set; } = true;
        public bool UseAccent { get; set; } = true;

        public static ThemeConfig FromPreset(ThemePreset preset, string name)
        {
            return new ThemeConfig
            {
                Name = name,
                Background = preset.Background,
                Surface = preset.Surface,
                Card = preset.Card,
                Control = preset.Control,
                Button = preset.Button,
                Foreground = preset.Foreground,
                Muted = preset.Muted,
                Border = preset.Border,
                Accent = preset.Accent,
                AccentSecondary = preset.AccentSecondary,
                Success = preset.Success,
                Danger = preset.Danger
            };
        }

        public ThemePreset ToPreset()
        {
            return new ThemePreset
            {
                Name = "custom:" + Name,
                DisplayName = Name,
                IsCustom = true,
                IsDark = ThemeManager.Luminance(Background) < 0.5,
                Background = Background,
                Surface = Surface,
                Card = Card,
                Control = Control,
                Button = Button,
                Foreground = Foreground,
                Muted = Muted,
                Border = Border,
                Accent = Accent,
                AccentSecondary = AccentSecondary,
                Success = Success,
                Danger = Danger
            };
        }
    }
}
