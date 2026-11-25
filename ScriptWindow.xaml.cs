using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace AutoClicker
{
    /// <summary>
    /// Окно Lua-сценариев: редактор, лог и небольшой API для кликов/пауз.
    /// Скрипт выполняется в фоновом потоке, «Стоп» выставляет флаг отмены.
    /// </summary>
    public partial class ScriptWindow : Window
    {
        private readonly string _language;
        private readonly string _scriptPath;
        private readonly string _examplesDir;
        private readonly List<string> _exampleNames = new List<string>();
        private readonly Random _rng = new Random();
        private bool _loadingExamples;

        private volatile bool _cancelRequested;
        private Thread _thread;
        private string _scriptText;

        public ScriptWindow(string language)
        {
            InitializeComponent();

            _language = language;
            _scriptPath = Path.Combine(SettingsManager.DataDirectory, "scripts", "last.lua");
            _examplesDir = Path.Combine(SettingsManager.DataDirectory, "scripts", "examples");

            ApplyLocalization();
            LoadScript();
            BuildExamples();

            Closing += (s, e) =>
            {
                _cancelRequested = true;
                try { _thread?.Join(250); } catch { }
                SaveScript();
            };
        }

        private string T(string key) => LanguageManager.GetString(_language, key);

        private void ApplyLocalization()
        {
            TitleText.Text = T("ScriptTitle");
            HintText.Text = T("ScriptHint");
            LogLabel.Text = T("LogLabel");
            RunButton.Content = T("BtnRunScript");
            StopButton.Content = T("BtnStopScript");
            ClearLogButton.Content = T("BtnClearLog");
            ExamplesLabel.Text = T("ExamplesLabel");
            RefreshExamplesButton.Content = T("BtnRefreshShort");
            RefreshExamplesButton.ToolTip = T("BtnRefreshExamples");
            StatusText.Text = T("ScriptReady");
            StopButton.IsEnabled = false;
            LogBox.Text = string.Empty;
        }

        // ===================== БИБЛИОТЕКА ПРИМЕРОВ =====================

        private void BuildExamples()
        {
            try
            {
                Directory.CreateDirectory(_examplesDir);
                SeedExamples();

                _exampleNames.Clear();
                _exampleNames.AddRange(Directory.GetFiles(_examplesDir, "*.lua")
                    .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
                    .Select(Path.GetFileNameWithoutExtension));

                _loadingExamples = true;
                ExampleBox.ItemsSource = null;
                ExampleBox.ItemsSource = _exampleNames.ToList();
                ExampleBox.SelectedIndex = -1;
                _loadingExamples = false;
            }
            catch { }
        }

        private void SeedExamples()
        {
            WriteExample("01 Приветствие.lua", Code(
                "-- Приветствие: только лог и паузы, без кликов",
                "log(\"Привет! Это Lua-сценарий.\")",
                "",
                "for i = 1, 3 do",
                "    log(\"Шаг \" .. i)",
                "    sleep(500)",
                "end",
                "",
                "log(\"Готово\")"));

            WriteExample("02 Клики по точкам.lua", Code(
                "-- Клики по координатам (пиксели экрана, от левого верхнего угла)",
                "local points = {",
                "    { x = 400, y = 300 },",
                "    { x = 600, y = 300 },",
                "    { x = 500, y = 500 },",
                "}",
                "",
                "for i, p in ipairs(points) do",
                "    click(p.x, p.y)",
                "    log(\"Клик \" .. i .. \": \" .. p.x .. \", \" .. p.y)",
                "    sleep(400)",
                "end"));

            WriteExample("03 Проверка цвета.lua", Code(
                "-- Ждём, пока пиксель станет нужного цвета, затем кликаем",
                "local x, y = 500, 400",
                "local wanted = \"#FF0000\"",
                "",
                "for i = 1, 100 do",
                "    local color = getColor(x, y)",
                "    if color == wanted then",
                "        click(x, y)",
                "        log(\"Цвет совпал: \" .. color)",
                "        break",
                "    end",
                "    sleep(200)",
                "end",
                "",
                "log(\"Текущий цвет: \" .. getColor(x, y))"));

            WriteExample("04 Случайные клики.lua", Code(
                "-- Случайные клики внутри прямоугольника",
                "local x, y = 400, 300",
                "local w, h = 300, 200",
                "",
                "for i = 1, 20 do",
                "    click(x + random(0, w), y + random(0, h))",
                "    sleep(150)",
                "end",
                "",
                "log(\"Готово: 20 кликов\")"));

            WriteExample("05 Цикл до Стоп.lua", Code(
                "-- Бесконечный цикл: остановить кнопкой «Стоп»",
                "local n = 0",
                "",
                "while running() do",
                "    click(500, 400)",
                "    n = n + 1",
                "    log(\"Клик \" .. n)",
                "    sleep(1000)",
                "end"));

            WriteExample("06 Правая кнопка.lua", Code(
                "-- Правая кнопка и двойной клик",
                "clickRight(600, 400)",
                "sleep(300)",
                "click(400, 400)",
                "sleep(80)",
                "click(400, 400)",
                "log(\"Готово\")"));

            WriteExample("07 Цвет пикселя.lua", Code(
                "-- Прочитать цвет пикселя и вывести в лог",
                "local x, y = 500, 400",
                "log(\"Цвет в \" .. x .. \",\" .. y .. \": \" .. getColor(x, y))"));

            WriteExample("08 Клавиши и текст.lua", Code(
                "-- Клавиши и текст (ввод идёт в активное окно)",
                "pressKey(\"F1\")",
                "sleep(300)",
                "typeText(\"Hello from AutoClicker\")",
                "sleep(300)",
                "pressKey(\"Enter\")",
                "log(\"Готово\")"));

            WriteExample("09 Поиск цвета.lua", Code(
                "-- Поиск цвета на экране",
                "local pos = findColor(\"#FF0000\", 0, 0, screenWidth(), screenHeight(), 4)",
                "if pos then",
                "    log(\"Найдено: \" .. pos)",
                "else",
                "    log(\"Цвет не найден\")",
                "end"));
        }

        private void WriteExample(string fileName, string code)
        {
            try
            {
                string path = Path.Combine(_examplesDir, fileName);
                if (!File.Exists(path)) File.WriteAllText(path, code);
            }
            catch { }
        }

        private void Example_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_loadingExamples || ExampleBox.SelectedIndex < 0 || ExampleBox.SelectedIndex >= _exampleNames.Count)
                return;

            string name = _exampleNames[ExampleBox.SelectedIndex];
            try
            {
                EditorBox.Text = File.ReadAllText(Path.Combine(_examplesDir, name + ".lua"));
                StatusText.Text = $"{T("StatusExampleLoaded")}: {name}";
            }
            catch { }
        }

        private void RefreshExamples_Click(object sender, RoutedEventArgs e)
        {
            BuildExamples();
            StatusText.Text = T("StatusExamplesRefreshed");
        }

        private static string Code(params string[] lines) => string.Join("\r\n", lines);
        // ===================== ЗАПУСК =====================

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (_thread != null && _thread.IsAlive) return;

            _scriptText = EditorBox.Text;
            _cancelRequested = false;
            SetRunning(true);

            _thread = new Thread(RunScript)
            {
                IsBackground = true,
                Name = "AutoClicker.Lua"
            };
            _thread.Start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _cancelRequested = true;
            StatusText.Text = T("ScriptStopped");
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Clear();
        }

        private void RunScript()
        {
            try
            {
                var script = new Script(CoreModules.Preset_SoftSandbox);
                script.Options.DebugPrint = message => Log(message);
                RegisterApi(script);

                script.DoString(_scriptText, null, "scenario");
                Log(T("ScriptFinished"));
            }
            catch (Exception ex)
            {
                if (_cancelRequested)
                {
                    Log(T("ScriptStopped"));
                }
                else if (ex is ScriptRuntimeException runtime)
                {
                    Log($"{T("ScriptError")} {runtime.DecoratedMessage}");
                }
                else
                {
                    Log($"{T("ScriptError")} {ex.Message}");
                }
            }
            finally
            {
                Dispatcher.BeginInvoke(new Action(() => SetRunning(false)));
            }
        }

        private void SetRunning(bool running)
        {
            RunButton.IsEnabled = !running;
            StopButton.IsEnabled = running;
            StatusText.Text = running ? T("ScriptRunning") : T("ScriptReady");
        }

        // ===================== API ДЛЯ LUA =====================

        private void RegisterApi(Script script)
        {
            script.Globals["log"] = (Action<string>)(message => Log(message ?? string.Empty));
            script.Globals["sleep"] = (Action<double>)(milliseconds => Sleep(milliseconds));
            script.Globals["click"] = (Action<double, double>)((x, y) => ClickAt(x, y, MouseButtonKind.Left));
            script.Globals["clickRight"] = (Action<double, double>)((x, y) => ClickAt(x, y, MouseButtonKind.Right));
            script.Globals["clickMiddle"] = (Action<double, double>)((x, y) => ClickAt(x, y, MouseButtonKind.Middle));
            script.Globals["move"] = (Action<double, double>)((x, y) =>
            {
                ThrowIfCancelled();
                NativeMethods.SetCursorPos((int)x, (int)y);
            });
            script.Globals["getColor"] = (Func<double, double, string>)((x, y) => NativeMethods.GetScreenPixel((int)x, (int)y));
            script.Globals["random"] = (Func<double, double, double>)((from, to) => from + _rng.NextDouble() * (to - from));
            script.Globals["running"] = (Func<bool>)(() => !_cancelRequested);

            // Клавиатура
            script.Globals["pressKey"] = (Action<object>)(key => PressKey(key, 30));
            script.Globals["pressKeyFor"] = (Action<object, double>)((key, ms) => PressKey(key, (int)ms));
            script.Globals["keyDown"] = (Action<object>)(key => { if (TryParseKey(key, out var vk)) NativeMethods.KeyDown(vk); });
            script.Globals["keyUp"] = (Action<object>)(key => { if (TryParseKey(key, out var vk)) NativeMethods.KeyUp(vk); });
            script.Globals["typeText"] = (Action<string>)(text => TypeText(text, 6));
            script.Globals["typeTextSlow"] = (Action<string, double>)((text, ms) => TypeText(text, (int)ms));

            // Экран
            script.Globals["cursorX"] = (Func<double>)(() => { NativeMethods.GetCursorPos(out var p); return p.X; });
            script.Globals["cursorY"] = (Func<double>)(() => { NativeMethods.GetCursorPos(out var p); return p.Y; });
            script.Globals["screenWidth"] = (Func<double>)(() => NativeMethods.ScreenWidth);
            script.Globals["screenHeight"] = (Func<double>)(() => NativeMethods.ScreenHeight);

            // Цвет
            script.Globals["waitColor"] = (Func<double, double, string, double, bool>)(
                (x, y, hex, timeout) => WaitColor(x, y, hex, (int)timeout, 0));
            script.Globals["waitColorTol"] = (Func<double, double, string, double, double, bool>)(
                (x, y, hex, timeout, tol) => WaitColor(x, y, hex, (int)timeout, (int)tol));
            script.Globals["findColor"] = (Func<string, double, double, double, double, double, string>)(
                (hex, x1, y1, x2, y2, step) => FindColor(hex, x1, y1, x2, y2, step, 0));
            script.Globals["findColorTol"] = (Func<string, double, double, double, double, double, double, string>)(
                (hex, x1, y1, x2, y2, step, tol) => FindColor(hex, x1, y1, x2, y2, step, (int)tol));
        }

        private void ClickAt(double x, double y, MouseButtonKind button)
        {
            ThrowIfCancelled();

            uint down, up;
            switch (button)
            {
                case MouseButtonKind.Right:
                    down = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                    up = NativeMethods.MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButtonKind.Middle:
                    down = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                    up = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                    break;
                default:
                    down = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                    up = NativeMethods.MOUSEEVENTF_LEFTUP;
                    break;
            }

            NativeMethods.SendMouseClick((int)x, (int)y, down, up);
        }

        private void Sleep(double milliseconds)
        {
            int total = (int)Math.Max(0, Math.Min(milliseconds, 3600000));
            var watch = Stopwatch.StartNew();

            while (watch.ElapsedMilliseconds < total)
            {
                ThrowIfCancelled();
                int remain = total - (int)watch.ElapsedMilliseconds;
                Thread.Sleep(Math.Min(20, Math.Max(1, remain)));
            }

            ThrowIfCancelled();
        }

        private void ThrowIfCancelled()
        {
            if (_cancelRequested) throw new OperationCanceledException();
        }

        // ===================== КЛАВИАТУРА И ЦВЕТ =====================

        private static readonly Dictionary<string, ushort> KeyAliases =
            new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
            {
                ["ESC"] = 0x1B, ["ESCAPE"] = 0x1B,
                ["ENTER"] = 0x0D, ["RETURN"] = 0x0D,
                ["CTRL"] = 0x11, ["CONTROL"] = 0x11,
                ["SHIFT"] = 0x10,
                ["ALT"] = 0x12, ["MENU"] = 0x12,
                ["SPACE"] = 0x20, ["TAB"] = 0x09,
                ["BACKSPACE"] = 0x08, ["BKSP"] = 0x08,
                ["UP"] = 0x26, ["DOWN"] = 0x28, ["LEFT"] = 0x25, ["RIGHT"] = 0x27,
                ["DEL"] = 0x2E, ["DELETE"] = 0x2E,
                ["WIN"] = 0x5B, ["LWIN"] = 0x5B
            };

        private static bool TryParseKey(object key, out ushort vk)
        {
            vk = 0;
            if (key == null) return false;

            if (key is double number)
            {
                int code = (int)number;
                if (code > 0 && code < 256) { vk = (ushort)code; return true; }
                return false;
            }

            string name = key.ToString().Trim();
            if (name.Length == 0) return false;
            if (KeyAliases.TryGetValue(name, out vk)) return true;

            if (Enum.TryParse<System.Windows.Forms.Keys>(name, true, out var parsed))
            {
                int code = (int)parsed;
                if (code > 0 && code < 256) { vk = (ushort)code; return true; }
            }

            return false;
        }

        private void PressKey(object key, int holdMs)
        {
            if (!TryParseKey(key, out var vk))
            {
                Log($"{T("ScriptError")} {T("ScriptUnknownKey")}: {key}");
                return;
            }

            ThrowIfCancelled();
            NativeMethods.PressKey(vk, Math.Max(0, holdMs));
        }

        private void TypeText(string text, int delayMs)
        {
            if (string.IsNullOrEmpty(text)) return;

            foreach (char c in text)
            {
                ThrowIfCancelled();
                NativeMethods.TypeUnicodeChar(c);
                if (delayMs > 0) Thread.Sleep(delayMs);
            }
        }

        private static bool TryParseColor(string text, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string value = text.Trim().TrimStart('#');
            if (value.Length == 3)
                value = $"{value[0]}{value[0]}{value[1]}{value[1]}{value[2]}{value[2]}";

            if (value.Length != 6) return false;
            if (!int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int color)) return false;

            r = (color >> 16) & 0xFF;
            g = (color >> 8) & 0xFF;
            b = color & 0xFF;
            return true;
        }

        private bool WaitColor(double x, double y, string hex, int timeoutMs, int tolerance)
        {
            if (!TryParseColor(hex, out int r, out int g, out int b)) return false;

            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMs)
            {
                ThrowIfCancelled();

                if (NativeMethods.TryGetPixel((int)x, (int)y, out int pr, out int pg, out int pb) &&
                    Math.Abs(pr - r) <= tolerance && Math.Abs(pg - g) <= tolerance && Math.Abs(pb - b) <= tolerance)
                    return true;

                Thread.Sleep(30);
            }

            return false;
        }

        private string FindColor(string hex, double x1, double y1, double x2, double y2, double step, int tolerance)
        {
            if (!TryParseColor(hex, out int r, out int g, out int b)) return null;
            return NativeMethods.FindColor(r, g, b, (int)x1, (int)y1, (int)x2, (int)y2, (int)step, tolerance);
        }
        // ===================== ЛОГ =====================

        private void Log(string message)
        {
            if (Dispatcher.CheckAccess()) AppendLog(message);
            else Dispatcher.BeginInvoke(new Action(() => AppendLog(message)));
        }

        private void AppendLog(string message)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        }

        // ===================== СЛУЖЕБНОЕ =====================

        private void LoadScript()
        {
            try
            {
                EditorBox.Text = File.Exists(_scriptPath)
                    ? File.ReadAllText(_scriptPath)
                    : DefaultScript();
            }
            catch
            {
                EditorBox.Text = DefaultScript();
            }
        }

        private void SaveScript()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_scriptPath));
                File.WriteAllText(_scriptPath, EditorBox.Text);
            }
            catch { }
        }

        private static string DefaultScript()
        {
            return
                "-- AutoClicker Pro · Lua-сценарий\n" +
                "-- API: click(x, y), clickRight(x, y), clickMiddle(x, y),\n" +
                "--      move(x, y), sleep(ms), log(text), getColor(x, y),\n" +
                "--      random(a, b), running()\n\n" +
                "log(\"Скрипт запущен\")\n\n" +
                "for i = 1, 3 do\n" +
                "    log(\"Шаг \" .. i)\n" +
                "    sleep(500)\n" +
                "end\n\n" +
                "log(\"Готово\")\n";
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyDown(e);
        }
    }
}
