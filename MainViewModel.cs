using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AutoClicker
{
    public class MainViewModel : BaseViewModel
    {
        // === ЛОКАЛИЗАЦИЯ ===
        public string this[string key] => LanguageManager.GetString(CurrentLanguage, key);

        public string AppVersion
        {
            get
            {
                try
                {
                    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    return version != null ? "v" + version.ToString(3) : "v1.0.0";
                }
                catch { return "v1.0.0"; }
            }
        }

        private string _currentLanguage = "Русский";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (!SetField(ref _currentLanguage, value)) return;
                OnPropertyChanged("Item[]");
                RebuildLocalizedLabels();
                MarkDirty();
            }
        }

        public ObservableCollection<SelectableItem> LanguageOptions { get; } = new ObservableCollection<SelectableItem>();
        public ObservableCollection<SelectableItem> LayoutOptions { get; } = new ObservableCollection<SelectableItem>();
        public ObservableCollection<SelectableItem> MouseButtonOptions { get; } = new ObservableCollection<SelectableItem>();
        public ObservableCollection<SelectableItem> ModeOptions { get; } = new ObservableCollection<SelectableItem>();
        public ObservableCollection<SelectableItem> IntervalPresets { get; } = new ObservableCollection<SelectableItem>();
        public ObservableCollection<ThemePreset> Themes { get; } = new ObservableCollection<ThemePreset>();

        public ThemePreset CustomThemePreset { get; }

        // === ИНТЕРВАЛ ===
        private int _intervalHours;
        public int IntervalHours { get => _intervalHours; set { if (SetField(ref _intervalHours, Math.Max(0, value))) { SyncIntervalChips(); PushLiveSettings(); MarkDirty(); } } }

        private int _intervalMinutes;
        public int IntervalMinutes { get => _intervalMinutes; set { if (SetField(ref _intervalMinutes, Math.Max(0, value))) { SyncIntervalChips(); PushLiveSettings(); MarkDirty(); } } }

        private int _intervalSeconds;
        public int IntervalSeconds { get => _intervalSeconds; set { if (SetField(ref _intervalSeconds, Math.Max(0, value))) { SyncIntervalChips(); PushLiveSettings(); MarkDirty(); } } }

        private int _intervalMs = 100;
        public int IntervalMs { get => _intervalMs; set { if (SetField(ref _intervalMs, Math.Max(0, value))) { SyncIntervalChips(); PushLiveSettings(); MarkDirty(); } } }

        public string IntervalText
        {
            get
            {
                if (IntervalHours == 0 && IntervalMinutes == 0 && IntervalSeconds == 0 && IntervalMs == 0)
                    return T("IntervalTurbo");

                var parts = new List<string>();
                if (IntervalHours > 0) parts.Add($"{IntervalHours} {T("UnitHours")}");
                if (IntervalMinutes > 0) parts.Add($"{IntervalMinutes} {T("UnitMinutes")}");
                if (IntervalSeconds > 0) parts.Add($"{IntervalSeconds} {T("UnitSeconds")}");
                if (IntervalMs > 0 || parts.Count == 0) parts.Add($"{IntervalMs} {T("UnitMs")}");
                return string.Join(" ", parts);
            }
        }

        private void SyncIntervalChips()
        {
            foreach (var chip in IntervalPresets)
            {
                int ms = chip.Tag is int v ? v : -1;
                chip.IsSelected = IntervalHours == 0 && IntervalMinutes == 0 && IntervalSeconds == 0 && ms == IntervalMs;
            }
            OnPropertyChanged(nameof(IntervalText));
        }

        // === КНОПКА МЫШИ ===
        private MouseButtonKind _mouseButton = MouseButtonKind.Left;
        public MouseButtonKind MouseButton
        {
            get => _mouseButton;
            set
            {
                if (!SetField(ref _mouseButton, value)) return;
                SyncOptionSelection(MouseButtonOptions, value);
                PushLiveSettings();
                MarkDirty();
            }
        }

        // === РЕЖИМ ===
        private ClickMode _clickMode = ClickMode.Foreground;
        public ClickMode ClickMode
        {
            get => _clickMode;
            set
            {
                if (!SetField(ref _clickMode, value)) return;
                SyncOptionSelection(ModeOptions, value);
                OnPropertyChanged(nameof(ModeText));
                PushLiveSettings();
                MarkDirty();
            }
        }

        // === ПАРАМЕТРЫ КЛИКА ===
        private int _clickHoldMs = 15;
        public int ClickHoldMs { get => _clickHoldMs; set { if (SetField(ref _clickHoldMs, Math.Max(0, Math.Min(5000, value)))) { PushLiveSettings(); MarkDirty(); } } }

        private bool _returnCursor;
        public bool ReturnCursor { get => _returnCursor; set { if (SetField(ref _returnCursor, value)) { PushLiveSettings(); MarkDirty(); } } }

        private bool _useCustomInterval;
        public bool UseCustomInterval { get => _useCustomInterval; set { if (SetField(ref _useCustomInterval, value)) { PushLiveSettings(); MarkDirty(); } } }

        private double _customInterval = 0.5;
        public double CustomInterval { get => _customInterval; set { if (SetField(ref _customInterval, Math.Max(0, value))) { PushLiveSettings(); MarkDirty(); } } }

        // === ДОП. РЕЖИМЫ ===
        private bool _enableCyclicMode;
        public bool EnableCyclicMode { get => _enableCyclicMode; set { if (SetField(ref _enableCyclicMode, value)) { PushLiveSettings(); MarkDirty(); } } }

        private double _clickDuration = 5.0;
        public double ClickDuration { get => _clickDuration; set { if (SetField(ref _clickDuration, Math.Max(0, value))) { PushLiveSettings(); MarkDirty(); } } }

        private double _restDuration = 2.0;
        public double RestDuration { get => _restDuration; set { if (SetField(ref _restDuration, Math.Max(0, value))) { PushLiveSettings(); MarkDirty(); } } }

        private bool _enableRandomization;
        public bool EnableRandomization { get => _enableRandomization; set { if (SetField(ref _enableRandomization, value)) { PushLiveSettings(); MarkDirty(); } } }

        private int _randomOffsetX = 5;
        public int RandomOffsetX { get => _randomOffsetX; set { if (SetField(ref _randomOffsetX, Math.Max(1, value))) { PushLiveSettings(); MarkDirty(); } } }

        private int _randomOffsetY = 5;
        public int RandomOffsetY { get => _randomOffsetY; set { if (SetField(ref _randomOffsetY, Math.Max(1, value))) { PushLiveSettings(); MarkDirty(); } } }

        private bool _enableHumanization;
        public bool EnableHumanization { get => _enableHumanization; set { if (SetField(ref _enableHumanization, value)) { PushLiveSettings(); MarkDirty(); } } }

        private int _humanizePercent = 20;
        public int HumanizePercent { get => _humanizePercent; set { if (SetField(ref _humanizePercent, Math.Max(0, Math.Min(90, value)))) { PushLiveSettings(); MarkDirty(); } } }

        // === ЛИМИТЫ ===
        private bool _enableClickLimit;
        public bool EnableClickLimit { get => _enableClickLimit; set { if (SetField(ref _enableClickLimit, value)) { PushLiveSettings(); MarkDirty(); } } }

        private int _maxClicks = 1000;
        public int MaxClicks { get => _maxClicks; set { if (SetField(ref _maxClicks, Math.Max(1, value))) { PushLiveSettings(); MarkDirty(); } } }

        private bool _enableTimeLimit;
        public bool EnableTimeLimit { get => _enableTimeLimit; set { if (SetField(ref _enableTimeLimit, value)) { PushLiveSettings(); MarkDirty(); } } }

        private int _timeLimitHours;
        public int TimeLimitHours { get => _timeLimitHours; set { if (SetField(ref _timeLimitHours, Math.Max(0, value))) { PushLiveSettings(); MarkDirty(); } } }

        private int _timeLimitMinutes;
        public int TimeLimitMinutes { get => _timeLimitMinutes; set { if (SetField(ref _timeLimitMinutes, Math.Max(0, value))) { PushLiveSettings(); MarkDirty(); } } }

        private int _timeLimitSeconds;
        public int TimeLimitSeconds { get => _timeLimitSeconds; set { if (SetField(ref _timeLimitSeconds, Math.Max(0, value))) { PushLiveSettings(); MarkDirty(); } } }

        // === ЦЕЛЬ ===
        private int _zoneX;
        public int ZoneX { get => _zoneX; private set => SetField(ref _zoneX, value); }

        private int _zoneY;
        public int ZoneY { get => _zoneY; private set => SetField(ref _zoneY, value); }

        private int _zoneWidth;
        public int ZoneWidth { get => _zoneWidth; private set => SetField(ref _zoneWidth, value); }

        private int _zoneHeight;
        public int ZoneHeight { get => _zoneHeight; private set => SetField(ref _zoneHeight, value); }

        public bool HasZone => ZoneWidth > 0 && ZoneHeight > 0;
        public bool HasPoint => !HasZone && HasTargetAnchor;

        private bool _hasTargetAnchor;
        public bool HasTargetAnchor { get => _hasTargetAnchor; private set => SetField(ref _hasTargetAnchor, value); }

        private IntPtr _targetWindowHandle = IntPtr.Zero;
        public IntPtr TargetWindowHandle { get => _targetWindowHandle; private set => SetField(ref _targetWindowHandle, value); }

        private string _targetWindowTitle = string.Empty;
        public string TargetWindowTitle { get => _targetWindowTitle; private set => SetField(ref _targetWindowTitle, value); }

        public bool HasTargetWindow => TargetWindowHandle != IntPtr.Zero;
        public string WindowText => HasTargetWindow ? TargetWindowTitle : T("TargetNone");
        public string ZoneText => HasZone
            ? $"{ZoneWidth} × {ZoneHeight} px  ·  {ZoneX}, {ZoneY}"
            : (HasPoint ? $"{ZoneX}, {ZoneY} px" : T("TargetNone"));

        public string TargetSummaryText => HasTargetWindow
            ? $"{T("LabelWindow")}: {TargetWindowTitle}"
            : HasZone
                ? $"{T("LabelZone")}: {ZoneWidth} × {ZoneHeight}"
                : HasPoint
                    ? $"{T("LabelPoint")}: {ZoneX}, {ZoneY}"
                    : T("TargetNone");

        public string ModeText
        {
            get
            {
                if (ClickMode == ClickMode.Background) return T("ModeBackground");
                if (ClickMode == ClickMode.Game) return T("ModeGame");
                return T("ModeForeground");
            }
        }

        // === ГОРЯЧАЯ КЛАВИША ===
        private uint _hotkeyVk = 0x75;
        public uint HotkeyVk { get => _hotkeyVk; private set => SetField(ref _hotkeyVk, value); }

        private uint _hotkeyModifiers;
        public uint HotkeyModifiers { get => _hotkeyModifiers; private set => SetField(ref _hotkeyModifiers, value); }

        private string _hotkeyDisplay = "F6";
        public string HotkeyDisplay { get => _hotkeyDisplay; private set => SetField(ref _hotkeyDisplay, value); }

        private bool _hotkeyEnabled = true;
        public bool HotkeyEnabled { get => _hotkeyEnabled; private set => SetField(ref _hotkeyEnabled, value); }

        private bool _isListeningForHotkey;
        public bool IsListeningForHotkey { get => _isListeningForHotkey; set => SetField(ref _isListeningForHotkey, value); }

        // === ИНТЕРФЕЙС ===
        private string _uiLayout = "Classic";
        public string UiLayout
        {
            get => _uiLayout;
            set
            {
                if (!SetField(ref _uiLayout, value)) return;
                SyncOptionSelection(LayoutOptions, value);
                MarkDirty();
            }
        }

        private bool _isTopmost;
        public bool IsTopmost { get => _isTopmost; set { if (SetField(ref _isTopmost, value)) MarkDirty(); } }

        private double _windowOpacity = 1.0;
        public double WindowOpacity { get => _windowOpacity; set { if (SetField(ref _windowOpacity, Math.Max(0.4, Math.Min(1.0, value)))) MarkDirty(); } }

        private bool _minimizeToTray;
        public bool MinimizeToTray { get => _minimizeToTray; set { if (SetField(ref _minimizeToTray, value)) MarkDirty(); } }

        private bool _smoothScrolling = true;
        public bool SmoothScrolling { get => _smoothScrolling; set { if (SetField(ref _smoothScrolling, value)) MarkDirty(); } }

        // === ТЕМА ===
        private string _themeName = "midnight";
        public string ThemeName { get => _themeName; private set => SetField(ref _themeName, value); }

        private bool _isCustomTheme;
        public bool IsCustomTheme { get => _isCustomTheme; private set => SetField(ref _isCustomTheme, value); }

        // Кастомные цвета
        private Brush _customBackgroundBrush = ThemeManager.Brush(ThemeManager.Rgb(0x0B0F19));
        public Brush CustomBackgroundBrush
        {
            get => _customBackgroundBrush;
            set
            {
                if (ReferenceEquals(_customBackgroundBrush, value)) return;
                _customBackgroundBrush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomBackgroundHex));
                OnCustomColorsChanged();
            }
        }

        private Brush _customForegroundBrush = ThemeManager.Brush(ThemeManager.Rgb(0xE8EEFA));
        public Brush CustomForegroundBrush
        {
            get => _customForegroundBrush;
            set
            {
                if (ReferenceEquals(_customForegroundBrush, value)) return;
                _customForegroundBrush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomForegroundHex));
                OnCustomColorsChanged();
            }
        }

        private Brush _customBorderBrush = ThemeManager.Brush(ThemeManager.Rgb(0x26344C));
        public Brush CustomBorderBrush
        {
            get => _customBorderBrush;
            set
            {
                if (ReferenceEquals(_customBorderBrush, value)) return;
                _customBorderBrush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomBorderHex));
                OnCustomColorsChanged();
            }
        }

        private Brush _customControlBrush = ThemeManager.Brush(ThemeManager.Rgb(0x1B2437));
        public Brush CustomControlBrush
        {
            get => _customControlBrush;
            set
            {
                if (ReferenceEquals(_customControlBrush, value)) return;
                _customControlBrush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomControlHex));
                OnCustomColorsChanged();
            }
        }

        private Brush _customButtonBrush = ThemeManager.Brush(ThemeManager.Rgb(0x233049));
        public Brush CustomButtonBrush
        {
            get => _customButtonBrush;
            set
            {
                if (ReferenceEquals(_customButtonBrush, value)) return;
                _customButtonBrush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomButtonHex));
                OnCustomColorsChanged();
            }
        }

        private Brush _customAccentBrush = ThemeManager.Brush(ThemeManager.Rgb(0x4C8DFF));
        public Brush CustomAccentBrush
        {
            get => _customAccentBrush;
            set
            {
                if (ReferenceEquals(_customAccentBrush, value)) return;
                _customAccentBrush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomAccentHex));
                OnCustomColorsChanged();
            }
        }

        public string CustomBackgroundHex
        {
            get => ToHex(CustomBackgroundBrush);
            set { if (TryParseHex(value, out var c)) CustomBackgroundBrush = ThemeManager.Brush(c); }
        }

        public string CustomForegroundHex
        {
            get => ToHex(CustomForegroundBrush);
            set { if (TryParseHex(value, out var c)) CustomForegroundBrush = ThemeManager.Brush(c); }
        }

        public string CustomBorderHex
        {
            get => ToHex(CustomBorderBrush);
            set { if (TryParseHex(value, out var c)) CustomBorderBrush = ThemeManager.Brush(c); }
        }

        public string CustomControlHex
        {
            get => ToHex(CustomControlBrush);
            set { if (TryParseHex(value, out var c)) CustomControlBrush = ThemeManager.Brush(c); }
        }

        public string CustomButtonHex
        {
            get => ToHex(CustomButtonBrush);
            set { if (TryParseHex(value, out var c)) CustomButtonBrush = ThemeManager.Brush(c); }
        }

        public string CustomAccentHex
        {
            get => ToHex(CustomAccentBrush);
            set { if (TryParseHex(value, out var c)) CustomAccentBrush = ThemeManager.Brush(c); }
        }

        private bool _useCustomBackground = true;
        public bool UseCustomBackground { get => _useCustomBackground; set { if (SetField(ref _useCustomBackground, value)) OnCustomColorsChanged(); } }

        private bool _useCustomForeground = true;
        public bool UseCustomForeground { get => _useCustomForeground; set { if (SetField(ref _useCustomForeground, value)) OnCustomColorsChanged(); } }

        private bool _useCustomBorder = true;
        public bool UseCustomBorder { get => _useCustomBorder; set { if (SetField(ref _useCustomBorder, value)) OnCustomColorsChanged(); } }

        private bool _useCustomControl = true;
        public bool UseCustomControl { get => _useCustomControl; set { if (SetField(ref _useCustomControl, value)) OnCustomColorsChanged(); } }

        private bool _useCustomButton = true;
        public bool UseCustomButton { get => _useCustomButton; set { if (SetField(ref _useCustomButton, value)) OnCustomColorsChanged(); } }

        private bool _useCustomAccent = true;
        public bool UseCustomAccent { get => _useCustomAccent; set { if (SetField(ref _useCustomAccent, value)) OnCustomColorsChanged(); } }

        // === СОХРАНЁННЫЕ ТЕМЫ ===
        public ObservableCollection<string> SavedConfigs { get; } = new ObservableCollection<string>();

        private string _themeConfigName = "My theme";
        public string ThemeConfigName { get => _themeConfigName; set => SetField(ref _themeConfigName, value); }

        // === СОСТОЯНИЕ ===
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (!SetField(ref _isRunning, value)) return;
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

        private long _clicksDone;
        public long ClicksDone { get => _clicksDone; private set => SetField(ref _clicksDone, value); }

        private double _cps;
        public double Cps { get => _cps; private set => SetField(ref _cps, Math.Round(value, 1)); }

        // Мини-график CPS для сайдбара
        private readonly Queue<double> _cpsHistory = new Queue<double>();
        private PointCollection _cpsPoints = new PointCollection();
        public PointCollection CpsPoints { get => _cpsPoints; private set => SetField(ref _cpsPoints, value); }
        private string _elapsedText = "00:00";
        public string ElapsedText { get => _elapsedText; private set => SetField(ref _elapsedText, value); }

        // === КОМАНДЫ ===
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand SelectAreaCommand { get; }
        public RelayCommand SelectPointCommand { get; }
        public RelayCommand SelectWindowCommand { get; }
        public RelayCommand TestClickCommand { get; }
        public RelayCommand SetHotkeyCommand { get; }
        public RelayCommand SaveThemeCommand { get; }
        public RelayCommand LoadThemeCommand { get; }
        public RelayCommand DeleteThemeCommand { get; }
        public RelayCommand<SelectableItem> SelectIntervalCommand { get; }
        public RelayCommand<SelectableItem> SelectMouseButtonCommand { get; }
        public RelayCommand<SelectableItem> SelectModeCommand { get; }
        public RelayCommand<SelectableItem> SelectLanguageCommand { get; }
        public RelayCommand<SelectableItem> SelectLayoutCommand { get; }
        public RelayCommand<ThemePreset> SelectThemeCommand { get; }
        public RelayCommand<string> PickColorCommand { get; }

        // === СОБЫТИЯ ===
        public event Action OnSelectArea;
        public event Action OnSelectPoint;
        public event Action OnSelectWindow;
        public event Action OnHotkeyCaptureRequested;
        public event Action OnHotkeyChanged;

        private readonly ClickerEngine _engine = new ClickerEngine();
        private readonly DispatcherTimer _statsTimer;
        private readonly DispatcherTimer _saveTimer;
        private bool _loading;
        private bool _dirty;

        public MainViewModel()
        {
            StartCommand = new RelayCommand(StartEngine, () => !IsRunning);
            StopCommand = new RelayCommand(StopEngine, () => IsRunning);
            SelectAreaCommand = new RelayCommand(() => OnSelectArea?.Invoke());
            SelectPointCommand = new RelayCommand(() => OnSelectPoint?.Invoke());
            SelectWindowCommand = new RelayCommand(() => OnSelectWindow?.Invoke());
            TestClickCommand = new RelayCommand(TestClick);
            SetHotkeyCommand = new RelayCommand(BeginHotkeyCapture);
            SaveThemeCommand = new RelayCommand(SaveThemeConfig);
            LoadThemeCommand = new RelayCommand(LoadThemeConfig);
            DeleteThemeCommand = new RelayCommand(DeleteThemeConfig);
            SelectIntervalCommand = new RelayCommand<SelectableItem>(SelectInterval);
            SelectMouseButtonCommand = new RelayCommand<SelectableItem>(SelectMouseButton);
            SelectModeCommand = new RelayCommand<SelectableItem>(SelectMode);
            SelectLanguageCommand = new RelayCommand<SelectableItem>(SelectLanguage);
            SelectLayoutCommand = new RelayCommand<SelectableItem>(SelectLayout);
            SelectThemeCommand = new RelayCommand<ThemePreset>(SelectTheme);
            PickColorCommand = new RelayCommand<string>(PickColor);

            CustomThemePreset = new ThemePreset
            {
                Name = "custom",
                IsCustom = true,
                DisplayName = "Custom"
            };

            BuildThemeList();
            BuildIntervalPresets();
            BuildMouseButtons();
            BuildModes();
            BuildLanguages();
            BuildLayouts();

            _statsTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _statsTimer.Tick += (s, e) => UpdateStats();

            _saveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(700)
            };
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop();
                if (_dirty && !_loading) SaveSettingsNow();
            };

            _engine.Stopped += OnEngineStopped;

            ApplySettings(SettingsManager.Load());
            RebuildLocalizedLabels();
            SyncIntervalChips();
            ReloadSavedThemes();
            ResetSparkline();
            StatusMessage = T("StatusReady");
        }

        // === ХЕЛПЕРЫ ===

        private string T(string key) => LanguageManager.GetString(CurrentLanguage, key);

        private void BuildThemeList()
        {
            Themes.Clear();
            foreach (var preset in ThemeManager.CreatePresets())
                Themes.Add(preset);
            Themes.Add(CustomThemePreset);
        }

        private void BuildIntervalPresets()
        {
            IntervalPresets.Clear();
            IntervalPresets.Add(new SelectableItem { Label = T("IntervalTurbo"), Tag = 0 });

            foreach (int ms in new[] { 1, 10, 50, 100, 250, 500, 1000 })
            {
                string label = ms >= 1000 ? $"1 {T("UnitSeconds")}" : $"{ms} {T("UnitMs")}";
                IntervalPresets.Add(new SelectableItem { Label = label, Tag = ms });
            }
            SyncIntervalChips();
        }

        private void BuildMouseButtons()
        {
            MouseButtonOptions.Clear();
            MouseButtonOptions.Add(new SelectableItem { Label = T("MouseLeft"), Tag = MouseButtonKind.Left });
            MouseButtonOptions.Add(new SelectableItem { Label = T("MouseRight"), Tag = MouseButtonKind.Right });
            MouseButtonOptions.Add(new SelectableItem { Label = T("MouseMiddle"), Tag = MouseButtonKind.Middle });
            SyncOptionSelection(MouseButtonOptions, MouseButton);
        }

        private void BuildModes()
        {
            ModeOptions.Clear();
            ModeOptions.Add(new SelectableItem
            {
                Label = T("ModeForeground"),
                Description = T("ModeForegroundDesc"),
                Tag = ClickMode.Foreground
            });
            ModeOptions.Add(new SelectableItem
            {
                Label = T("ModeBackground"),
                Description = T("ModeBackgroundDesc"),
                Tag = ClickMode.Background
            });
            ModeOptions.Add(new SelectableItem
            {
                Label = T("ModeGame"),
                Description = T("ModeGameDesc"),
                Tag = ClickMode.Game
            });
            SyncOptionSelection(ModeOptions, ClickMode);
        }

        private void BuildLanguages()
        {
            LanguageOptions.Clear();
            foreach (var lang in LanguageManager.AvailableLanguages)
                LanguageOptions.Add(new SelectableItem { Label = lang, Tag = lang });
            SyncOptionSelection(LanguageOptions, CurrentLanguage);
        }


        private void BuildLayouts()
        {
            LayoutOptions.Clear();
            LayoutOptions.Add(new SelectableItem { Label = T("LayoutClassic"), Description = "214 px sidebar", Tag = "Classic" });
            LayoutOptions.Add(new SelectableItem { Label = T("LayoutCompact"), Description = "64 px icon rail", Tag = "Compact" });
            LayoutOptions.Add(new SelectableItem { Label = T("LayoutMinimal"), Description = "top navigation", Tag = "Minimal" });
            SyncOptionSelection(LayoutOptions, UiLayout);
        }

        private void SelectLayout(SelectableItem item)
        {
            if (item?.Tag is string layout) UiLayout = layout;
        }
        private void RebuildLocalizedLabels()
        {
            CustomThemePreset.DisplayName = T("ThemeCustom");
            BuildIntervalPresets();
            BuildMouseButtons();
            BuildModes();
            BuildLanguages();
            BuildLayouts();
            OnPropertyChanged(nameof(WindowText));
            OnPropertyChanged(nameof(ZoneText));
            OnPropertyChanged(nameof(IntervalText));
            OnPropertyChanged(nameof(StatusMessage));
        }

        private static void SyncOptionSelection<T>(IEnumerable<SelectableItem> items, T selected)
        {
            foreach (var item in items)
                item.IsSelected = item.Tag is T tag && Equals(tag, selected);
        }

        private void SelectInterval(SelectableItem item)
        {
            if (item?.Tag is int ms)
            {
                _loading = true;
                IntervalHours = 0;
                IntervalMinutes = 0;
                IntervalSeconds = 0;
                IntervalMs = ms;
                _loading = false;
                SyncIntervalChips();
                PushLiveSettings();
                MarkDirty();
            }
        }

        private void SelectMouseButton(SelectableItem item)
        {
            if (item?.Tag is MouseButtonKind kind) MouseButton = kind;
        }

        private void SelectMode(SelectableItem item)
        {
            if (item?.Tag is ClickMode mode) ClickMode = mode;
        }

        private void SelectLanguage(SelectableItem item)
        {
            if (item?.Tag is string lang) CurrentLanguage = lang;
        }

        private void SelectTheme(ThemePreset preset)
        {
            if (preset == null) return;
            if (preset.IsCustom)
            {
                RebuildCustomPreset();
                ApplyTheme(CustomThemePreset);
            }
            else
            {
                ApplyTheme(preset);
            }
        }

        public void ApplyTheme(ThemePreset preset, bool persist = true)
        {
            if (preset == null) return;
            ThemeManager.Apply(preset);

            foreach (var theme in Themes)
                theme.IsSelected = ReferenceEquals(theme, preset) || theme.Name == preset.Name;

            IsCustomTheme = preset.IsCustom;
            ThemeName = preset.Name;

            if (persist)
            {
                StatusMessage = T("StatusThemeApplied");
                MarkDirty();
            }
        }

        // === КАСТОМНЫЕ ЦВЕТА ===

        private void OnCustomColorsChanged()
        {
            if (_loading) return;
            RebuildCustomPreset();
            ApplyTheme(CustomThemePreset);
        }

        private void RebuildCustomPreset()
        {
            Color bg = BrushColor(CustomBackgroundBrush, ThemeManager.Rgb(0x0B0F19));
            Color fg = UseCustomForeground ? BrushColor(CustomForegroundBrush, ThemeManager.Rgb(0xE8EEFA)) : ThemeManager.Rgb(0xE8EEFA);
            Color border = UseCustomBorder ? BrushColor(CustomBorderBrush, ThemeManager.Rgb(0x26344C)) : ThemeManager.Rgb(0x26344C);
            Color control = UseCustomControl ? BrushColor(CustomControlBrush, ThemeManager.Rgb(0x1B2437)) : ThemeManager.Rgb(0x1B2437);
            Color button = UseCustomButton ? BrushColor(CustomButtonBrush, ThemeManager.Rgb(0x233049)) : ThemeManager.Rgb(0x233049);
            Color accent = UseCustomAccent ? BrushColor(CustomAccentBrush, ThemeManager.Rgb(0x4C8DFF)) : ThemeManager.Rgb(0x4C8DFF);

            bool dark = ThemeManager.Luminance(bg) < 0.5;
            Color accentSecondary = dark ? ThemeManager.Lighten(accent, 0.35) : ThemeManager.Darken(accent, 0.25);
            Color success = dark ? ThemeManager.Rgb(0x2DD4A7) : ThemeManager.Rgb(0x0EA472);
            Color danger = dark ? ThemeManager.Rgb(0xFF5C7A) : ThemeManager.Rgb(0xDC2626);

            var preset = ThemeManager.BuildCustom(
                bg, fg, border, control, button, accent,
                ThemeManager.Mix(bg, fg, 0.045),
                ThemeManager.Mix(bg, fg, 0.085),
                accentSecondary, success, danger);

            CustomThemePreset.CopyColorsFrom(preset);
            CustomThemePreset.DisplayName = T("ThemeCustom");
        }

        private void PickColor(string kind)
        {
            Brush currentBrush;
            switch (kind)
            {
                case "Foreground": currentBrush = CustomForegroundBrush; break;
                case "Border": currentBrush = CustomBorderBrush; break;
                case "Control": currentBrush = CustomControlBrush; break;
                case "Button": currentBrush = CustomButtonBrush; break;
                case "Accent": currentBrush = CustomAccentBrush; break;
                default: currentBrush = CustomBackgroundBrush; break;
            }

            var current = BrushColor(currentBrush, Colors.White);

            var picker = new ColorPickerWindow(current, CurrentLanguage);
            var owner = Application.Current?.MainWindow;
            if (owner != null && owner.IsVisible) picker.Owner = owner;

            if (picker.ShowDialog() != true) return;

            var brush = ThemeManager.Brush(picker.SelectedColor);

            switch (kind)
            {
                case "Foreground": CustomForegroundBrush = brush; break;
                case "Border": CustomBorderBrush = brush; break;
                case "Control": CustomControlBrush = brush; break;
                case "Button": CustomButtonBrush = brush; break;
                case "Accent": CustomAccentBrush = brush; break;
                default: CustomBackgroundBrush = brush; break;
            }
        }

        private static Color BrushColor(Brush brush, Color fallback)
            => brush is SolidColorBrush solid ? solid.Color : fallback;

        private static string ToHex(Brush brush)
        {
            var color = BrushColor(brush, Colors.Transparent);
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static bool TryParseHex(string text, out Color color)
        {
            color = Colors.Transparent;
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                var parsed = (Color)ColorConverter.ConvertFromString(text.Trim());
                color = parsed;
                return true;
            }
            catch { return false; }
        }

        // === СОХРАНЁННЫЕ ТЕМЫ ===

        private void SaveThemeConfig()
        {
            var preset = ThemeManager.Current ?? CustomThemePreset;
            string name = string.IsNullOrWhiteSpace(ThemeConfigName) ? "My theme" : ThemeConfigName.Trim();
            var config = ThemeConfig.FromPreset(preset, name);
            config.UseBackground = UseCustomBackground;
            config.UseForeground = UseCustomForeground;
            config.UseBorder = UseCustomBorder;
            config.UseControlBg = UseCustomControl;
            config.UseButtonBg = UseCustomButton;
            config.UseAccent = UseCustomAccent;

            SettingsManager.SaveThemeConfig(config);
            ThemeConfigName = name;
            ReloadSavedThemes();
            StatusMessage = T("StatusSaved");
        }

        private void LoadThemeConfig()
        {
            if (string.IsNullOrWhiteSpace(ThemeConfigName)) return;
            var config = SettingsManager.LoadThemeConfig(ThemeConfigName);
            if (config == null) return;

            CustomBackgroundBrush = ThemeManager.Brush(config.Background);
            CustomForegroundBrush = ThemeManager.Brush(config.Foreground);
            CustomBorderBrush = ThemeManager.Brush(config.Border);
            CustomControlBrush = ThemeManager.Brush(config.Control);
            CustomButtonBrush = ThemeManager.Brush(config.Button);
            CustomAccentBrush = ThemeManager.Brush(config.Accent);

            RebuildCustomPreset();
            ApplyTheme(CustomThemePreset);
            ReloadSavedThemes();
        }

        private void DeleteThemeConfig()
        {
            if (string.IsNullOrWhiteSpace(ThemeConfigName)) return;
            if (MessageBox.Show(T("MsgConfirmDelete"), "AutoClicker", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            SettingsManager.DeleteThemeConfig(ThemeConfigName);
            ReloadSavedThemes();
            StatusMessage = T("StatusDeleted");
        }

        private void ReloadSavedThemes()
        {
            SavedConfigs.Clear();
            foreach (var name in SettingsManager.GetSavedThemeConfigs())
                SavedConfigs.Add(name);
        }

        // === ДВИЖОК ===

        private void StartEngine()
        {
            if (IsRunning) return;

            ConfigureEngine();
            ClicksDone = 0;
            Cps = 0;
            ElapsedText = "00:00";
            ResetSparkline();

            _engine.Start();
            IsRunning = true;
            StatusMessage = T("StatusRunning");
            _statsTimer.Start();

            if (ClickMode == ClickMode.Background && !HasTargetWindow)
                StatusMessage = T("StatusWindowMissing");
            else if (ClickMode == ClickMode.Game && !HasTargetWindow)
                StatusMessage = T("StatusWindowMissing");
        }

        private void StopEngine()
        {
            if (!IsRunning) return;
            _engine.Stop();
        }

        public void ToggleRun()
        {
            if (IsRunning) StopEngine();
            else StartEngine();
        }

        private void TestClick()
        {
            ConfigureEngine();
            _engine.ClickOnce();
            StatusMessage = T("StatusTest");
        }

        private void OnEngineStopped(string reason)
        {
            var app = Application.Current;
            if (app == null) return;

            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                _statsTimer.Stop();
                IsRunning = false;
                ClicksDone = _engine.ClicksDone;
                Cps = 0;
                UpdateStats();

                if (reason == "limit") StatusMessage = T("StatusLimit");
                else if (reason == "time") StatusMessage = T("StatusTime");
                else if (reason == "error") StatusMessage = T("StatusError");
                else StatusMessage = T("StatusStopped");

                MarkDirty();
            }));
        }

        private void UpdateStats()
        {
            ClicksDone = _engine.ClicksDone;
            Cps = _engine.Cps;
            UpdateCpsSparkline(Cps);

            var elapsed = _engine.Elapsed;
            ElapsedText = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        private void UpdateCpsSparkline(double cps)
        {
            _cpsHistory.Enqueue(cps);
            while (_cpsHistory.Count > 48) _cpsHistory.Dequeue();
            RebuildSparklinePoints();
        }

        private void ResetSparkline()
        {
            _cpsHistory.Clear();
            for (int i = 0; i < 48; i++) _cpsHistory.Enqueue(0);
            RebuildSparklinePoints();
        }

        private void RebuildSparklinePoints()
        {
            const double width = 168.0;
            const double height = 46.0;

            double max = 10.0;
            foreach (var value in _cpsHistory)
                if (value > max) max = value;

            var points = new PointCollection();
            int count = _cpsHistory.Count;
            int index = 0;

            foreach (var value in _cpsHistory)
            {
                double x = count <= 1 ? 0 : index * width / (count - 1);
                double y = height - 2 - (value / max) * (height - 6);
                points.Add(new Point(x, y));
                index++;
            }

            CpsPoints = points;
        }
        private void ConfigureEngine()
        {
            _engine.IntervalMs = IntervalHours * 3600000 + IntervalMinutes * 60000 + IntervalSeconds * 1000 + IntervalMs;
            _engine.UseCustomInterval = UseCustomInterval;
            _engine.CustomInterval = CustomInterval;
            _engine.Button = MouseButton;
            _engine.Mode = ClickMode;
            _engine.ZoneX = ZoneX;
            _engine.ZoneY = ZoneY;
            _engine.ZoneWidth = ZoneWidth;
            _engine.ZoneHeight = ZoneHeight;
            _engine.TargetWindow = TargetWindowHandle;
            _engine.EnableCyclicMode = EnableCyclicMode;
            _engine.ClickDuration = ClickDuration;
            _engine.RestDuration = RestDuration;
            _engine.EnableRandomization = EnableRandomization;
            _engine.RandomOffsetX = RandomOffsetX;
            _engine.RandomOffsetY = RandomOffsetY;
            _engine.EnableHumanization = EnableHumanization;
            _engine.HumanizePercent = HumanizePercent;
            _engine.ClickHoldMs = ClickHoldMs;
            _engine.ReturnCursor = ReturnCursor;
            _engine.EnableClickLimit = EnableClickLimit;
            _engine.MaxClicks = MaxClicks;
            _engine.EnableTimeLimit = EnableTimeLimit;
            _engine.TimeLimitHours = TimeLimitHours;
            _engine.TimeLimitMinutes = TimeLimitMinutes;
            _engine.TimeLimitSeconds = TimeLimitSeconds;
        }

        private void PushLiveSettings()
        {
            if (_loading || _engine == null || !IsRunning) return;
            ConfigureEngine();
        }

        // === ЦЕЛЬ (вызывается из окна) ===

        public void UpdateZone(int x, int y, int width, int height)
        {
            ZoneX = x;
            ZoneY = y;
            ZoneWidth = Math.Max(0, width);
            ZoneHeight = Math.Max(0, height);
            HasTargetAnchor = false;
            OnTargetChanged();
            StatusMessage = $"{T("StatusZoneSelected")}: {ZoneWidth} × {ZoneHeight}";
        }

        public void UpdatePoint(int x, int y)
        {
            ZoneX = x;
            ZoneY = y;
            ZoneWidth = 0;
            ZoneHeight = 0;
            HasTargetAnchor = true;
            OnTargetChanged();
            StatusMessage = $"{T("StatusPointSelected")}: {x}, {y}";
        }

        public void UpdateWindow(IntPtr handle, string title)
        {
            TargetWindowHandle = handle;
            TargetWindowTitle = string.IsNullOrWhiteSpace(title) ? $"HWND {handle.ToInt64():X}" : title;
            OnTargetChanged();
            StatusMessage = $"{T("StatusWindowSelected")}: {TargetWindowTitle}";
        }

        public void ClearTarget()
        {
            ZoneWidth = 0;
            ZoneHeight = 0;
            HasTargetAnchor = false;
            TargetWindowHandle = IntPtr.Zero;
            TargetWindowTitle = string.Empty;
            OnTargetChanged();
        }

        private void OnTargetChanged()
        {
            OnPropertyChanged(nameof(HasZone));
            OnPropertyChanged(nameof(HasPoint));
            OnPropertyChanged(nameof(HasTargetWindow));
            OnPropertyChanged(nameof(ZoneText));
            OnPropertyChanged(nameof(WindowText));
            OnPropertyChanged(nameof(TargetSummaryText));
            MarkDirty();
        }

        // === ГОРЯЧАЯ КЛАВИША ===

        private void BeginHotkeyCapture()
        {
            IsListeningForHotkey = true;
            StatusMessage = T("StatusCapture");
            OnHotkeyCaptureRequested?.Invoke();
        }

        public void CommitHotkey(uint modifiers, uint vk, string display)
        {
            HotkeyVk = vk;
            HotkeyModifiers = modifiers;
            HotkeyDisplay = display;
            HotkeyEnabled = true;
            IsListeningForHotkey = false;
            StatusMessage = $"{T("SectionHotkey")}: {display}";
            OnHotkeyChanged?.Invoke();
            MarkDirty();
        }

        public void DisableHotkey()
        {
            HotkeyVk = 0;
            HotkeyModifiers = 0;
            HotkeyDisplay = T("HotkeyOff");
            HotkeyEnabled = false;
            IsListeningForHotkey = false;
            StatusMessage = T("StatusHotkeyOff");
            OnHotkeyChanged?.Invoke();
            MarkDirty();
        }

        public void CancelHotkeyCapture()
        {
            IsListeningForHotkey = false;
            StatusMessage = T("StatusReady");
        }

        // === НАСТРОЙКИ ===

        private void ApplySettings(AppSettings s)
        {
            if (s == null) return;

            _loading = true;
            try
            {
                if (LanguageManager.IsSupported(s.Language)) _currentLanguage = s.Language;

                IntervalHours = s.IntervalHours;
                IntervalMinutes = s.IntervalMinutes;
                IntervalSeconds = s.IntervalSeconds;
                IntervalMs = s.IntervalMs;
                _mouseButton = s.MouseButton;
                _clickMode = s.Mode;
                UseCustomInterval = s.UseCustomInterval;
                CustomInterval = s.CustomInterval;
                ClickHoldMs = s.ClickHoldMs;
                ReturnCursor = s.ReturnCursor;

                EnableCyclicMode = s.EnableCyclicMode;
                ClickDuration = s.ClickDuration;
                RestDuration = s.RestDuration;
                EnableRandomization = s.EnableRandomization;
                RandomOffsetX = s.RandomOffsetX;
                RandomOffsetY = s.RandomOffsetY;
                EnableHumanization = s.EnableHumanization;
                HumanizePercent = s.HumanizePercent;

                EnableClickLimit = s.EnableClickLimit;
                MaxClicks = s.MaxClicks;
                EnableTimeLimit = s.EnableTimeLimit;
                TimeLimitHours = s.TimeLimitHours;
                TimeLimitMinutes = s.TimeLimitMinutes;
                TimeLimitSeconds = s.TimeLimitSeconds;

                ZoneX = s.ZoneX;
                ZoneY = s.ZoneY;
                ZoneWidth = s.ZoneWidth;
                ZoneHeight = s.ZoneHeight;
                HasTargetAnchor = s.ZoneWidth == 0 && s.ZoneHeight == 0 && (s.ZoneX != 0 || s.ZoneY != 0);
                _targetWindowTitle = s.TargetWindowTitle ?? string.Empty;
                _hasTargetAnchor = HasTargetAnchor;

                HotkeyVk = s.HotkeyVk;
                HotkeyModifiers = s.HotkeyModifiers;
                HotkeyEnabled = s.HotkeyEnabled;
                HotkeyDisplay = HotkeyEnabled ? (string.IsNullOrWhiteSpace(s.HotkeyDisplay)
                    ? HotkeyManager.Format(s.HotkeyModifiers, s.HotkeyVk)
                    : s.HotkeyDisplay) : T("HotkeyOff");

                IsTopmost = s.IsTopmost;
                _windowOpacity = Math.Max(0.4, Math.Min(1.0, s.WindowOpacity));
                MinimizeToTray = s.MinimizeToTray;
                SmoothScrolling = s.SmoothScrolling;
                UiLayout = string.IsNullOrWhiteSpace(s.UiLayout) ? "Classic" : s.UiLayout;

                UseCustomBackground = s.UseCustomBackground;
                UseCustomForeground = s.UseCustomForeground;
                UseCustomBorder = s.UseCustomBorder;
                UseCustomControl = s.UseCustomControl;
                UseCustomButton = s.UseCustomButton;
                UseCustomAccent = s.UseCustomAccent;

                _customBackgroundBrush = ParseBrush(s.CustomBackground, 0x0B0F19);
                _customForegroundBrush = ParseBrush(s.CustomForeground, 0xE8EEFA);
                _customBorderBrush = ParseBrush(s.CustomBorder, 0x223049);
                _customControlBrush = ParseBrush(s.CustomControl, 0x1A2335);
                _customButtonBrush = ParseBrush(s.CustomButton, 0x212C42);
                _customAccentBrush = ParseBrush(s.CustomAccent, 0x4C8DFF);

                OnPropertyChanged(nameof(CustomBackgroundBrush));
                OnPropertyChanged(nameof(CustomForegroundBrush));
                OnPropertyChanged(nameof(CustomBorderBrush));
                OnPropertyChanged(nameof(CustomControlBrush));
                OnPropertyChanged(nameof(CustomButtonBrush));
                OnPropertyChanged(nameof(CustomAccentBrush));
                OnPropertyChanged(nameof(CustomBackgroundHex));
                OnPropertyChanged(nameof(CustomForegroundHex));
                OnPropertyChanged(nameof(CustomBorderHex));
                OnPropertyChanged(nameof(CustomControlHex));
                OnPropertyChanged(nameof(CustomButtonHex));
                OnPropertyChanged(nameof(CustomAccentHex));

                ThemeName = s.ThemeName;
                _isCustomTheme = s.IsCustomTheme;

                SyncOptionSelection(MouseButtonOptions, _mouseButton);
                SyncOptionSelection(ModeOptions, _clickMode);
                SyncOptionSelection(LanguageOptions, _currentLanguage);

                // Применяем сохранённую тему
                if (s.IsCustomTheme)
                {
                    RebuildCustomPreset();
                    ApplyTheme(CustomThemePreset, false);
                }
                else
                {
                    var preset = Themes.FirstOrDefault(t => !t.IsCustom && t.Name == s.ThemeName);
                    if (preset != null) ApplyTheme(preset, false);
                    else ApplyTheme(CustomThemePreset, false);
                }

                OnPropertyChanged(nameof(HasZone));
                OnPropertyChanged(nameof(HasPoint));
                OnPropertyChanged(nameof(HasTargetWindow));
                OnPropertyChanged(nameof(ZoneText));
                OnPropertyChanged(nameof(WindowText));
                OnPropertyChanged(nameof(IntervalText));
                OnPropertyChanged("Item[]");
            }
            finally
            {
                _loading = false;
            }
        }

        private static Brush ParseBrush(string hex, int fallbackRgb)
        {
            if (!string.IsNullOrWhiteSpace(hex) && TryParseHex(hex, out var color))
                return ThemeManager.Brush(color);
            return ThemeManager.Brush(ThemeManager.Rgb(fallbackRgb));
        }

        public AppSettings BuildSettings()
        {
            return new AppSettings
            {
                IntervalHours = IntervalHours,
                IntervalMinutes = IntervalMinutes,
                IntervalSeconds = IntervalSeconds,
                IntervalMs = IntervalMs,
                MouseButton = MouseButton,
                Mode = ClickMode,
                UseCustomInterval = UseCustomInterval,
                CustomInterval = CustomInterval,
                ClickHoldMs = ClickHoldMs,
                ReturnCursor = ReturnCursor,
                EnableCyclicMode = EnableCyclicMode,
                ClickDuration = ClickDuration,
                RestDuration = RestDuration,
                EnableRandomization = EnableRandomization,
                RandomOffsetX = RandomOffsetX,
                RandomOffsetY = RandomOffsetY,
                EnableHumanization = EnableHumanization,
                HumanizePercent = HumanizePercent,
                EnableClickLimit = EnableClickLimit,
                MaxClicks = MaxClicks,
                EnableTimeLimit = EnableTimeLimit,
                TimeLimitHours = TimeLimitHours,
                TimeLimitMinutes = TimeLimitMinutes,
                TimeLimitSeconds = TimeLimitSeconds,
                ZoneX = ZoneX,
                ZoneY = ZoneY,
                ZoneWidth = ZoneWidth,
                ZoneHeight = ZoneHeight,
                TargetWindowTitle = TargetWindowTitle,
                HotkeyVk = HotkeyVk,
                HotkeyModifiers = HotkeyModifiers,
                HotkeyDisplay = HotkeyEnabled ? HotkeyManager.Format(HotkeyModifiers, HotkeyVk) : string.Empty,
                HotkeyEnabled = HotkeyEnabled,
                Language = CurrentLanguage,
                ThemeName = ThemeName,
                IsCustomTheme = IsCustomTheme,
                IsTopmost = IsTopmost,
                WindowOpacity = WindowOpacity,
                MinimizeToTray = MinimizeToTray,
                SmoothScrolling = SmoothScrolling,
                UiLayout = UiLayout,
                CustomBackground = ToHex(CustomBackgroundBrush),
                CustomForeground = ToHex(CustomForegroundBrush),
                CustomBorder = ToHex(CustomBorderBrush),
                CustomControl = ToHex(CustomControlBrush),
                CustomButton = ToHex(CustomButtonBrush),
                CustomAccent = ToHex(CustomAccentBrush),
                UseCustomBackground = UseCustomBackground,
                UseCustomForeground = UseCustomForeground,
                UseCustomBorder = UseCustomBorder,
                UseCustomControl = UseCustomControl,
                UseCustomButton = UseCustomButton,
                UseCustomAccent = UseCustomAccent
            };
        }

        public void SaveSettingsNow()
        {
            _dirty = false;
            SettingsManager.Save(BuildSettings());
        }

        private void MarkDirty()
        {
            if (_loading) return;
            _dirty = true;
            _saveTimer?.Stop();
            _saveTimer?.Start();
        }
    }
}
