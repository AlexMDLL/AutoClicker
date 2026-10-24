using System.Windows.Media;

namespace AutoClicker
{
    /// <summary>Готовая цветовая схема интерфейса.</summary>
    public class ThemePreset : BaseViewModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsDark { get; set; } = true;
        public bool IsCustom { get; set; }

        public Color Background { get; set; }
        public Color Surface { get; set; }
        public Color Card { get; set; }
        public Color Control { get; set; }
        public Color Button { get; set; }
        public Color Foreground { get; set; }
        public Color Muted { get; set; }
        public Color Border { get; set; }
        public Color Accent { get; set; }
        public Color AccentSecondary { get; set; }
        public Color Success { get; set; }
        public Color Danger { get; set; }

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }

        /// <summary>Мини-превью темы для плитки в настройках.</summary>
        public Brush PreviewBrush
        {
            get
            {
                var brush = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 1)
                };
                brush.GradientStops.Add(new GradientStop(Background, 0));
                brush.GradientStops.Add(new GradientStop(Card, 0.55));
                brush.GradientStops.Add(new GradientStop(Accent, 1));
                brush.Freeze();
                return brush;
            }
        }

        /// <summary>Точка-акцент на плитке.</summary>
        public Brush AccentDot
        {
            get
            {
                var brush = new SolidColorBrush(AccentSecondary);
                brush.Freeze();
                return brush;
            }
        }

        public void RefreshVisuals()
        {
            OnPropertyChanged(nameof(PreviewBrush));
            OnPropertyChanged(nameof(AccentDot));
            OnPropertyChanged(nameof(DisplayName));
        }

        public void CopyColorsFrom(ThemePreset other)
        {
            Background = other.Background;
            Surface = other.Surface;
            Card = other.Card;
            Control = other.Control;
            Button = other.Button;
            Foreground = other.Foreground;
            Muted = other.Muted;
            Border = other.Border;
            Accent = other.Accent;
            AccentSecondary = other.AccentSecondary;
            Success = other.Success;
            Danger = other.Danger;
            IsDark = other.IsDark;
            RefreshVisuals();
        }

        public ThemePreset Clone()
        {
            var clone = new ThemePreset
            {
                Name = Name,
                DisplayName = DisplayName,
                IsDark = IsDark,
                IsCustom = IsCustom
            };
            clone.CopyColorsFrom(this);
            return clone;
        }
    }
}
