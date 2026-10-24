using System;

namespace AutoClicker
{
    /// <summary>Всё, что сохраняется между запусками.</summary>
    public class AppSettings
    {
        public int SettingsVersion { get; set; } = 2;

        // === Интервал ===
        public int IntervalHours { get; set; }
        public int IntervalMinutes { get; set; }
        public int IntervalSeconds { get; set; }
        public int IntervalMs { get; set; } = 100;

        // === Клик ===
        public MouseButtonKind MouseButton { get; set; } = MouseButtonKind.Left;
        public ClickMode Mode { get; set; } = ClickMode.Foreground;
        public bool UseCustomInterval { get; set; }
        public double CustomInterval { get; set; } = 0.5;
        public int ClickHoldMs { get; set; } = 15;
        public bool ReturnCursor { get; set; }

        // === Доп. режимы ===
        public bool EnableCyclicMode { get; set; }
        public double ClickDuration { get; set; } = 5.0;
        public double RestDuration { get; set; } = 2.0;
        public bool EnableRandomization { get; set; }
        public int RandomOffsetX { get; set; } = 5;
        public int RandomOffsetY { get; set; } = 5;
        public bool EnableHumanization { get; set; }
        public int HumanizePercent { get; set; } = 20;

        // === Лимиты ===
        public bool EnableClickLimit { get; set; }
        public int MaxClicks { get; set; } = 1000;
        public bool EnableTimeLimit { get; set; }
        public int TimeLimitHours { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int TimeLimitSeconds { get; set; }

        // === Цель ===
        public int ZoneX { get; set; }
        public int ZoneY { get; set; }
        public int ZoneWidth { get; set; }
        public int ZoneHeight { get; set; }
        public string TargetWindowTitle { get; set; } = string.Empty;

        // === Горячая клавиша ===
        public uint HotkeyVk { get; set; } = 0x75; // F6
        public uint HotkeyModifiers { get; set; }
        public string HotkeyDisplay { get; set; } = "F6";
        public bool HotkeyEnabled { get; set; } = true;

        // === Интерфейс ===
        public string Language { get; set; } = "Русский";
        public string ThemeName { get; set; } = "midnight";
        public bool IsCustomTheme { get; set; }
        public bool IsTopmost { get; set; }
        public double WindowOpacity { get; set; } = 1.0;
        public bool MinimizeToTray { get; set; }
        public bool SmoothScrolling { get; set; } = true;

        // Кастомные цвета (hex #AARRGGBB)
        public string CustomBackground { get; set; } = "#FF0B0F19";
        public string CustomForeground { get; set; } = "#FFE8EEFA";
        public string CustomBorder { get; set; } = "#FF26344C";
        public string CustomControl { get; set; } = "#FF1B2437";
        public string CustomButton { get; set; } = "#FF233049";
        public string CustomAccent { get; set; } = "#FF4C8DFF";

        public bool UseCustomBackground { get; set; } = true;
        public bool UseCustomForeground { get; set; } = true;
        public bool UseCustomBorder { get; set; } = true;
        public bool UseCustomControl { get; set; } = true;
        public bool UseCustomButton { get; set; } = true;
        public bool UseCustomAccent { get; set; } = true;

        public AppSettings Clone() => (AppSettings)MemberwiseClone();
    }

    public enum MouseButtonKind
    {
        Left,
        Right,
        Middle
    }

    public enum ClickMode
    {
        /// <summary>Обычные клики через SendInput — работают везде, включая игры в фокусе.</summary>
        Foreground,
        /// <summary>Клики прямо в окно (PostMessage) — окно может быть свёрнуто, курсор не двигается.</summary>
        Background,
        /// <summary>Игровой: активирует выбранное окно и кликает SendInput с заданным удержанием.</summary>
        Game
    }
}
