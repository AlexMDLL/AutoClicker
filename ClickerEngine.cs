using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AutoClicker
{
    /// <summary>
    /// Движок кликера. Работает в отдельном потоке с точным таймером
    /// (timeBeginPeriod + Stopwatch + гибрид Sleep/SpinWait), поэтому интервалы
    /// не «плывут» на 15 мс, как у Task.Delay, и UI не подвисает.
    /// </summary>
    public sealed class ClickerEngine
    {
        private Thread _thread;
        private volatile bool _stopRequested;
        private volatile bool _running;
        private long _clicks;
        private long _startTicks;

        private readonly Random _rng = new Random();
        private readonly object _cpsLock = new object();
        private readonly Queue<long> _recentClicks = new Queue<long>();

        private DateTime _lastActivation = DateTime.MinValue;

        public event Action Started;
        public event Action<string> Stopped; // null — остановили вручную

        // === Настройки (меняются из UI, читаются потоком кликера) ===
        public int IntervalMs { get; set; } = 100;
        public bool UseCustomInterval { get; set; }
        public double CustomInterval { get; set; }
        public MouseButtonKind Button { get; set; } = MouseButtonKind.Left;
        public ClickMode Mode { get; set; } = ClickMode.Foreground;

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
                if (_startTicks == 0) return TimeSpan.Zero;
                long delta = Stopwatch.GetTimestamp() - _startTicks;
                return TimeSpan.FromSeconds(delta / (double)Stopwatch.Frequency);
            }
        }

        public double Cps
        {
            get
            {
                lock (_cpsLock)
                {
                    TrimCpsQueue();
                    return _recentClicks.Count;
                }
            }
        }

        public void Start()
        {
            if (_running) return;

            _stopRequested = false;
            Interlocked.Exchange(ref _clicks, 0);
            lock (_cpsLock) _recentClicks.Clear();
            _startTicks = Stopwatch.GetTimestamp();
            _running = true;
            _lastActivation = DateTime.MinValue;

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
            {
                // Поток просыпается минимум раз в пару миллисекунд, ждём недолго.
                thread.Join(250);
            }
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

                    if (Mode == ClickMode.Game) EnsureTargetForeground();

                    if (EnableCyclicMode)
                    {
                        double burst = Math.Max(0.05, ClickDuration);
                        var burstEnd = sw.Elapsed + TimeSpan.FromSeconds(burst);

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
            return Math.Max(1.0, ms);
        }

        /// <summary>Ждёт следующего клика, компенсируя накопленное отставание.</summary>
        private bool WaitInterval(Stopwatch sw, ref long nextTick, double intervalMs, ref bool first)
        {
            double jitter = 1.0;
            if (EnableHumanization && HumanizePercent > 0)
            {
                double max = Math.Min(90, HumanizePercent) / 100.0;
                jitter = 1.0 + (_rng.NextDouble() * 2.0 - 1.0) * max;
            }

            double actual = Math.Max(0.5, intervalMs * jitter);
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

            if (Mode == ClickMode.Background && TargetWindow != IntPtr.Zero)
            {
                if (IsTargetValid())
                {
                    ClickBackground(x, y);
                    RegisterClick();
                    return;
                }
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
            GetButtonFlags(out uint down, out uint up);

            NativeMethods.POINT original = default;
            bool restore = ReturnCursor && NativeMethods.GetCursorPos(out original);

            int hold = Math.Max(0, ClickHoldMs);
            if (hold == 0)
            {
                NativeMethods.SendMouseClick(screenX, screenY, down, up);
            }
            else
            {
                // В играх важно, чтобы кнопка была реально зажата какое-то время.
                NativeMethods.SendMouseDown(screenX, screenY, down);
                PreciseDelay(hold);
                NativeMethods.SendMouseUp(screenX, screenY, up);
            }

            if (restore) NativeMethods.SetCursorPos(original.X, original.Y);
        }

        private void ClickBackground(int screenX, int screenY)
        {
            var client = new NativeMethods.POINT(screenX, screenY);
            if (!NativeMethods.ScreenToClient(TargetWindow, ref client)) return;

            IntPtr lParam = NativeMethods.MakeLParam(client.X, client.Y);
            int down, up, mk;
            GetButtonMessages(out down, out up, out mk);

            // Двигаем «виртуальный» курсор внутри окна, затем кликаем.
            NativeMethods.PostMessage(TargetWindow, NativeMethods.WM_MOUSEMOVE, IntPtr.Zero, lParam);
            NativeMethods.PostMessage(TargetWindow, down, (IntPtr)mk, lParam);

            int hold = Math.Max(0, ClickHoldMs);
            if (hold > 0) PreciseDelay(hold);

            NativeMethods.PostMessage(TargetWindow, up, IntPtr.Zero, lParam);
        }

        private void GetButtonFlags(out uint down, out uint up)
        {
            switch (Button)
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
        }

        private void GetButtonMessages(out int down, out int up, out int mk)
        {
            switch (Button)
            {
                case MouseButtonKind.Right:
                    down = NativeMethods.WM_RBUTTONDOWN;
                    up = NativeMethods.WM_RBUTTONUP;
                    mk = NativeMethods.MK_RBUTTON;
                    break;
                case MouseButtonKind.Middle:
                    down = NativeMethods.WM_MBUTTONDOWN;
                    up = NativeMethods.WM_MBUTTONUP;
                    mk = NativeMethods.MK_MBUTTON;
                    break;
                default:
                    down = NativeMethods.WM_LBUTTONDOWN;
                    up = NativeMethods.WM_LBUTTONUP;
                    mk = NativeMethods.MK_LBUTTON;
                    break;
            }
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
            var sw = Stopwatch.StartNew();
            while (!_stopRequested && sw.ElapsedMilliseconds < milliseconds)
            {
                long remain = milliseconds - sw.ElapsedMilliseconds;
                if (remain > 2) Thread.Sleep(1);
                else Thread.SpinWait(30);
            }
        }

        private void RegisterClick()
        {
            Interlocked.Increment(ref _clicks);
            lock (_cpsLock)
            {
                _recentClicks.Enqueue(Stopwatch.GetTimestamp());
                TrimCpsQueue();
            }
        }

        private void TrimCpsQueue()
        {
            long threshold = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
            while (_recentClicks.Count > 0 && _recentClicks.Peek() < threshold)
                _recentClicks.Dequeue();
        }
    }
}
