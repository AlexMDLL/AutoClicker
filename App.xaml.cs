using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace AutoClicker
{
    public partial class App : Application
    {
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int value);

        private static Mutex _instanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _instanceMutex = new Mutex(true, "AutoClickerPro_SingleInstance_Mutex", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("AutoClicker Pro уже запущен.", "AutoClicker Pro",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // Per-Monitor DPI: чёткая картинка на мониторах с масштабом
            try { SetProcessDpiAwareness(2); }
            catch { }

            DispatcherUnhandledException += (s, args) =>
            {
                args.Handled = true;
                MessageBox.Show($"Произошла ошибка:\n{args.Exception.Message}", "AutoClicker Pro",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            };

            ThemeManager.Apply(ThemeManager.CreatePresets()[0]);

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _instanceMutex?.ReleaseMutex(); } catch { }
            _instanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
