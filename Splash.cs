using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NdxNexus
{
    // Opstartscherm in NDX-stijl. Het Studio-splash is één afbeelding; het installer-splash toont het logo met de productnaam.
    internal sealed class SplashWindow : Window
    {
        private const double W = 960;
        private const double H = 540;
        private readonly Border _bar = new Border();

        private SplashWindow(string title, string subtitle, bool studio)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = true;
            Topmost = true;
            Width = W;
            Height = H;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Ui.Br(Ui.Navy);
            Title = Edition.Name;

            Grid root = new Grid();
            if (studio)
            {
                Image img = new Image();
                img.Source = Ui.Image("splash-" + Edition.SplashKey + "-" + L.Lang + ".png");
                img.Stretch = Stretch.Uniform;
                root.Children.Add(img);
            }
            else
            {
                Border bg = new Border();
                bg.Background = Theme.SplashBrush();
                root.Children.Add(bg);

                StackPanel sp = new StackPanel();
                sp.VerticalAlignment = VerticalAlignment.Center;
                sp.HorizontalAlignment = HorizontalAlignment.Center;
                FrameworkElement logo = Theme.LogoElement(Edition.IsOpen ? 380 : 520, title, 40) as FrameworkElement;
                if (logo != null)
                {
                    logo.HorizontalAlignment = HorizontalAlignment.Center;
                    sp.Children.Add(logo);
                }
                TextBlock t = Ui.Text(title, 34, Colors.White, true, true);
                t.TextAlignment = TextAlignment.Center;
                t.Margin = new Thickness(0, logo != null ? 36 : 0, 0, 0);
                sp.Children.Add(t);
                TextBlock s = Ui.Text(subtitle, 17, Color.FromRgb(200, 210, 230), false, false);
                s.TextAlignment = TextAlignment.Center;
                s.Margin = new Thickness(0, 8, 0, 0);
                sp.Children.Add(s);
                root.Children.Add(sp);
            }

            _bar.Height = 4;
            _bar.Width = 0;
            _bar.Background = Ui.BrandGradient();
            _bar.HorizontalAlignment = HorizontalAlignment.Left;
            _bar.VerticalAlignment = VerticalAlignment.Bottom;
            root.Children.Add(_bar);
            Content = root;

            Loaded += delegate { Animate(); };
        }

        private void Animate()
        {
            DoubleAnimation a = new DoubleAnimation(0, W, TimeSpan.FromMilliseconds(1800));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            _bar.BeginAnimation(FrameworkElement.WidthProperty, a);
        }

        public static async Task ShowAsync(string title, string subtitle, bool studio, int ms)
        {
            SplashWindow w = new SplashWindow(title, subtitle, studio);
            w.Show();
            await Task.Delay(ms);
            DoubleAnimation f = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            f.Completed += delegate { w.Close(); tcs.SetResult(true); };
            w.BeginAnimation(OpacityProperty, f);
            await tcs.Task;
        }
    }
}
