using System;
using System.Windows;
using System.Windows.Input;

namespace AutoClicker
{
    /// <summary>Небольшое окно: нажмите клавишу или сочетание — оно попадёт в список автонажатия.</summary>
    public partial class KeyCaptureWindow : Window
    {
        private readonly string _language;

        public KeyBindingEntry Result { get; private set; }

        public KeyCaptureWindow(string language)
        {
            InitializeComponent();

            _language = language;
            TitleText.Text = LanguageManager.GetString(_language, "KeyCaptureTitle");
            HintText.Text = LanguageManager.GetString(_language, "KeyCaptureHint");

            Loaded += (s, e) =>
            {
                Activate();
                Focus();
                Keyboard.Focus(this);
            };
        }

        private static bool IsModifierKey(Key key)
        {
            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LWin:
                case Key.RWin:
                    return true;
                default:
                    return false;
            }
        }

        private void UpdatePreview()
        {
            ModifierKeys modifiers = Keyboard.Modifiers;
            var parts = new System.Collections.Generic.List<string>(5);

            if ((modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");
            if ((modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
            if ((modifiers & ModifierKeys.Windows) != 0) parts.Add("Win");

            parts.Add("…");
            PreviewText.Text = string.Join(" + ", parts);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
                return;
            }

            if (IsModifierKey(key))
            {
                UpdatePreview();
                e.Handled = true;
                return;
            }

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey <= 0) return;

            ModifierKeys modifiers = Keyboard.Modifiers;
            Result = new KeyBindingEntry
            {
                VirtualKey = virtualKey,
                Ctrl = (modifiers & ModifierKeys.Control) != 0,
                Shift = (modifiers & ModifierKeys.Shift) != 0,
                Alt = (modifiers & ModifierKeys.Alt) != 0,
                Win = (modifiers & ModifierKeys.Windows) != 0,
                Enabled = true
            };

            DialogResult = true;
            Close();
            e.Handled = true;
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
