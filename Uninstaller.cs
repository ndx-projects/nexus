using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace NdxNexus
{
    internal static class UninstallEngine
    {
        public static InstallLog Load(string dir)
        {
            string f = Path.Combine(dir, "install.json");
            if (!File.Exists(f)) return null;
            try { return JsonSerializer.Deserialize<InstallLog>(File.ReadAllText(f)); }
            catch (Exception) { return null; }
        }

        public static void Run(InstallLog log, string self, Action<double, string> progress)
        {
            int total = log.Files.Count + log.Shortcuts.Count + log.Dirs.Count + 2;
            int n = 0;
            foreach (string f in log.Shortcuts)
            {
                TryDelete(f);
                progress(++n / (double)total, Path.GetFileName(f));
            }
            foreach (string f in log.Files)
            {
                if (string.Equals(Path.GetFullPath(f), Path.GetFullPath(self), StringComparison.OrdinalIgnoreCase)) { n++; continue; }
                TryDelete(f);
                progress(++n / (double)total, Path.GetFileName(f));
            }
            TryDelete(Path.Combine(log.Dir, "install.json"));
            foreach (string d in log.Dirs)
            {
                try { if (Directory.Exists(d) && Directory.GetFileSystemEntries(d).Length == 0) Directory.Delete(d); } catch (Exception) { }
                progress(++n / (double)total, "");
            }
            try
            {
                RegistryKey hive = log.PerMachine ? Registry.LocalMachine : Registry.CurrentUser;
                if (log.Autostart)
                {
                    using (RegistryKey rk = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (rk != null) rk.DeleteValue(log.Id, false);
                    }
                }
                hive.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + log.Id, false);
            }
            catch (Exception) { }
            progress(1.0, L.T("inst.done"));

            // uninstall.exe draait nog: laat cmd hem even later opruimen
            try
            {
                string cmd = "/c ping 127.0.0.1 -n 3 > nul & del /f /q \"" + self + "\" & rmdir \"" + log.Dir + "\"";
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
            }
            catch (Exception) { }
        }

        private static void TryDelete(string f)
        {
            try
            {
                if (File.Exists(f))
                {
                    File.SetAttributes(f, FileAttributes.Normal);
                    File.Delete(f);
                }
            }
            catch (Exception) { }
        }
    }

    internal sealed class UninstallerWindow : Window
    {
        private readonly InstallLog _log;
        private readonly string _self;
        private readonly NdxButton _go = new NdxButton(L.T("btn.uninstall"), true, true);
        private readonly NdxButton _no = new NdxButton(L.T("btn.cancel"), false, true);
        private readonly TextBlock _status;
        private readonly Border _fill = new Border();
        private const double BarW = 400;
        private bool _busy;
        private bool _finished;

        public UninstallerWindow(InstallLog log, string self)
        {
            _log = log;
            _self = self;
            Title = L.T("title.uninstall", log.Name);
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Width = 520;
            Height = 330;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Ui.Br(Ui.Navy);
            string icon = Path.Combine(log.Dir, "app.ico");
            try { if (File.Exists(icon)) Icon = Ui.IconFromBytes(File.ReadAllBytes(icon)); } catch (Exception) { }

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.Background = Edition.IsOpen ? Theme.SplashBrush() : new ImageBrush(Ui.Image("bg-route.png")) { Stretch = Stretch.UniformToFill, Opacity = 0.35 };
            root.MouseLeftButtonDown += delegate (object sender, System.Windows.Input.MouseButtonEventArgs e) { if (!e.Handled) { try { DragMove(); } catch (Exception) { } } };

            FrameworkElement logo = Theme.LogoElement(Edition.IsOpen ? 150 : 130, log.Name, 20) as FrameworkElement;
            if (logo != null)
            {
                logo.Margin = new Thickness(32, 28, 0, 0);
                logo.VerticalAlignment = VerticalAlignment.Top;
                root.Children.Add(logo);
            }

            StackPanel mid = new StackPanel();
            mid.Margin = new Thickness(32, 20, 32, 0);
            mid.VerticalAlignment = VerticalAlignment.Center;
            mid.Children.Add(Ui.Text(L.T("title.uninstall", log.Name), 24, Colors.White, true, true));
            TextBlock sub = Ui.Text(L.T("un.intro", log.Version), 14, Ui.Grey, false, false);
            sub.TextWrapping = TextWrapping.Wrap;
            sub.Margin = new Thickness(0, 8, 0, 0);
            mid.Children.Add(sub);
            Grid bar = new Grid();
            bar.Width = BarW;
            bar.Margin = new Thickness(0, 22, 0, 0);
            bar.HorizontalAlignment = HorizontalAlignment.Left;
            Border track = new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = Ui.Br(Ui.Line) };
            _fill.Height = 8; _fill.Width = 0; _fill.CornerRadius = new CornerRadius(4);
            _fill.Background = Ui.BrandGradient(); _fill.HorizontalAlignment = HorizontalAlignment.Left;
            bar.Children.Add(track); bar.Children.Add(_fill);
            bar.Visibility = Visibility.Hidden;
            mid.Children.Add(bar);
            _status = Ui.Text("", 12, Ui.Grey, false, false);
            _status.Margin = new Thickness(0, 8, 0, 0);
            mid.Children.Add(_status);
            Grid.SetRow(mid, 1);
            root.Children.Add(mid);

            StackPanel btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 32, 26) };
            _no.Margin = new Thickness(0, 0, 10, 0);
            btns.Children.Add(_no); btns.Children.Add(_go);
            Grid.SetRow(btns, 2);
            root.Children.Add(btns);

            Border frame = new Border { BorderBrush = Ui.Br(Ui.Line), BorderThickness = new Thickness(1), Child = root };
            Content = frame;

            _no.Click += delegate { if (!_busy) Close(); };
            _go.Click += delegate
            {
                if (_busy) return;
                if (_finished) { Close(); return; }
                _busy = true;
                _go.IsEnabled = false; _no.Visibility = Visibility.Hidden;
                bar.Visibility = Visibility.Visible;
                Task.Run(delegate
                {
                    UninstallEngine.Run(_log, _self, delegate (double f, string name)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate { _fill.Width = BarW * f; _status.Text = name; }));
                    });
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        _busy = false; _status.Text = L.T("un.done", _log.Name);
                        _finished = true;
                        _go.Text = L.T("btn.close"); _go.IsEnabled = true;
                    }));
                });
            };
        }
    }
}
