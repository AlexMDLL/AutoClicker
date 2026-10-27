using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoClicker
{
    /// <summary>
    /// Движок кликера. Работает в отдельном потоке с точным таймером
    /// (timeBeginPeriod + Stopwatch + гибрид Sleep/SpinWait).
    /// Горячий путь оптимизирован: переиспользуемые буферы SendInput,
    /// кэш метрик экрана/флагов кнопки и lock-free кольцо для CPS.
    /// </summary>
    public sealed class ClickerEngine
    {
        private static readonly int InputSize = Marshal.SizeOf(typeof(NativeMethods.INPUT));

        private readonly NativeMethods.INPUT[] _inputs = new NativeMethods.INPUT[3];
        private readonly Random _rng = new Random();

        private Thread _thread;
        private volatile bool _stopRequested;
        private volatile bool _running;
        private long _clicks;
        private long _startTicks;

        // Lock-free кольцо отметок кликов для подсчёта CPS
        private readonly long[] _clickStamps = new long[512];
        private int _clickStampPos;

        // Кэш метрик виртуального экрана
        private int _virtualX, _virtualY, _virtualWidth, _virtualHeight;
        private long _metricsStamp;

        // Кэш кнопки и её сообщений
        private MouseButtonKind _button = MouseButtonKind.Left;
        private uint _downFlag, _upFlag;
        private int _downMessage, _upMessage, _mouseWParam;

        private DateTime _lastActivation = DateTime.MinValue;
        private int _turboYieldCounter;

        public event Action Started;
        public event Action<string> Stopped; // null — остановили вручную

        // === Настройки (меняются из UI, читаются потоком кликера) ===
        public int IntervalMs { get; set; } = 100;
        public bool UseCustomInterval { get; set; }
        public double CustomInterval { get; set; }

        public MouseButtonKind Button
        {
            get => _button;
            set
            {
                if (_button == value) return;
                _button = value;
                UpdateButtonCache();
            }
        }

        public ClickMode Mode { get; set; } = ClickMode.Foreground;

        /// <summary>Игровой режим: не кликать, пока целевое окно не в фокусе.</summary>
        public bool PauseWhenUnfocused { get; set; } = true;

        public int ZoneX { get; set; }
        public int ZoneY { get; set; }
        public int ZoneWidth { get; set; }
        public int ZoneHeight { get; set; }
        public IntPtr TargetWindow { get; set; } = IntPtr.Zero;

        public bool EnableCyclicMode { get; set; }
        public double ClickDuration { get; set; } = 5.0;
        public double RestDuration { get; set; } = 2.0;

        public bool EnableRandomization { get; set; }
        public int RandomOffsetX { get; set; } = 5;
        public int RandomOffsetY { get; set; } = 5;
        public bool EnableHumanization { get; set; }
        public int HumanizePercent { get; set; } = 20;

        public int ClickHoldMs { get; set; } = 15;
        public bool ReturnCursor { get; set; }

        public bool EnableClickLimit { get; set; }
        public int MaxClicks { get; set; } = 1000;
        public bool EnableTimeLimit { get; set; }
        public int TimeLimitHours { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int TimeLimitSeconds { get; set; }

        // === Состояние ===
        public bool IsRunning => _running;
        public long ClicksDone => Interlocked.Read(ref _clicks);

        public TimeSpan Elapsed
        {
            get
            {
                long start = _startTicks;
                if (start == 0) return TimeSpan.Zero;
                long delta = Stopwatch.GetTimestamp() - start;
                return TimeSpan.FromSeconds(delta / (double)Stopwatch.Frequency);
            }
        }

        public double Cps
        {
            get
            {
                long now = Stopwatch.GetTimestamp();
                long cutoff = now - Stopwatch.Frequency;
                int position = Volatile.Read(ref _clickStampPos);
                int count = 0;

                for (int i = 1; i <= _clickStamps.Length; i++)
                {
                    long stamp = Volatile.Read(ref _clickStamps[(position - i) & 511]);
                    if (stamp < cutoff) break;
                    count++;
                }
                return count;
            }
        }

        public ClickerEngine()
        {
            UpdateButtonCache();
        }

        private void UpdateButtonCache()
        {
            switch (_button)
            {
                case MouseButtonKind.Right:
                    _downFlag = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                    _upFlag = NativeMethods.MOUSEEVENTF_RIGHTUP;
                    _downMessage = NativeMethods.WM_RBUTTONDOWN;
                    _upMessage = NativeMethods.WM_RBUTTONUP;
                    _mouseWParam = NativeMethods.MK_RBUTTON;
                    break;
                case MouseButtonKind.Middle:
                    _downFlag = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                    _upFlag = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                    _downMessage = NativeMethods.WM_MBUTTONDOWN;
                    _upMessage = NativeMethods.WM_MBUTTONUP;
                    _mouseWParam = NativeMethods.MK_MBUTTON;
                    break;
                default:
                    _downFlag = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                    _upFlag = NativeMethods.MOUSEEVENTF_LEFTUP;
                    _downMessage = NativeMethods.WM_LBUTTONDOWN;
                    _upMessage = NativeMethods.WM_LBUTTONUP;
                    _mouseWParam = NativeMethods.MK_LBUTTON;
                    break;
            }
        }

        public void Start()
        {
            if (_running) return;

            _stopRequested = false;
            Interlocked.Exchange(ref _clicks, 0);
            Interlocked.Exchange(ref _clickStampPos, 0);
            Array.Clear(_clickStamps, 0, _clickStamps.Length);
            _startTicks = Stopwatch.GetTimestamp();
            _running = true;
            _lastActivation = DateTime.MinValue;
            _turboYieldCounter = 0;

            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "AutoClicker.Engine",
                Priority = ThreadPriority.AboveNormal
            };

            Started?.Invoke();
            _thread.Start();
        }

        public void Stop()
        {
            _stopRequested = true;
            var thread = _thread;
            if (thread != null && thread.IsAlive && thread != Thread.CurrentThread)
                thread.Join(250);
        }

        /// <summary>Одиночный клик (кнопка «Тестовый клик»).</summary>
        public void ClickOnce() => PerformClick();

        // === Основной цикл ===

        private void Run()
        {
            string reason = null;
            var sw = Stopwatch.StartNew();

            NativeMethods.timeBeginPeriod(1);
            NativeMethods.SetThreadExecutionState(
                NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);

            try
            {
                TimeSpan timeLimit = TimeSpan.FromHours(TimeLimitHours)
                                   + TimeSpan.FromMinutes(TimeLimitMinutes)
                                   + TimeSpan.FromSeconds(TimeLimitSeconds);

                long nextTick = 0;
                bool first = true;

                while (!_stopRequested)
                {
                    if (EnableClickLimit && ClicksDone >= MaxClicks) { reason = "limit"; break; }
                    if (EnableTimeLimit && timeLimit > TimeSpan.Zero && sw.Elapsed >= timeLimit) { reason = "time"; break; }

                    if (Mode == ClickMode.Game && PauseWhenUnfocused && IsTargetValid()
                        && NativeMethods.GetForegroundWindow() != TargetWindow)
                    {
                        // Ждём, пока игра снова получит фокус — не спамим кликами по другим окнам
                        if (!WaitUntil(sw.ElapsedTicks + Stopwatch.Frequency / 10, sw)) break;
                        continue;
                    }

                    if (Mode == ClickMode.Game) EnsureTargetForeground();

                    if (EnableCyclicMode)
                    {
                        var burstEnd = sw.Elapsed + TimeSpan.FromSeconds(Math.Max(0.05, ClickDuration));

                        while (!_stopRequested && sw.Elapsed < burstEnd)
                        {
                            if (EnableClickLimit && ClicksDone >= MaxClicks) { reason = "limit"; break; }
                            if (EnableTimeLimit && timeLimit > TimeSpan.Zero && sw.Elapsed >= timeLimit) { reason = "time"; break; }

                            PerformClick();
                            if (!WaitInterval(sw, ref nextTick, GetIntervalMs(), ref first)) break;
                        }
                        if (reason != null || _stopRequested) break;

                        var restEnd = sw.Elapsed + TimeSpan.FromSeconds(Math.Max(0.05, RestDuration));
                        if (!WaitUntil(sw.ElapsedTicks + (long)((restEnd - sw.Elapsed).TotalSeconds * Stopwatch.Frequency), sw))
                            break;

                        first = true;
                    }
                    else
                    {
                        PerformClick();
                        if (!WaitInterval(sw, ref nextTick, GetIntervalMs(), ref first)) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Engine] {ex.Message}");
                reason = "error";
            }
            finally
            {
                NativeMethods.timeEndPeriod(1);
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
                _running = false;
                _startTicks = 0;
                try { Stopped?.Invoke(reason); } catch { }
            }
        }

        private double GetIntervalMs()
        {
            double ms = IntervalMs;
            if (UseCustomInterval) ms += CustomInterval * 1000.0;
            return Math.Max(0.0, ms); // 0 = турбо, без паузы
        }

        /// <summary>Ждёт следующего клика, компенсируя накопленное отставание.</summary>
        private bool WaitInterval(Stopwatch sw, ref long nextTick, double intervalMs, ref bool first)
        {
            if (intervalMs <= 0.0)
            {
                // Турбо: не спим вообще, но изредка отдаём квант, чтобы не душить систему
                if ((++_turboYieldCounter & 127) == 0) Thread.Sleep(0);
                return !_stopRequested;
            }

            double jitter = 1.0;
            if (EnableHumanization && HumanizePercent > 0)
            {
                double max = Math.Min(90, HumanizePercent) / 100.0;
                jitter = 1.0 + (_rng.NextDouble() * 2.0 - 1.0) * max;
            }

            double actual = Math.Max(0.2, intervalMs * jitter);
            long step = (long)(actual * Stopwatch.Frequency / 1000.0);

            if (first) { nextTick = sw.ElapsedTicks + step; first = false; }
            else nextTick += step;

            long now = sw.ElapsedTicks;
            if (nextTick < now) nextTick = now + step / 2; // догнали — не копим долг

            return WaitUntil(nextTick, sw);
        }

        /// <summary>Точное ожидание: Sleep для больших пауз, SpinWait для последних миллисекунд.</summary>
        private bool WaitUntil(long targetTicks, Stopwatch sw)
        {
            while (!_stopRequested)
            {
                long remain = targetTicks - sw.ElapsedTicks;
                if (remain <= 0) return true;

                double ms = remain * 1000.0 / Stopwatch.Frequency;
                if (ms > 3.0) Thread.Sleep((int)(ms - 2));
                else if (ms > 0.8) Thread.Sleep(0);
                else Thread.SpinWait(24);
            }
            return false;
        }

        // === Клик ===

        private void PerformClick()
        {
            if (!TryResolvePoint(out int x, out int y)) return;

            if (Mode == ClickMode.Background && TargetWindow != IntPtr.Zero && IsTargetValid())
            {
                ClickBackground(x, y);
                RegisterClick();
                return;
            }

            ClickForeground(x, y);
            RegisterClick();
        }

        private bool IsTargetValid()
            => TargetWindow != IntPtr.Zero && NativeMethods.IsWindow(TargetWindow);

        private bool TryResolvePoint(out int x, out int y)
        {
            x = 0;
            y = 0;

            if (ZoneWidth > 0 && ZoneHeight > 0)
            {
                if (EnableRandomization)
                {
                    x = ZoneX + _rng.Next(ZoneWidth);
                    y = ZoneY + _rng.Next(ZoneHeight);
                }
                else
                {
                    x = ZoneX + ZoneWidth / 2;
                    y = ZoneY + ZoneHeight / 2;
                }
                return true;
            }

            if (IsTargetValid() && NativeMethods.GetClientRect(TargetWindow, out var client))
            {
                var center = new NativeMethods.POINT(client.Right / 2, client.Bottom / 2);
                if (NativeMethods.ClientToScreen(TargetWindow, ref center))
                {
                    x = center.X;
                    y = center.Y;
                    return true;
                }
            }

            if (!NativeMethods.GetCursorPos(out var cursor)) return false;
            x = cursor.X;
            y = cursor.Y;

            if (EnableRandomization)
            {
                int rx = Math.Max(1, RandomOffsetX);
                int ry = Math.Max(1, RandomOffsetY);
                x += _rng.Next(-rx, rx + 1);
                y += _rng.Next(-ry, ry + 1);
            }
            return true;
        }

        private void ClickForeground(int screenX, int screenY)
        {
            ToAbsolute(screenX, screenY, out int ax, out int ay);

            NativeMethods.POINT original = default;
            bool restoreCursor = ReturnCursor && NativeMethods.GetCursorPos(out original);

            if (ClickHoldMs > 0)
            {
                _inputs[0] = MakeInput(ax, ay, NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_VIRTUALDESK);
                _inputs[1] = MakeInput(ax, ay, _downFlag | NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_VIRTUALDESK);
                NativeMethods.SendInput(2, _inputs, InputSize);

                PreciseDelay(ClickHoldMs);

                _inputs[0] = MakeInput(ax, ay, _upFlag | NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_VIRTUALDESK);
                NativeMethods.SendInput(1, _inputs, InputSize);
            }
            else
            {
                _inputs[0] = MakeInput(ax, ay, NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_VIRTUALDESK);
                _inputs[1] = MakeInput(ax, ay, _downFlag | NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_VIRTUALDESK);
                _inputs[2] = MakeInput(ax, ay, _upFlag | NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_VIRTUALDESK);
                NativeMethods.SendInput(3, _inputs, InputSize);
            }

            if (restoreCursor) NativeMethods.SetCursorPos(original.X, original.Y);
        }

        private static NativeMethods.INPUT MakeInput(int ax, int ay, uint flags)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = ax,
                    dy = ay,
                    dwFlags = flags
                }
            };
        }

        private void ClickBackground(int screenX, int screenY)
        {
            var client = new NativeMethods.POINT(screenX, screenY);
            if (!NativeMethods.ScreenToClient(TargetWindow, ref client)) return;

            IntPtr lParam = NativeMethods.MakeLParam(client.X, client.Y);

            NativeMethods.PostMessage(TargetWindow, NativeMethods.WM_MOUSEMOVE, IntPtr.Zero, lParam);
            NativeMethods.PostMessage(TargetWindow, _downMessage, (IntPtr)_mouseWParam, lParam);

            if (ClickHoldMs > 0) PreciseDelay(ClickHoldMs);

            NativeMethods.PostMessage(TargetWindow, _upMessage, IntPtr.Zero, lParam);
        }

        /// <summary>Игровой режим: возвращаем окно в фокус, иначе SendInput уйдёт не туда.</summary>
        private void EnsureTargetForeground()
        {
            if (!IsTargetValid()) return;
            if (NativeMethods.GetForegroundWindow() == TargetWindow) return;
            if ((DateTime.UtcNow - _lastActivation).TotalMilliseconds < 700) return;

            _lastActivation = DateTime.UtcNow;

            try
            {
                if (NativeMethods.IsIconic(TargetWindow))
                    NativeMethods.ShowWindow(TargetWindow, NativeMethods.SW_RESTORE);

                uint foregroundThread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
                uint currentThread = NativeMethods.GetCurrentThreadId();

                if (foregroundThread != currentThread)
                {
                    NativeMethods.AttachThreadInput(foregroundThread, currentThread, true);
                    try { NativeMethods.SetForegroundWindow(TargetWindow); }
                    finally { NativeMethods.AttachThreadInput(foregroundThread, currentThread, false); }
                }
                else
                {
                    NativeMethods.SetForegroundWindow(TargetWindow);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Engine] focus: {ex.Message}");
            }
        }

        private void PreciseDelay(int milliseconds)
        {
            if (milliseconds <= 0) return;

            long end = Stopwatch.GetTimestamp() + milliseconds * Stopwatch.Frequency / 1000;
            while (!_stopRequested)
            {
                long remain = end - Stopwatch.GetTimestamp();
                if (remain <= 0) return;

                double ms = remain * 1000.0 / Stopwatch.Frequency;
                if (ms > 2.0) Thread.Sleep(1);
                else Thread.SpinWait(30);
            }
        }

        /// <summary>Абсолютные координаты с кэшем метрик экрана (обновление раз в секунду).</summary>
        private void ToAbsolute(int screenX, int screenY, out int absX, out int absY)
        {
            long now = Stopwatch.GetTimestamp();
            if (_virtualWidth <= 1 || now - _metricsStamp > Stopwatch.Frequency)
            {
                _virtualX = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
                _virtualY = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
                _virtualWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
                _virtualHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
                _metricsStamp = now;
            }

            int width = Math.Max(2, _virtualWidth);
            int height = Math.Max(2, _virtualHeight);

            absX = (int)Math.Round((screenX - _virtualX) * 65535.0 / (width - 1));
            absY = (int)Math.Round((screenY - _virtualY) * 65535.0 / (height - 1));
        }

        private void RegisterClick()
        {
            Interlocked.Increment(ref _clicks);
            int position = Interlocked.Increment(ref _clickStampPos);
            Volatile.Write(ref _clickStamps[position & 511], Stopwatch.GetTimestamp());
        }
    }
}
