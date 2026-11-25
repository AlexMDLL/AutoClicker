using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AutoClicker
{
    /// <summary>
    /// Автонажатие клавиш: несколько привязок, своя скорость, режим «одновременно/по очереди».
    /// Отправляет ввод через SendInput (NativeMethods), поток отдельный.
    /// </summary>
    public sealed class KeyPresser
    {
        /// <summary>Отправка одной клавиши; заменяется в тестах, чтобы не слать реальный ввод.</summary>
        public static Action<ushort, bool> KeySender = (vk, down) =>
        {
            if (down) NativeMethods.KeyDown(vk);
            else NativeMethods.KeyUp(vk);
        };

        public int IntervalMs { get; set; } = 100;
        public bool Simultaneous { get; set; } = true;
        public long Presses { get; private set; }
        public bool IsRunning => _running;

        private Thread _thread;
        private volatile bool _running;
        private List<KeyBindingEntry> _bindings = new List<KeyBindingEntry>();
        private int _sequenceIndex;

        public void SetBindings(IEnumerable<KeyBindingEntry> bindings)
        {
            _bindings = bindings == null
                ? new List<KeyBindingEntry>()
                : bindings.Where(b => b != null && b.Enabled && b.VirtualKey > 0).ToList();
        }

        public void Start()
        {
            if (_running || _bindings.Count == 0) return;

            _running = true;
            _sequenceIndex = 0;
            Presses = 0;

            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "AutoClicker.Keys"
            };
            _thread.Start();
        }

        public void Stop()
        {
            if (!_running) return;

            _running = false;
            try { _thread?.Join(500); } catch { }
            _thread = null;
        }

        private void Run()
        {
            NativeMethods.timeBeginPeriod(1);
            try
            {
                var watch = Stopwatch.StartNew();
                long next = watch.ElapsedTicks;

                while (_running)
                {
                    PressCycle();

                    if (IntervalMs > 0)
                    {
                        next += (long)IntervalMs * Stopwatch.Frequency / 1000;
                        while (_running && watch.ElapsedTicks < next)
                            Thread.Sleep(1);

                        if (watch.ElapsedTicks > next + Stopwatch.Frequency / 2)
                            next = watch.ElapsedTicks;
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
            }
            finally
            {
                NativeMethods.timeEndPeriod(1);
            }
        }

        private void PressCycle()
        {
            if (_bindings.Count == 0) return;

            if (Simultaneous)
            {
                var keys = new List<ushort>();
                foreach (var binding in _bindings)
                {
                    AddModifiers(keys, binding);
                    AddKey(keys, (ushort)binding.VirtualKey);
                }
                PressKeys(keys);
            }
            else
            {
                var binding = _bindings[_sequenceIndex % _bindings.Count];
                _sequenceIndex = (_sequenceIndex + 1) % _bindings.Count;

                var keys = new List<ushort>();
                AddModifiers(keys, binding);
                AddKey(keys, (ushort)binding.VirtualKey);
                PressKeys(keys);
            }

            Presses++;
        }

        private static void AddModifiers(List<ushort> keys, KeyBindingEntry binding)
        {
            if (binding.Ctrl) AddKey(keys, 0x11);
            if (binding.Shift) AddKey(keys, 0x10);
            if (binding.Alt) AddKey(keys, 0x12);
            if (binding.Win) AddKey(keys, 0x5B);
        }

        private static void AddKey(List<ushort> keys, ushort vk)
        {
            if (!keys.Contains(vk)) keys.Add(vk);
        }

        private static void PressKeys(List<ushort> keys)
        {
            if (keys.Count == 0) return;

            foreach (ushort vk in keys) KeySender(vk, true);
            Thread.Sleep(2);
            for (int i = keys.Count - 1; i >= 0; i--) KeySender(keys[i], false);
        }
    }
}
