using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinForms = System.Windows.Forms;

namespace AutoClicker
{
    public partial class MainWindow : Window
    {
        private enum SelectionKind
        {
            Zone,
            Point
        }

        private readonly MainViewModel VM;
        private HwndSource _source;
        private IntPtr _hwnd = IntPtr.Zero;

        // Состояние выбора цели
        private bool _isSelecting;
        private SelectionKind _selectionKind;
        private Point _selectionStart;
        private WindowState _prevWindowState;
        private WindowStyle _prevWindowStyle;
        private ResizeMode _prevResizeMode;
        private bool _prevTopmost;
        private double _prevLeft, _prevTop, _prevWidth, _prevHeight, _prevOpacity;

        // Состояние «своего» maximize
        private bool _isMaximized;
        private bool _wasMaximized;
        private Rect _restoreBounds = Rect.Empty;

        // Выбор окна кликом
        private bool _pickingWindow;
        private IntPtr _pickHook = IntPtr.Zero;
        private IntPtr _pickResult = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc _pickProc;
        private Window _pickHint;
        private DateTime _pickStarted;

        private WinForms.NotifyIcon _tray;
        private bool _exiting;
        private bool _loaded;

        public MainWindow()
        {
            InitializeComponent();

            VM = new MainViewModel();
            DataContext = VM;

            VM.OnSelectArea += () => Dispatcher.BeginInvoke(new Action(() => BeginSelection(SelectionKind.Zone)));
            VM.OnSelectPoint += () => Dispatcher.BeginInvoke(new Action(() => BeginSelection(SelectionKind.Point)));
            VM.OnSelectWindow += () => Dispatcher.BeginInvoke(new Action(SelectWindowInteractive));
            VM.OnHotkeyChanged += () => Dispatcher.BeginInvoke(new Action(RegisterCurrentHotkey));
            VM.PropertyChanged += ViewModel_PropertyChanged;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
        }

        // ===================== ЖИЗНЕННЫЙ ЦИКЛ =====================

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded) return;
            _loaded = true;

            // Не вылезаем за рабочую область даже на маленьких экранах
            var workArea = SystemParameters.WorkArea;
            if (Height > workArea.Height - 16) Height = Math.Max(MinHeight, workArea.Height - 16);
            if (Width > workArea.Width - 16) Width = Math.Max(MinWidth, workArea.Width - 16);

            Opacity = VM.WindowOpacity;
            Topmost = VM.IsTopmost;
            UpdateRunVisuals();

            _hwnd = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(WndProc);

            RegisterCurrentHotkey();
            SetupTrayIcon();
            PlayIntroAnimation();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (VM.MinimizeToTray && !_exiting)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            if (VM.IsRunning) VM.StopCommand.Execute(null);

            VM.SaveSettingsNow();
            HotkeyManager.Unregister(_hwnd);
            RemovePickHook();

            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && VM.MinimizeToTray)
                Dispatcher.BeginInvoke(new Action(HideToTray));
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.WindowOpacity):
                    if (!_isSelecting) Opacity = VM.WindowOpacity;
                    break;
                case nameof(MainViewModel.IsTopmost):
                    if (!_isSelecting) Topmost = VM.IsTopmost;
                    break;
                case nameof(MainViewModel.StatusMessage):
                    AnimateStatus();
                    break;
                case nameof(MainViewModel.IsRunning):
                    UpdateRunVisuals();
                    break;
            }
        }

        // ===================== АНИМАЦИИ =====================

        private void PlayIntroAnimation()
        {
            WindowBorder.Opacity = 0;
            WindowBorder.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            if (WindowBorder.RenderTransform is ScaleTransform scale)
            {
                var grow = new DoubleAnimation(0.975, 1, TimeSpan.FromMilliseconds(340))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            }
        }

        private void AnimateStatus()
        {
            if (StatusText == null) return;
            StatusText.BeginAnimation(OpacityProperty, new DoubleAnimation(0.15, 1, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void UpdateRunVisuals()
        {
            if (StartGlow == null) return;

            if (VM.IsRunning)
            {
                StartGlow.BeginAnimation(OpacityProperty, new DoubleAnimation(0.55, 0.12, TimeSpan.FromMilliseconds(1200))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });

                StatusPulse.BeginAnimation(OpacityProperty, new DoubleAnimation(0.75, 0, TimeSpan.FromMilliseconds(1100))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });

                if (StatusPulseScale != null)
                {
                    StatusPulseScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 2.8, TimeSpan.FromMilliseconds(1100)) { RepeatBehavior = RepeatBehavior.Forever });
                    StatusPulseScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 2.8, TimeSpan.FromMilliseconds(1100)) { RepeatBehavior = RepeatBehavior.Forever });
                }
            }
            else
            {
                StartGlow.BeginAnimation(OpacityProperty, null);
                StartGlow.Opacity = 0;
                StatusPulse.BeginAnimation(OpacityProperty, null);
                StatusPulse.Opacity = 0;
                StatusPulseScale?.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                StatusPulseScale?.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }
        }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != MainTabs) return;
            var presenter = MainTabs.Template?.FindName("PART_SelectedContentHost", MainTabs) as ContentPresenter;
            if (presenter == null) return;

            presenter.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
            if (presenter.RenderTransform is TranslateTransform shift)
            {
                shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(260))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            }
        }

        // ===================== ГОРЯЧАЯ КЛАВИША =====================

        private void RegisterCurrentHotkey()
        {
            if (_hwnd == IntPtr.Zero) return;

            if (!VM.HotkeyEnabled || VM.HotkeyVk == 0)
            {
                HotkeyManager.Unregister(_hwnd);
                return;
            }

            if (!HotkeyManager.TryRegister(_hwnd, VM.HotkeyModifiers, VM.HotkeyVk, out _))
                VM.StatusMessage = LanguageManager.GetString(VM.CurrentLanguage, "StatusHotkeyBusy");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyManager.HotkeyId)
            {
                if (!VM.IsListeningForHotkey && !_isSelecting && !_pickingWindow)
                    VM.ToggleRun();
                handled = true;
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_isSelecting && e.Key == Key.Escape)
            {
                e.Handled = true;
                EndSelection();
                VM.StatusMessage = LanguageManager.GetString(VM.CurrentLanguage, "StatusReady");
                return;
            }

            if (!VM.IsListeningForHotkey) return;

            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ждём «настоящую» клавишу, а не сам модификатор
            if (HotkeyManager.IsModifierKey(key)) return;

            if (key == Key.Escape)
            {
                VM.CancelHotkeyCapture();
                RegisterCurrentHotkey();
                return;
            }

            if (key == Key.Delete || key == Key.Back)
            {
                VM.DisableHotkey();
                return;
            }

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (vk == 0) return;

            uint modifiers = HotkeyManager.FromModifierKeys(Keyboard.Modifiers);

            // Та же комбинация — просто выходим из режима перехвата.
            if (vk == VM.HotkeyVk && modifiers == VM.HotkeyModifiers)
            {
                VM.CancelHotkeyCapture();
                return;
            }

            // Сначала проверяем, что клавиша свободна: иначе не потеряем текущую.
            if (!HotkeyManager.IsAvailable(_hwnd, modifiers, vk, out _))
            {
                VM.StatusMessage = LanguageManager.GetString(VM.CurrentLanguage, "StatusHotkeyBusy");
                return;
            }

            if (HotkeyManager.TryRegister(_hwnd, modifiers, vk, out _))
                VM.CommitHotkey(modifiers, vk, HotkeyManager.Format(modifiers, vk));
            else
                VM.StatusMessage = LanguageManager.GetString(VM.CurrentLanguage, "StatusHotkeyBusy");
        }

        // ===================== ОКНО =====================

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (IsFromButton(e.OriginalSource)) return;

            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            if (_isMaximized) RestoreFromMaximized();

            try { DragMove(); } catch { /* окно уже отпущено */ }
        }

        private static bool IsFromButton(object source)
        {
            var current = source as DependencyObject;
            while (current != null)
            {
                if (current is ButtonBase) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            if (VM.MinimizeToTray) HideToTray();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ToggleMaximize()
        {
            if (_isMaximized) RestoreFromMaximized();
            else MaximizeToWorkArea();
        }

        private void MaximizeToWorkArea()
        {
            _restoreBounds = new Rect(Left, Top, Width, Height);
            var workArea = SystemParameters.WorkArea;

            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;

            _isMaximized = true;
            WindowBorder.Margin = new Thickness(0);
            WindowBorder.CornerRadius = new CornerRadius(0);
            WindowBorder.Effect = null;
            UpdateGripVisibility();
        }

        private void RestoreFromMaximized()
        {
            if (_restoreBounds.IsEmpty) _restoreBounds = new Rect(Left, Top, 1080, 790);

            Left = _restoreBounds.Left;
            Top = _restoreBounds.Top;
            Width = _restoreBounds.Width;
            Height = _restoreBounds.Height;

            _isMaximized = false;
            WindowBorder.Margin = new Thickness(10);
            WindowBorder.CornerRadius = new CornerRadius(16);
            if (WindowBorder.Effect == null)
            {
                WindowBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 34,
                    ShadowDepth = 0,
                    Opacity = 0.5,
                    Color = Colors.Black
                };
            }
            UpdateGripVisibility();
        }

        private void UpdateGripVisibility()
        {
            var visibility = _isMaximized ? Visibility.Collapsed : Visibility.Visible;
            GripLeft.Visibility = visibility;
            GripRight.Visibility = visibility;
            GripTop.Visibility = visibility;
            GripBottom.Visibility = visibility;
            GripTopLeft.Visibility = visibility;
            GripTopRight.Visibility = visibility;
            GripBottomLeft.Visibility = visibility;
            GripBottomRight.Visibility = visibility;
        }

        private void GripLeft_DragDelta(object sender, DragDeltaEventArgs e) => ResizeHorizontal(e.HorizontalChange, true);
        private void GripRight_DragDelta(object sender, DragDeltaEventArgs e) => ResizeHorizontal(-e.HorizontalChange, false);
        private void GripTop_DragDelta(object sender, DragDeltaEventArgs e) => ResizeVertical(e.VerticalChange, true);
        private void GripBottom_DragDelta(object sender, DragDeltaEventArgs e) => ResizeVertical(-e.VerticalChange, false);

        private void ResizeHorizontal(double delta, bool moveLeft)
        {
            if (_isMaximized) return;
            double newWidth = Math.Max(MinWidth, Width + delta);
            if (moveLeft) Left += Width - newWidth;
            Width = newWidth;
        }

        private void ResizeVertical(double delta, bool moveTop)
        {
            if (_isMaximized) return;
            double newHeight = Math.Max(MinHeight, Height + delta);
            if (moveTop) Top += Height - newHeight;
            Height = newHeight;
        }

        private void GripTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeHorizontal(e.HorizontalChange, true);
            ResizeVertical(e.VerticalChange, true);
        }

        private void GripTopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeHorizontal(-e.HorizontalChange, false);
            ResizeVertical(e.VerticalChange, true);
        }

        private void GripBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeHorizontal(e.HorizontalChange, true);
            ResizeVertical(-e.VerticalChange, false);
        }

        private void GripBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeHorizontal(-e.HorizontalChange, false);
            ResizeVertical(-e.VerticalChange, false);
        }

        // ===================== ВЫБОР ЗОНЫ И ТОЧКИ =====================

        private void BeginSelection(SelectionKind kind)
        {
            if (_isSelecting || _pickingWindow) return;

            _isSelecting = true;
            _selectionKind = kind;

            _prevWindowState = WindowState;
            _prevWindowStyle = WindowStyle;
            _prevResizeMode = ResizeMode;
            _prevTopmost = Topmost;
            _prevLeft = Left;
            _prevTop = Top;
            _prevWidth = Width;
            _prevHeight = Height;
            _prevOpacity = Opacity;
            _wasMaximized = _isMaximized;

            if (_isMaximized) RestoreFromMaximized();

            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Opacity = 1;

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = Math.Max(200, SystemParameters.VirtualScreenWidth);
            Height = Math.Max(200, SystemParameters.VirtualScreenHeight);

            WindowBorder.Visibility = Visibility.Collapsed;
            CaptureBanner.Visibility = Visibility.Collapsed;

            SelectionOverlay.Visibility = Visibility.Visible;
            SelectionRect.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            SelectionOverlay.Cursor = Cursors.Cross;

            OverlayTitle.Text = T(kind == SelectionKind.Zone ? "OverlayZone" : "OverlayPoint");
            OverlayHint.Text = T("OverlayCancel");

            SelectionOverlay.Focus();
            Keyboard.Focus(SelectionOverlay);

            VM.StatusMessage = T(kind == SelectionKind.Zone ? "StatusSelectZone" : "StatusSelectPoint");
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting || e.ChangedButton != MouseButton.Left) return;

            _selectionStart = e.GetPosition(SelectionCanvas);
            SelectionRect.Visibility = Visibility.Visible;
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            Canvas.SetLeft(SelectionRect, _selectionStart.X);
            Canvas.SetTop(SelectionRect, _selectionStart.Y);

            SelectionOverlay.CaptureMouse();
            e.Handled = true;
        }

        private void Overlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            Point current = e.GetPosition(SelectionCanvas);

            if (_selectionKind == SelectionKind.Zone)
            {
                double x = Math.Min(_selectionStart.X, current.X);
                double y = Math.Min(_selectionStart.Y, current.Y);
                double w = Math.Abs(_selectionStart.X - current.X);
                double h = Math.Abs(_selectionStart.Y - current.Y);

                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = w;
                SelectionRect.Height = h;

                SizeBadge.Visibility = Visibility.Visible;
                SizeText.Text = $"{(int)w} × {(int)h}";
                Canvas.SetLeft(SizeBadge, Math.Min(x + w + 12, SelectionCanvas.ActualWidth - 110));
                Canvas.SetTop(SizeBadge, Math.Min(y + h + 12, SelectionCanvas.ActualHeight - 40));
            }
            else
            {
                SizeBadge.Visibility = Visibility.Visible;
                var screen = ToScreen(current);
                SizeText.Text = $"X: {screen.X}   Y: {screen.Y}";
                Canvas.SetLeft(SizeBadge, current.X + 18);
                Canvas.SetTop(SizeBadge, current.Y + 18);
            }
        }

        private void Overlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting || e.ChangedButton != MouseButton.Left) return;

            SelectionOverlay.ReleaseMouseCapture();

            Point end = e.GetPosition(SelectionCanvas);
            Point startScreen = ToScreen(_selectionStart);
            Point endScreen = ToScreen(end);

            int x = (int)Math.Min(startScreen.X, endScreen.X);
            int y = (int)Math.Min(startScreen.Y, endScreen.Y);
            int w = (int)Math.Abs(startScreen.X - endScreen.X);
            int h = (int)Math.Abs(startScreen.Y - endScreen.Y);

            EndSelection();

            if (_selectionKind == SelectionKind.Zone)
            {
                if (w < 5 || h < 5) VM.UpdatePoint((int)endScreen.X, (int)endScreen.Y);
                else VM.UpdateZone(x, y, w, h);
            }
            else
            {
                VM.UpdatePoint((int)endScreen.X, (int)endScreen.Y);
            }
        }

        private void EndSelection()
        {
            _isSelecting = false;

            SelectionOverlay.Visibility = Visibility.Collapsed;
            SelectionRect.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            WindowBorder.Visibility = Visibility.Visible;

            Topmost = _prevTopmost;
            Opacity = _prevOpacity;
            WindowStyle = _prevWindowStyle;
            ResizeMode = _prevResizeMode;
            Left = _prevLeft;
            Top = _prevTop;
            Width = _prevWidth;
            Height = _prevHeight;

            if (_wasMaximized) MaximizeToWorkArea();
            else WindowState = _prevWindowState;
            _wasMaximized = false;
        }

        private Point ToScreen(Point local)
        {
            if (_hwnd == IntPtr.Zero || !NativeMethods.GetWindowRect(_hwnd, out var rect))
                return local;

            double scaleX = (rect.Right - rect.Left) / Math.Max(1.0, ActualWidth);
            double scaleY = (rect.Bottom - rect.Top) / Math.Max(1.0, ActualHeight);

            return new Point(rect.Left + local.X * scaleX, rect.Top + local.Y * scaleY);
        }

        // ===================== ВЫБОР ОКНА =====================

        private async void SelectWindowInteractive()
        {
            if (_pickingWindow || _isSelecting) return;

            _pickingWindow = true;
            _pickResult = IntPtr.Zero;
            VM.StatusMessage = T("StatusSelectWindow");

            _pickHint = CreatePickHintWindow();
            _pickHint.Show();

            Hide();

            _pickStarted = DateTime.UtcNow;
            _pickProc = PickMouseHook;
            _pickHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _pickProc,
                NativeMethods.GetModuleHandle(null), 0);

            while (_pickingWindow && _pickResult == IntPtr.Zero &&
                   (DateTime.UtcNow - _pickStarted) < TimeSpan.FromSeconds(10))
            {
                if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_ESCAPE) & 0x8000) != 0) break;

                // Резервный вариант, если хук не установился
                if (_pickHook == IntPtr.Zero && (DateTime.UtcNow - _pickStarted).TotalMilliseconds > 900)
                {
                    IntPtr foreground = NativeMethods.GetForegroundWindow();
                    if (foreground != IntPtr.Zero && !IsOwnWindow(foreground))
                    {
                        IntPtr root = NativeMethods.GetAncestor(foreground, NativeMethods.GA_ROOT);
                        _pickResult = root != IntPtr.Zero ? root : foreground;
                        break;
                    }
                }

                await Task.Delay(50);
            }

            RemovePickHook();
            _pickingWindow = false;

            if (_pickHint != null)
            {
                _pickHint.Close();
                _pickHint = null;
            }

            Show();
            Activate();
            Topmost = VM.IsTopmost;
            Opacity = VM.WindowOpacity;

            if (_pickResult != IntPtr.Zero)
                VM.UpdateWindow(_pickResult, NativeMethods.GetWindowTitle(_pickResult));
            else
                VM.StatusMessage = T("StatusReady");
        }

        private IntPtr PickMouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _pickingWindow)
            {
                int message = wParam.ToInt32();
                if (message == NativeMethods.WM_LBUTTONDOWN)
                {
                    var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                    IntPtr root = NativeMethods.GetRootWindowFromPoint(data.pt.X, data.pt.Y);
                    if (root != IntPtr.Zero && !IsOwnWindow(root))
                        _pickResult = root;
                }
            }
            return NativeMethods.CallNextHookEx(_pickHook, nCode, wParam, lParam);
        }

        private void RemovePickHook()
        {
            if (_pickHook == IntPtr.Zero) return;
            try { NativeMethods.UnhookWindowsHookEx(_pickHook); } catch { }
            _pickHook = IntPtr.Zero;
        }

        private static bool IsOwnWindow(IntPtr hwnd)
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            return processId == (uint)Process.GetCurrentProcess().Id;
        }

        private Window CreatePickHintWindow()
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(22, 14, 22, 14),
                BorderThickness = new Thickness(1)
            };

            border.SetResourceReference(Border.BackgroundProperty, "CardBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");

            var panel = new StackPanel();
            var title = new TextBlock
            {
                Text = T("OverlayWindow"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            title.SetResourceReference(TextBlock.ForegroundProperty, "ForegroundBrush");

            var hint = new TextBlock
            {
                Text = T("OverlayWindowCancel"),
                FontSize = 11,
                Margin = new Thickness(0, 5, 0, 0)
            };
            hint.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

            panel.Children.Add(title);
            panel.Children.Add(hint);
            border.Child = panel;

            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ShowActivated = false,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Content = border
            };

            window.SourceInitialized += (s, e) =>
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                NativeMethods.AddExStyle(handle,
                    NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
            };

            window.Loaded += (s, e) =>
            {
                window.Left = (SystemParameters.PrimaryScreenWidth - window.ActualWidth) / 2;
                window.Top = 90;
            };

            return window;
        }

        // ===================== ТРЕЙ =====================

        private void SetupTrayIcon()
        {
            if (_tray != null) return;

            System.Drawing.Icon icon = null;
            try
            {
                string exe = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exe)) icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
            }
            catch { }

            _tray = new WinForms.NotifyIcon
            {
                Icon = icon ?? System.Drawing.SystemIcons.Application,
                Text = "AutoClicker Pro",
                Visible = true
            };

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add(new WinForms.ToolStripMenuItem(T("TrayShow"), null, (s, e) => RestoreFromTray()));
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(new WinForms.ToolStripMenuItem(T("BtnStart"), null, (s, e) => VM.StartCommand.Execute(null)));
            menu.Items.Add(new WinForms.ToolStripMenuItem(T("BtnStop"), null, (s, e) => VM.StopCommand.Execute(null)));
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(new WinForms.ToolStripMenuItem(T("TrayExit"), null, (s, e) =>
            {
                _exiting = true;
                Close();
            }));

            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void HideToTray()
        {
            if (_tray == null) return;
            Hide();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Topmost = VM.IsTopmost;
        }

        private string T(string key) => LanguageManager.GetString(VM.CurrentLanguage, key);
    }
}
