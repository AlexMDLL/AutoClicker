using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AutoClicker
{
    /// <summary>
    /// Плавная прокрутка колесом: вместо резкого прыжка ScrollViewer
    /// смещает содержимое с easing-анимацией.
    /// </summary>
    public static class SmoothScroll
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SmoothScroll),
                new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyProperty AnimationProperty =
            DependencyProperty.RegisterAttached("Animation", typeof(DispatcherTimer), typeof(SmoothScroll),
                new PropertyMetadata(null));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ScrollViewer viewer)) return;

            viewer.PreviewMouseWheel -= Viewer_PreviewMouseWheel;
            if ((bool)e.NewValue) viewer.PreviewMouseWheel += Viewer_PreviewMouseWheel;
        }

        private static void Viewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!(sender is ScrollViewer viewer)) return;
            if (HasInnerScrollViewer(viewer, e.OriginalSource)) return;
            if (viewer.ScrollableHeight <= 0) return;

            double target = Math.Max(0, Math.Min(viewer.ScrollableHeight, viewer.VerticalOffset - e.Delta * 0.9));
            e.Handled = true;

            Animate(viewer, target);
        }

        private static void Animate(ScrollViewer viewer, double target)
        {
            var existing = (DispatcherTimer)viewer.GetValue(AnimationProperty);
            existing?.Stop();

            double start = viewer.VerticalOffset;
            if (Math.Abs(target - start) < 0.5) return;

            var timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(15)
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const double duration = 190;

            timer.Tick += (s, e) =>
            {
                double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration);
                double eased = 1.0 - Math.Pow(1.0 - progress, 3);
                viewer.ScrollToVerticalOffset(start + (target - start) * eased);

                if (progress >= 1.0)
                {
                    timer.Stop();
                    viewer.SetValue(AnimationProperty, null);
                }
            };

            viewer.SetValue(AnimationProperty, timer);
            timer.Start();
        }

        /// <summary>Если под курсором вложенный ScrollViewer, прокручивать должен он.</summary>
        private static bool HasInnerScrollViewer(ScrollViewer viewer, object source)
        {
            var current = source as DependencyObject;
            while (current != null && current != viewer)
            {
                if (current is ScrollViewer inner && inner != viewer) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }
}
