using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoClicker
{
    /// <summary>
    /// Современный выбор цвета в стиле приложения: поле насыщенности/яркости,
    /// слайдер оттенка, HEX/RGB/HSL/CMYK с копированием и превью.
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        private readonly string _language;
        private bool _updating;

        private double _hue;
        private double _saturation;
        private double _value = 1.0;

        public Color SelectedColor { get; private set; }

        public ColorPickerWindow(Color initialColor, string language)
        {
            InitializeComponent();

            _language = language;
            ApplyLocalization();

            PreviewOld.Background = new SolidColorBrush(initialColor);
            SetColor(initialColor, true);

            Loaded += (s, e) => PositionMarkers();
        }

        private void ApplyLocalization()
        {
            TitleText.Text = T("ColorPickerTitle");
            ApplyButton.Content = T("BtnApply");
            CancelButton.Content = T("BtnCancel");
            OldLabel.Text = T("PreviewOld");
            NewLabel.Text = T("PreviewNew");
            HintText.Text = T("ColorPickerHint");
        }

        private string T(string key) => LanguageManager.GetString(_language, key);

        // ===================== ВЫБОР ЦВЕТА =====================

        private void SetColor(Color color, bool updateText)
        {
            var (h, s, v) = ColorToHsv(color);
            if (s > 0.0001) _hue = h; // для серого оттенок не определён — сохраняем прежний
            _saturation = s;
            _value = v;
            ApplyHsv(updateText);
        }

        private void ApplyHsv(bool updateText)
        {
            Color color = HsvToColor(_hue, _saturation, _value);
            _updating = true;
            try
            {
                HueBase.Fill = new SolidColorBrush(HsvToColor(_hue, 1.0, 1.0));

                if (updateText)
                {
                    HexBox.Text = ToHex(color);
                    RBox.Text = color.R.ToString();
                    GBox.Text = color.G.ToString();
                    BBox.Text = color.B.ToString();
                    HslBox.Text = ToHsl(color);
                    CmykBox.Text = ToCmyk(color);
                }

                PreviewNew.Background = new SolidColorBrush(color);
                SelectedColor = color;
                PositionMarkers();
            }
            finally
            {
                _updating = false;
            }
        }

        private void PositionMarkers()
        {
            double width = SvSurface.ActualWidth;
            double height = SvSurface.ActualHeight;
            if (width > 1 && height > 1)
            {
                double x = _saturation * width - SvMarker.Width / 2;
                double y = (1.0 - _value) * height - SvMarker.Height / 2;
                Canvas.SetLeft(SvMarker, Math.Max(0, Math.Min(width - SvMarker.Width, x)));
                Canvas.SetTop(SvMarker, Math.Max(0, Math.Min(height - SvMarker.Height, y)));
            }

            double hueWidth = HueContainer.ActualWidth;
            if (hueWidth > 1)
            {
                double markerTop = (HueContainer.ActualHeight - HueMarker.Height) / 2;
                Canvas.SetTop(HueMarker, Math.Max(0, markerTop));
                Canvas.SetLeft(HueMarker, Math.Max(0, Math.Min(hueWidth - HueMarker.Width,
                    _hue / 360.0 * (hueWidth - HueMarker.Width))));
            }
        }

        // ===================== МЫШЬ =====================

        private void Sv_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            SvSurface.CaptureMouse();
            PickSaturationValue(e.GetPosition(SvSurface));
            e.Handled = true;
        }

        private void Sv_MouseMove(object sender, MouseEventArgs e)
        {
            if (SvSurface.IsMouseCaptured)
                PickSaturationValue(e.GetPosition(SvSurface));
        }

        private void Sv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (SvSurface.IsMouseCaptured) SvSurface.ReleaseMouseCapture();
        }

        private void Sv_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!SvSurface.IsMouseCaptured) return;
            // При захвате мыши поле «не отпускаем», пока кнопка зажата
        }

        private void PickSaturationValue(Point point)
        {
            if (SvSurface.ActualWidth <= 1 || SvSurface.ActualHeight <= 1) return;

            _saturation = Clamp(point.X / SvSurface.ActualWidth, 0, 1);
            _value = 1.0 - Clamp(point.Y / SvSurface.ActualHeight, 0, 1);
            ApplyHsv(true);
        }

        private void Hue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            HueBar.CaptureMouse();
            PickHue(e.GetPosition(HueBar));
            e.Handled = true;
        }

        private void Hue_MouseMove(object sender, MouseEventArgs e)
        {
            if (HueBar.IsMouseCaptured)
                PickHue(e.GetPosition(HueBar));
        }

        private void Hue_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (HueBar.IsMouseCaptured) HueBar.ReleaseMouseCapture();
        }

        private void Hue_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!HueBar.IsMouseCaptured) return;
        }

        private void PickHue(Point point)
        {
            if (HueBar.ActualWidth <= 1) return;
            _hue = Clamp(point.X / HueBar.ActualWidth, 0, 0.9999) * 360.0;
            ApplyHsv(true);
        }

        // ===================== ТЕКСТОВЫЕ ПОЛЯ =====================

        private void HexBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            CommitHex();
            e.Handled = true;
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e) => CommitHex();

        private void CommitHex()
        {
            if (_updating) return;
            if (TryParseHex(HexBox.Text, out Color color))
                SetColor(color, true);
            else
                ApplyHsv(true); // вернуть нормализованный текст
        }

        private void RgbBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            CommitRgb();
            e.Handled = true;
        }

        private void RgbBox_LostFocus(object sender, RoutedEventArgs e) => CommitRgb();

        private void CommitRgb()
        {
            if (_updating) return;

            if (byte.TryParse(RBox.Text, out byte r) &&
                byte.TryParse(GBox.Text, out byte g) &&
                byte.TryParse(BBox.Text, out byte b))
            {
                SetColor(Color.FromRgb(r, g, b), true);
            }
            else
            {
                ApplyHsv(true);
            }
        }

        // ===================== КОПИРОВАНИЕ =====================

        private void CopyHex_Click(object sender, RoutedEventArgs e) => Copy(HexBox.Text);
        private void CopyRgb_Click(object sender, RoutedEventArgs e) => Copy($"{RBox.Text}, {GBox.Text}, {BBox.Text}");
        private void CopyHsl_Click(object sender, RoutedEventArgs e) => Copy(HslBox.Text);
        private void CopyCmyk_Click(object sender, RoutedEventArgs e) => Copy(CmykBox.Text);

        private static void Copy(string text)
        {
            try { Clipboard.SetText(string.IsNullOrEmpty(text) ? " " : text); }
            catch { /* буфер обмена может быть занят */ }
        }

        // ===================== КНОПКИ =====================

        private void Apply_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyDown(e);
        }

        // ===================== КОНВЕРТАЦИИ =====================

        private static double Clamp(double value, double min, double max)
            => value < min ? min : (value > max ? max : value);

        private static (double h, double s, double v) ColorToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0.00001)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max <= 0 ? 0 : delta / max;
            return (h, s, max);
        }

        private static Color HsvToColor(double h, double s, double v)
        {
            h = ((h % 360) + 360) % 360;
            s = Clamp(s, 0, 1);
            v = Clamp(v, 0, 1);

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        private static string ToHsl(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double l = (max + min) / 2;
            double h = 0;
            double s = 0;

            if (delta > 0.00001)
            {
                s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            return $"{h:0}, {s * 100:0}%, {l * 100:0}%";
        }

        private static string ToCmyk(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double k = 1 - Math.Max(r, Math.Max(g, b));
            double c, m, y;
            if (k >= 0.9999) { c = 0; m = 0; y = 0; }
            else
            {
                c = (1 - r - k) / (1 - k);
                m = (1 - g - k) / (1 - k);
                y = (1 - b - k) / (1 - k);
            }

            return $"{c * 100:0}, {m * 100:0}, {y * 100:0}, {k * 100:0}";
        }

        private static bool TryParseHex(string text, out Color color)
        {
            color = Colors.Transparent;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string value = text.Trim().TrimStart('#');
            if (value.Length == 3)
                value = new string(new[] { value[0], value[0], value[1], value[1], value[2], value[2] });
            if (value.Length == 8) value = value.Substring(2); // AARRGGBB
            if (value.Length != 6) return false;

            try
            {
                byte r = Convert.ToByte(value.Substring(0, 2), 16);
                byte g = Convert.ToByte(value.Substring(2, 2), 16);
                byte b = Convert.ToByte(value.Substring(4, 2), 16);
                color = Color.FromRgb(r, g, b);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
