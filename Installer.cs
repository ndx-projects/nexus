using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace NdxNexus
{
    // Het eigenlijke installeren (gedeeld door het venster en de stille modus /S).
    internal static class InstallEngine
    {
        public static string DefaultDir(Manifest m)
        {
            if (m.PerMachine)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), SafeName(m.Publisher), SafeName(m.Name));
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", SafeName(m.Name));
        }

        public static string SafeName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Programma";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }

        public static bool IsAdmin()
        {
            using (WindowsIdentity id = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static string RegPath(Manifest m)
        {
            return @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + m.Id;
        }

        public static void MakeShortcut(string lnk, string target, string workDir, string icon, string description)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lnk));
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            dynamic link = shell.CreateShortcut(lnk);
            link.TargetPath = target;
            link.WorkingDirectory = workDir;
            link.Description = description;
            if (!string.IsNullOrEmpty(icon)) link.IconLocation = icon;
            link.Save();
            Marshal.FinalReleaseComObject(link);
            Marshal.FinalReleaseComObject(shell);
        }

        // progress: (fractie 0..1, tekst)
        public static void Install(Payload p, string dir, bool desktop, bool startMenu, bool taskbar, bool autostart, Action<double, string> progress)
        {
            Manifest m = p.Manifest;
            string root = Path.GetFullPath(dir).TrimEnd('\\') + "\\";
            Directory.CreateDirectory(root);

            InstallLog log = new InstallLog();
            log.Id = m.Id; log.Name = m.Name; log.Version = m.Version; log.Publisher = m.Publisher;
            log.Dir = root.TrimEnd('\\'); log.PerMachine = m.PerMachine; log.Lang = L.Lang;
            log.Files = new List<string>(); log.Dirs = new List<string>(); log.Shortcuts = new List<string>();

            using (ZipArchive za = p.OpenZip())
            {
                List<ZipArchiveEntry> entries = za.Entries
                    .Where(e => e.FullName.StartsWith("app/", StringComparison.Ordinal) && !e.FullName.EndsWith("/", StringComparison.Ordinal))
                    .ToList();
                long total = 0;
                foreach (ZipArchiveEntry e in entries) total += e.Length;
                long done = 0;
                HashSet<string> dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ZipArchiveEntry e in entries)
                {
                    string rel = e.FullName.Substring(4).Replace('/', '\\');
                    string target = Path.GetFullPath(Path.Combine(root, rel));
                    if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue; // nooit buiten de doelmap schrijven
                    string d = Path.GetDirectoryName(target);
                    while (d != null && d.Length >= root.Length - 1 && !string.Equals(d.TrimEnd('\\') + "\\", root, StringComparison.OrdinalIgnoreCase))
                    {
                        dirs.Add(d);
                        d = Path.GetDirectoryName(d);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    e.ExtractToFile(target, true);
                    log.Files.Add(target);
                    done += e.Length;
                    progress(total == 0 ? 0.9 : 0.9 * done / total, rel);
                }
                log.Dirs.AddRange(dirs.OrderByDescending(x => x.Length));

                ZipArchiveEntry ic = za.GetEntry("icon.ico");
                if (ic != null)
                {
                    string icoPath = Path.Combine(root, "app.ico");
                    ic.ExtractToFile(icoPath, true);
                    log.Files.Add(icoPath);
                }
            }

            progress(0.92, L.T("inst.unins"));
            string unins = Path.Combine(root, "uninstall.exe");
            using (FileStream src = new FileStream(p.ExePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream dst = new FileStream(unins, FileMode.Create, FileAccess.Write))
            {
                byte[] buf = new byte[81920];
                long left = p.StubLength;
                while (left > 0)
                {
                    int n = src.Read(buf, 0, (int)Math.Min(buf.Length, left));
                    if (n <= 0) break;
                    dst.Write(buf, 0, n);
                    left -= n;
                }
            }
            log.Files.Add(unins);

            string mainExe = Path.Combine(root, m.MainExe.Replace('/', '\\'));
            string iconLoc = File.Exists(Path.Combine(root, "app.ico")) ? Path.Combine(root, "app.ico") : mainExe;

            progress(0.95, L.T("inst.shortcuts"));
            if (startMenu)
            {
                string programs = Environment.GetFolderPath(m.PerMachine ? Environment.SpecialFolder.CommonPrograms : Environment.SpecialFolder.Programs);
                string folder = Path.Combine(programs, SafeName(m.Name));
                string l1 = Path.Combine(folder, SafeName(m.Name) + ".lnk");
                string l2 = Path.Combine(folder, SafeName(L.T("un.link", m.Name)) + ".lnk");
                MakeShortcut(l1, mainExe, Path.GetDirectoryName(mainExe), iconLoc, m.Name);
                MakeShortcut(l2, unins, root, unins, L.T("un.link", m.Name));
                log.Shortcuts.Add(l1);
                log.Shortcuts.Add(l2);
                log.Dirs.Add(folder);
            }
            if (desktop)
            {
                string desk = Environment.GetFolderPath(m.PerMachine ? Environment.SpecialFolder.CommonDesktopDirectory : Environment.SpecialFolder.DesktopDirectory);
                string l = Path.Combine(desk, SafeName(m.Name) + ".lnk");
                MakeShortcut(l, mainExe, Path.GetDirectoryName(mainExe), iconLoc, m.Name);
                log.Shortcuts.Add(l);
            }
            if (taskbar)
            {
                // Windows heeft geen officiele API om vast te maken; dit is de bekende beste poging (per gebruiker).
                try
                {
                    string tb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                    string l = Path.Combine(tb, SafeName(m.Name) + ".lnk");
                    MakeShortcut(l, mainExe, Path.GetDirectoryName(mainExe), iconLoc, m.Name);
                    log.Shortcuts.Add(l);
                }
                catch (Exception) { }
            }

            progress(0.98, L.T("inst.register"));
            RegistryKey hive = m.PerMachine ? Registry.LocalMachine : Registry.CurrentUser;
            using (RegistryKey k = hive.CreateSubKey(RegPath(m)))
            {
                k.SetValue("DisplayName", m.Name);
                k.SetValue("DisplayVersion", m.Version ?? "");
                k.SetValue("Publisher", m.Publisher ?? "");
                k.SetValue("InstallLocation", root.TrimEnd('\\'));
                k.SetValue("DisplayIcon", iconLoc);
                k.SetValue("UninstallString", "\"" + unins + "\"");
                k.SetValue("QuietUninstallString", "\"" + unins + "\" /S");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", (int)Math.Min(int.MaxValue, m.TotalBytes / 1024), RegistryValueKind.DWord);
            }
            if (autostart)
            {
                using (RegistryKey rk = hive.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    rk.SetValue(m.Id, "\"" + mainExe + "\"");
                }
                log.Autostart = true;
            }

            if (Edition.IsOpen)
            {
                // de verwijderaar heeft geen payload meer; daarom blijft de vormgeving naast het programma staan
                string bd = Path.Combine(root, "nexus-brand");
                foreach (string bn in new string[] { Theme.BrandFile, Theme.LogoFile, Theme.SideFile, Theme.SplashFile })
                {
                    byte[] bb = p.ReadEntry(bn);
                    if (bb == null) continue;
                    Directory.CreateDirectory(bd);
                    string bf = Path.Combine(bd, bn);
                    File.WriteAllBytes(bf, bb);
                    log.Files.Add(bf);
                }
                if (Directory.Exists(bd)) log.Dirs.Insert(0, bd);
            }

            File.WriteAllText(Path.Combine(root, "install.json"), JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            progress(1.0, L.T("inst.done"));
        }

        public static int RunSilent(Payload p, string[] args)
        {
            string dir = DefaultDir(p.Manifest);
            foreach (string a in args)
            {
                if (a.StartsWith("/D=", StringComparison.OrdinalIgnoreCase)) dir = a.Substring(3).Trim('"');
            }
            if (p.Manifest.PerMachine && !IsAdmin()) return 5; // toegang geweigerd
            try
            {
                Install(p, dir, p.Manifest.DesktopShortcut, p.Manifest.StartMenu, p.Manifest.TaskbarPin, p.Manifest.Autostart, delegate { });
                return 0;
            }
            catch (Exception)
            {
                return 1;
            }
        }
    }

    // De wizard in NDX-huisstijl: links een donker merkpaneel, rechts de pagina's op lichtgrijs.
    internal sealed class InstallerWindow : Window
    {
        private enum Step { Welcome, License, Folder, Options, Install, Done }

        private readonly Payload _p;
        private readonly Manifest _m;
        private Step _step = Step.Welcome;
        private readonly ContentControl _page = new ContentControl();
        private readonly NdxButton _back = new NdxButton("", false, false);
        private readonly NdxButton _next = new NdxButton("", true, false);
        private readonly NdxButton _cancel = new NdxButton("", false, false);
        private readonly TextBlock _verText;
        private readonly TextBlock _nameText;
        private TextBox _dirBox;
        private NdxCheck _accept;
        private NdxCheck _deskCheck;
        private NdxCheck _menuCheck;
        private NdxCheck _taskCheck;
        private NdxCheck _autoCheck;
        private NdxCheck _launchCheck;
        private string _dir;
        private bool _desktop;
        private bool _menu;
        private bool _taskbar;
        private bool _autostart;
        private bool _launch;
        private bool _accepted;
        private bool _installing;
        private bool _failed;
        private string _errorText;
        private string _licenseText;
        private readonly Dictionary<string, string> _licenses = new Dictionary<string, string>();
        private bool _showOriginal;
        private Border _fill;
        private TextBlock _fileText;
        private TextBlock _percentText;
        private double _barWidth = 440;

        public InstallerWindow(Payload p)
        {
            _p = p;
            _m = p.Manifest;
            _dir = InstallEngine.DefaultDir(_m);
            _desktop = _m.DesktopShortcut;
            _menu = _m.StartMenu;
            _taskbar = _m.TaskbarPin;
            _autostart = _m.Autostart;
            _launch = _m.LaunchAfter;
            if (_m.HasLicense && _m.LicenseLangs != null)
            {
                foreach (string lc in _m.LicenseLangs)
                {
                    byte[] licBytes = p.ReadEntry("license." + lc + ".txt");
                    if (licBytes != null) _licenses[lc] = Encoding.UTF8.GetString(licBytes);
                }
                if (_licenses.Count > 0) _licenseText = "";   // niet null: de licentiepagina bestaat
            }

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Width = 780;
            Height = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Ui.Br(Color.FromRgb(244, 246, 250));
            ImageSource ico = Ui.IconFromBytes(p.ReadEntry("icon.ico"));
            if (ico != null) Icon = ico;

            Border frame = new Border();
            frame.BorderBrush = Ui.Br(Ui.Line);
            frame.BorderThickness = new Thickness(1);
            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            frame.Child = root;
            Content = frame;

            // links: donker merkpaneel met routepatroon
            Grid left = new Grid();
            left.Background = Theme.SidePanelBrush();
            left.MouseLeftButtonDown += delegate { try { DragMove(); } catch (Exception) { } };
            StackPanel lt = new StackPanel();
            lt.VerticalAlignment = VerticalAlignment.Top;
            lt.Margin = new Thickness(24, 34, 24, 0);
            UIElement logo = Theme.LogoElement(190, _m.Name, 22);
            if (logo != null) lt.Children.Add(logo);
            left.Children.Add(lt);
            StackPanel lb = new StackPanel();
            lb.VerticalAlignment = VerticalAlignment.Bottom;
            lb.Margin = new Thickness(24, 0, 24, 26);
            _nameText = Ui.Text(_m.Name, 20, Colors.White, true, true);
            _nameText.TextWrapping = TextWrapping.Wrap;
            lb.Children.Add(_nameText);
            _verText = Ui.Text("", 13, Color.FromRgb(174, 184, 204), false, false);
            _verText.Margin = new Thickness(0, 4, 0, 0);
            lb.Children.Add(_verText);
            left.Children.Add(lb);
            Grid.SetColumn(left, 0);
            root.Children.Add(left);

            // rechts: pagina + voetbalk
            Grid right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(right, 1);
            root.Children.Add(right);

            Grid pageHost = new Grid();
            pageHost.Margin = new Thickness(34, 26, 34, 10);
            pageHost.Children.Add(_page);
            Grid.SetRow(pageHost, 0);
            right.Children.Add(pageHost);

            StackPanel foot = new StackPanel();
            foot.Background = Ui.Br(Colors.White);
            foot.Children.Add(Ui.GradientLine(3));
            Grid fg = new Grid();
            fg.Margin = new Thickness(26, 14, 26, 16);
            fg.Children.Add(_cancel);
            _cancel.HorizontalAlignment = HorizontalAlignment.Left;
            StackPanel nav = new StackPanel();
            nav.Orientation = Orientation.Horizontal;
            nav.HorizontalAlignment = HorizontalAlignment.Right;
            _back.Margin = new Thickness(0, 0, 10, 0);
            nav.Children.Add(_back);
            nav.Children.Add(_next);
            fg.Children.Add(nav);
            foot.Children.Add(fg);
            Grid.SetRow(foot, 1);
            right.Children.Add(foot);

            _next.Click += OnNext;
            _back.Click += OnBack;
            _cancel.Click += OnCancel;
            KeyDown += delegate (object s, KeyEventArgs e) { if (e.Key == Key.Escape) OnCancel(); };
            Closing += delegate (object s, System.ComponentModel.CancelEventArgs e) { if (_installing) e.Cancel = true; };

            ShowStep(Step.Welcome);
        }

        private List<Step> Steps()
        {
            List<Step> l = new List<Step>();
            l.Add(Step.Welcome);
            if (_licenseText != null) l.Add(Step.License);
            l.Add(Step.Folder);
            l.Add(Step.Options);
            l.Add(Step.Install);
            l.Add(Step.Done);
            return l;
        }

        // Bewaart wat de gebruiker op de huidige pagina heeft ingevuld voordat de pagina wisselt of ververst.
        private void Capture()
        {
            if (_step == Step.Folder && _dirBox != null) _dir = _dirBox.Text.Trim();
            if (_step == Step.License && _accept != null) _accepted = _accept.IsChecked;
            if (_step == Step.Options && _menuCheck != null)
            {
                _menu = _menuCheck.IsChecked;
                _desktop = _deskCheck.IsChecked;
                _taskbar = _taskCheck.IsChecked;
                _autostart = _autoCheck.IsChecked;
                _launch = _launchCheck.IsChecked;
            }
        }

        private void OnNext()
        {
            if (_step == Step.Done)
            {
                if (!_failed && _launch) LaunchApp();
                Close();
                return;
            }
            Capture();
            if (_step == Step.Folder && (string.IsNullOrWhiteSpace(_dir) || !Path.IsPathRooted(_dir)))
            {
                MessageBox.Show(this, L.T("folder.invalid", "C:\\Programs\\" + _m.Name), _m.Name);
                return;
            }
            List<Step> s = Steps();
            ShowStep(s[s.IndexOf(_step) + 1]);
        }

        private void OnBack()
        {
            Capture();
            List<Step> s = Steps();
            int i = s.IndexOf(_step);
            if (i > 0) ShowStep(s[i - 1]);
        }

        private void OnCancel()
        {
            if (_installing) return;
            if (_step == Step.Done) { Close(); return; }
            if (MessageBox.Show(this, L.T("cancel.confirm"), _m.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) Close();
        }

        private void LaunchApp()
        {
            try
            {
                string exe = Path.Combine(_dir, _m.MainExe.Replace('/', '\\'));
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exe) });
            }
            catch (Exception)
            {
            }
        }

        private static StackPanel PageBase(string title, string intro, out StackPanel body)
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(Ui.Text(title, 26, Ui.Navy, true, true));
            if (!string.IsNullOrEmpty(intro))
            {
                TextBlock t = Ui.Text(intro, 14, Color.FromRgb(60, 70, 95), false, false);
                t.TextWrapping = TextWrapping.Wrap;
                t.Margin = new Thickness(0, 10, 0, 0);
                sp.Children.Add(t);
            }
            body = new StackPanel();
            body.Margin = new Thickness(0, 16, 0, 0);
            sp.Children.Add(body);
            return sp;
        }

        // Taalkeuze: vijf knoppen naast elkaar; de gekozen taal is gevuld.
        private UIElement LanguageBar()
        {
            StackPanel wrap = new StackPanel();
            wrap.Margin = new Thickness(0, 22, 0, 0);
            TextBlock lab = Ui.Text(L.T("language"), 12, Color.FromRgb(90, 100, 125), false, false);
            lab.Margin = new Thickness(0, 0, 0, 6);
            wrap.Children.Add(lab);
            WrapPanel row = new WrapPanel();
            for (int i = 0; i < L.Codes.Length; i++)
            {
                string code = L.Codes[i];
                bool on = code == L.Lang;
                Border chip = new Border();
                chip.CornerRadius = new CornerRadius(16);
                chip.Padding = new Thickness(14, 6, 14, 6);
                chip.Margin = new Thickness(0, 0, 8, 8);
                chip.Cursor = Cursors.Hand;
                chip.BorderThickness = new Thickness(1.5);
                chip.BorderBrush = Ui.Br(on ? Ui.Cyan : Color.FromRgb(190, 200, 215));
                chip.Background = on ? Ui.Br(Ui.Cyan) : Brushes.Transparent;
                TextBlock t = Ui.Text(L.Names[i], 13, on ? Colors.White : Ui.Navy, false, on);
                chip.Child = t;
                chip.MouseLeftButtonUp += delegate
                {
                    L.Set(code);
                    ShowStep(_step);
                };
                row.Children.Add(chip);
            }
            wrap.Children.Add(row);
            return wrap;
        }

        private void ShowStep(Step s)
        {
            _step = s;
            Title = L.T("title.install", _m.Name);
            _verText.Text = L.T("version", _m.Version);
            _back.Text = L.T("btn.back");
            _cancel.Text = L.T("btn.cancel");
            _back.Visibility = (s == Step.Welcome || s == Step.Install || s == Step.Done) ? Visibility.Hidden : Visibility.Visible;
            _cancel.Visibility = (s == Step.Install) ? Visibility.Hidden : Visibility.Visible;
            _next.IsEnabled = true;
            _next.Text = L.T("btn.next");
            StackPanel body;

            if (s == Step.Welcome)
            {
                StackPanel sp = PageBase(L.T("welcome.title"), L.T("welcome.intro", _m.Name), out body);
                body.Children.Add(Ui.Text(L.T("publisher", _m.Publisher), 14, Ui.Navy, false, false));
                TextBlock sz = Ui.Text(L.T("space", FormatSize(_m.TotalBytes)), 14, Ui.Navy, false, false);
                sz.Margin = new Thickness(0, 6, 0, 0);
                body.Children.Add(sz);
                body.Children.Add(LanguageBar());
                string tl = Theme.TaglineText();
                if (tl != null)
                {
                    TextBlock tag = Ui.Text(tl, 14, Ui.Cyan, true, true);
                    tag.TextWrapping = TextWrapping.Wrap;
                    tag.Margin = new Thickness(0, 14, 0, 0);
                    body.Children.Add(tag);
                }
                _next.Text = L.T("btn.start");
                _page.Content = sp;
            }
            else if (s == Step.License)
            {
                string src = _m.LicenseSource ?? "";
                string shown = _licenses.ContainsKey(L.Lang) ? L.Lang : (_licenses.ContainsKey(src) ? src : null);
                if (shown == null) foreach (string k in _licenses.Keys) { shown = k; break; }
                bool translated = _m.LicenseMt != null && shown != null && _m.LicenseMt.Contains(shown);
                if (_showOriginal && _licenses.ContainsKey(src)) { shown = src; translated = false; }
                StackPanel sp = PageBase(L.T("license.title"), L.T("license.intro"), out body);
                TextBox tb = Ui.Input(false);
                tb.Text = shown == null ? "" : _licenses[shown];
                tb.IsReadOnly = true;
                tb.Height = translated || (_m.LicenseMt != null && _m.LicenseMt.Count > 0) ? 170 : 200;
                tb.TextWrapping = TextWrapping.Wrap;
                tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                body.Children.Add(tb);
                int si = Array.IndexOf(L.Codes, src);
                if (translated && si >= 0)
                {
                    TextBlock mt = Ui.Text(L.T("license.mt", L.Names[si]), 12, Color.FromRgb(90, 100, 125), false, false);
                    mt.TextWrapping = TextWrapping.Wrap;
                    mt.Margin = new Thickness(0, 8, 0, 0);
                    body.Children.Add(mt);
                }
                if (si >= 0 && _licenses.ContainsKey(src) && src != L.Lang && _m.LicenseMt != null && _m.LicenseMt.Count > 0)
                {
                    TextBlock link = Ui.Text(L.T(_showOriginal ? "license.showtrans" : "license.showorig", L.Names[si]), 12, Ui.Cyan, true, true);
                    link.Cursor = Cursors.Hand;
                    link.Margin = new Thickness(0, 4, 0, 0);
                    link.MouseLeftButtonUp += delegate { Capture(); _showOriginal = !_showOriginal; ShowStep(Step.License); };
                    body.Children.Add(link);
                }
                _accept = new NdxCheck(L.T("license.accept"), _accepted, false);
                _accept.Margin = new Thickness(0, 10, 0, 0);
                _accept.Changed += delegate { _next.IsEnabled = _accept.IsChecked; };
                body.Children.Add(_accept);
                _next.IsEnabled = _accepted;
                _page.Content = sp;
            }
            else if (s == Step.Folder)
            {
                StackPanel sp = PageBase(L.T("folder.title"), L.T("folder.intro", _m.Name), out body);
                _dirBox = Ui.Input(false);
                _dirBox.Text = _dir;
                Grid row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Children.Add(_dirBox);
                NdxButton browse = new NdxButton(L.T("btn.browse"), false, false);
                browse.Margin = new Thickness(10, 0, 0, 0);
                browse.MinWidth = 90;
                browse.Click += delegate
                {
                    using (System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        d.Description = L.T("folder.dialog");
                        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            _dirBox.Text = Path.Combine(d.SelectedPath, InstallEngine.SafeName(_m.Name));
                        }
                    }
                };
                Grid.SetColumn(browse, 1);
                row.Children.Add(browse);
                body.Children.Add(row);
                TextBlock note = Ui.Text(L.T(_m.PerMachine ? "folder.all" : "folder.user"), 13, Color.FromRgb(90, 100, 125), false, false);
                note.TextWrapping = TextWrapping.Wrap;
                note.Margin = new Thickness(0, 12, 0, 0);
                body.Children.Add(note);
                _page.Content = sp;
            }
            else if (s == Step.Options)
            {
                StackPanel sp = PageBase(L.T("opt.title"), L.T("opt.intro"), out body);
                _menuCheck = new NdxCheck(L.T("opt.menu"), _menu, false);
                _deskCheck = new NdxCheck(L.T("opt.desktop"), _desktop, false);
                _taskCheck = new NdxCheck(L.T("opt.taskbar"), _taskbar, false);
                _autoCheck = new NdxCheck(L.T("opt.autostart"), _autostart, false);
                _launchCheck = new NdxCheck(L.T("opt.launch", _m.Name), _launch, false);
                body.Children.Add(_menuCheck);
                body.Children.Add(_deskCheck);
                body.Children.Add(_taskCheck);
                body.Children.Add(_autoCheck);
                body.Children.Add(_launchCheck);
                TextBlock note = Ui.Text(L.T("opt.taskbarnote"), 12, Color.FromRgb(90, 100, 125), false, false);
                note.TextWrapping = TextWrapping.Wrap;
                note.Margin = new Thickness(0, 12, 0, 0);
                body.Children.Add(note);
                _next.Text = L.T("btn.install");
                _page.Content = sp;
            }
            else if (s == Step.Install)
            {
                StackPanel sp = PageBase(L.T("inst.title"), L.T("inst.intro", _m.Name), out body);
                Border track = new Border();
                track.Width = _barWidth;
                track.Height = 10;
                track.CornerRadius = new CornerRadius(5);
                track.Background = Ui.Br(Ui.Grey);
                track.HorizontalAlignment = HorizontalAlignment.Left;
                _fill = new Border();
                _fill.Width = 0;
                _fill.Height = 10;
                _fill.CornerRadius = new CornerRadius(5);
                _fill.Background = Ui.BrandGradient();
                _fill.HorizontalAlignment = HorizontalAlignment.Left;
                Grid g = new Grid();
                g.Width = _barWidth;
                g.HorizontalAlignment = HorizontalAlignment.Left;
                g.Children.Add(track);
                g.Children.Add(_fill);
                body.Children.Add(g);
                _percentText = Ui.Text("0%", 14, Ui.Navy, true, true);
                _percentText.Margin = new Thickness(0, 14, 0, 0);
                body.Children.Add(_percentText);
                _fileText = Ui.Text("", 12, Color.FromRgb(90, 100, 125), false, false);
                _fileText.Margin = new Thickness(0, 4, 0, 0);
                _fileText.TextTrimming = TextTrimming.CharacterEllipsis;
                _fileText.TextWrapping = TextWrapping.NoWrap;
                body.Children.Add(_fileText);
                _next.IsEnabled = false;
                _page.Content = sp;
                StartInstall();
            }
            else
            {
                string title = L.T(_failed ? "fail.title" : "done.title");
                string intro = L.T(_failed ? "fail.intro" : "done.intro", _m.Name);
                StackPanel sp = PageBase(title, intro, out body);
                if (_failed)
                {
                    TextBlock er = Ui.Text(_errorText ?? "", 13, Color.FromRgb(147, 0, 10), false, false);
                    er.TextWrapping = TextWrapping.Wrap;
                    body.Children.Add(er);
                }
                else
                {
                    string tl2 = Theme.TaglineText();
                    if (tl2 != null)
                    {
                        TextBlock tag = Ui.Text(tl2, 14, Ui.Cyan, true, true);
                        tag.TextWrapping = TextWrapping.Wrap;
                        tag.Margin = new Thickness(0, 14, 0, 0);
                        body.Children.Add(tag);
                    }
                }
                _next.Text = (!_failed && _launch) ? L.T("btn.launchclose") : L.T("btn.close");
                _cancel.Visibility = Visibility.Hidden;
                _page.Content = sp;
            }
        }

        private void StartInstall()
        {
            _installing = true;
            _back.Visibility = Visibility.Hidden;
            string dir = _dir;
            bool desktop = _desktop;
            bool menu = _menu;
            bool taskbar = _taskbar;
            bool autostart = _autostart;
            Task.Run(delegate
            {
                try
                {
                    if (_p.IsPreview)
                    {
                        for (int i = 1; i <= 20; i++)
                        {
                            System.Threading.Thread.Sleep(90);
                            double f = i / 20.0;
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                _fill.Width = _barWidth * f;
                                _percentText.Text = Math.Round(f * 100) + "%";
                                _fileText.Text = L.T("preview.note");
                            }));
                        }
                    }
                    else
                    InstallEngine.Install(_p, dir, desktop, menu, taskbar, autostart, delegate (double f, string file)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            _fill.Width = _barWidth * Math.Max(0, Math.Min(1, f));
                            _percentText.Text = Math.Round(f * 100) + "%";
                            _fileText.Text = file;
                        }));
                    });
                    _failed = false;
                }
                catch (Exception ex)
                {
                    _failed = true;
                    _errorText = ex.Message;
                }
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _installing = false;
                    ShowStep(Step.Done);
                }));
            });
        }

        private static string FormatSize(long bytes)
        {
            double mb = bytes / 1048576.0;
            if (mb < 1) return Math.Max(1, bytes / 1024) + " KB";
            if (mb < 1024) return Math.Round(mb) + " MB";
            return Math.Round(mb / 1024, 1) + " GB";
        }
    }
}
