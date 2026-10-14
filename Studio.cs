using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    internal sealed class ProjectFile
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string SourceDir { get; set; }
        public string MainExe { get; set; }
        public string IconFile { get; set; }
        public bool PerMachine { get; set; }
        public bool StartMenu { get; set; }
        public bool Desktop { get; set; }
        public bool Launch { get; set; }
        public bool Taskbar { get; set; }
        public bool Autostart { get; set; }

        // licentie
        public string LicenseSource { get; set; }
        public Dictionary<string, string> LicenseTexts { get; set; }
        public List<string> LicenseMt { get; set; }

        // vormgeving (Nexus wereldwijd)
        public BrandSpec Brand { get; set; }
        public string LogoFile { get; set; }
        public string SideFile { get; set; }
        public string SplashFile { get; set; }
    }

    // Rij keuzeknoppen (chips); precies een is gekozen.
    internal sealed class ChipBar : WrapPanel
    {
        public event Action<int> Changed;
        private readonly string[] _labels;
        private int _sel;

        public ChipBar(string[] labels, int selected)
        {
            _labels = labels;
            _sel = selected;
            Render();
        }

        public int Selected
        {
            get { return _sel; }
            set { _sel = value; Render(); }
        }

        private void Render()
        {
            Children.Clear();
            for (int i = 0; i < _labels.Length; i++)
            {
                int idx = i;
                bool on = i == _sel;
                Border chip = new Border();
                chip.CornerRadius = new CornerRadius(16);
                chip.Padding = new Thickness(14, 6, 14, 6);
                chip.Margin = new Thickness(0, 0, 8, 8);
                chip.Cursor = Cursors.Hand;
                chip.BorderThickness = new Thickness(1.5);
                chip.BorderBrush = Ui.Br(on ? Ui.Cyan : Ui.Line);
                chip.Background = on ? Ui.Br(Ui.Cyan) : Brushes.Transparent;
                chip.Child = Ui.Text(_labels[i], 13, on ? Colors.White : Ui.Grey, false, on);
                chip.MouseLeftButtonUp += delegate
                {
                    if (_sel == idx) return;
                    _sel = idx;
                    Render();
                    if (Changed != null) Changed(idx);
                };
                Children.Add(chip);
            }
        }
    }

    // Kleurveld: staal + hex-tekst + Kies-knop.
    internal sealed class ColorField : Grid
    {
        private readonly Border _swatch = new Border();
        private readonly TextBox _box = Ui.Input(true);

        public ColorField(string hex)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _swatch.CornerRadius = new CornerRadius(6);
            _swatch.BorderBrush = Ui.Br(Ui.Line);
            _swatch.BorderThickness = new Thickness(1);
            _swatch.Margin = new Thickness(0, 0, 8, 0);
            Children.Add(_swatch);
            Grid.SetColumn(_box, 1);
            Children.Add(_box);
            NdxButton pick = new NdxButton(L.T("btn.choose"), false, true);
            pick.MinWidth = 80;
            pick.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(pick, 2);
            Children.Add(pick);
            _box.Text = hex;
            _box.TextChanged += delegate { Paint(); };
            pick.Click += delegate
            {
                using (System.Windows.Forms.ColorDialog d = new System.Windows.Forms.ColorDialog())
                {
                    Color c = Value;
                    d.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
                    d.FullOpen = true;
                    if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _box.Text = "#" + d.Color.R.ToString("X2") + d.Color.G.ToString("X2") + d.Color.B.ToString("X2");
                    }
                }
            };
            Paint();
        }

        public string Hex
        {
            get { return _box.Text.Trim(); }
            set { _box.Text = value; }
        }

        public Color Value { get { return Theme.ParseColor(_box.Text, Colors.Gray); } }

        private void Paint()
        {
            _swatch.Background = Ui.Br(Value);
        }
    }

    // Hoofdvenster: hier maak je een setup.exe.
    internal sealed class StudioWindow : Window
    {
        private readonly TextBox _name = Ui.Input(true);
        private readonly TextBox _ver = Ui.Input(true);
        private readonly TextBox _pub = Ui.Input(true);
        private readonly TextBox _src = Ui.Input(true);
        private readonly TextBox _exe = Ui.Input(true);
        private readonly TextBox _ico = Ui.Input(true);
        private readonly NdxCheck _perMachine = new NdxCheck(L.T("chk.permachine"), false, true);
        private readonly NdxCheck _menu = new NdxCheck(L.T("chk.menu"), true, true);
        private readonly NdxCheck _desk = new NdxCheck(L.T("chk.desktop"), true, true);
        private readonly NdxCheck _launch = new NdxCheck(L.T("chk.launch"), true, true);
        private readonly NdxCheck _taskbar = new NdxCheck(L.T("chk.taskbar"), false, true);
        private readonly NdxCheck _autostart = new NdxCheck(L.T("chk.autostart"), false, true);
        private readonly NdxButton _build = new NdxButton(L.T("btn.build"), true, true);
        private readonly NdxButton _preview = new NdxButton(L.T("btn.preview"), false, true);
        private readonly TextBlock _status = Ui.Text("", 13, Ui.Grey, false, false);
        private readonly Border _fill = new Border();
        private const double BarW = 520;
        private string _lastOutput;
        private ChipBar _uiBar;

        // vormgeving (alleen Nexus wereldwijd)
        private readonly TextBox _logo = Ui.Input(true);
        private readonly TextBox _side = Ui.Input(true);
        private readonly TextBox _splash = Ui.Input(true);
        private readonly TextBox _tagline = Ui.Input(true);
        private readonly ColorField _cDark = new ColorField("#070B19");
        private readonly ColorField _cAccent = new ColorField("#0088FF");
        private readonly ColorField _cPrimary = new ColorField("#F2A900");
        private readonly NdxCheck _sysFont = new NdxCheck(L.T("chk.sysfont"), false, true);

        // licentie en vertaling
        private readonly string[] _licText = new string[5] { "", "", "", "", "" };
        private readonly bool[] _licMt = new bool[5];
        private int _licSrc = Math.Max(0, Array.IndexOf(L.Codes, L.Lang));
        private int _licCur = Math.Max(0, Array.IndexOf(L.Codes, L.Lang));
        private bool _licLoading;
        private ChipBar _srcBar;
        private ChipBar _tabBar;
        private readonly TextBox _licBox = Ui.Input(true);
        private readonly TextBlock _licInfo = Ui.Text("", 12, Ui.Grey, false, false);
        private ChipBar _provBar;
        private readonly TextBox _tKey = Ui.Input(true);
        private readonly TextBox _tUrl = Ui.Input(true);
        private readonly TextBox _tModel = Ui.Input(true);
        private readonly NdxCheck _tRemember = new NdxCheck(L.T("lic.remember"), false, true);
        private StackPanel _tUrlRow;
        private StackPanel _tModelRow;
        private readonly NdxButton _translate = new NdxButton(L.T("lic.translate"), true, true);
        private readonly TranslatorConfig _tcfg = new TranslatorConfig();

        public StudioWindow() : this(null, null)
        {
        }

        public StudioWindow(ProjectFile snapshot, TranslatorConfig translatorSnapshot)
        {
            Title = Edition.Name;
            Width = 1000;
            Height = 820;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Ui.Br(Ui.Navy);
            try { Icon = Ui.Image("nexus-icon.png"); } catch (Exception) { }
            _ver.Text = "1.0.0";
            _pub.Text = Edition.IsOpen ? "" : "NDX";

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // header
            Grid head = new Grid();
            head.Margin = new Thickness(32, 22, 32, 14);
            Image lock1 = new Image { Source = Ui.Image("nexus-lockup-white.png"), Height = 40, HorizontalAlignment = HorizontalAlignment.Left };
            head.Children.Add(lock1);
            StackPanel hb = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            _uiBar = new ChipBar(L.Names, Math.Max(0, Array.IndexOf(L.Codes, L.Lang)));
            _uiBar.Margin = new Thickness(0, 8, 18, 0);
            _uiBar.VerticalAlignment = VerticalAlignment.Center;
            _uiBar.Changed += delegate (int i) { SwitchLanguage(L.Codes[i]); };
            hb.Children.Add(_uiBar);
            NdxButton open = new NdxButton(L.T("btn.open_project"), false, true);
            NdxButton save = new NdxButton(L.T("btn.save_project"), false, true);
            save.Margin = new Thickness(10, 0, 0, 0);
            hb.Children.Add(open); hb.Children.Add(save);
            head.Children.Add(hb);
            StackPanel top = new StackPanel();
            top.Children.Add(head);
            top.Children.Add(Ui.GradientLine(3));
            Grid.SetRow(top, 0);
            root.Children.Add(top);

            // formulier
            StackPanel form = new StackPanel();
            form.Margin = new Thickness(32, 20, 32, 10);
            form.Children.Add(Ui.Text(L.T("s.new"), 24, Colors.White, true, true));
            TextBlock intro = Ui.Text(L.T("s.intro", Edition.Name) + L.T(Edition.IsOpen ? "s.intro.open" : "s.intro.ndx"), 14, Ui.Grey, false, false);
            intro.TextWrapping = TextWrapping.Wrap;
            intro.Margin = new Thickness(0, 6, 0, 16);
            form.Children.Add(intro);

            Grid two = new Grid();
            two.ColumnDefinitions.Add(new ColumnDefinition());
            two.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            two.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            two.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            two.ColumnDefinitions.Add(new ColumnDefinition());
            AddCell(two, 0, L.T("f.name"), _name);
            AddCell(two, 2, L.T("f.version"), _ver);
            AddCell(two, 4, L.T("f.publisher"), _pub);
            form.Children.Add(two);

            form.Children.Add(Pick(L.T("f.srcdir"), _src, L.T("btn.choose"), PickFolder));
            form.Children.Add(Pick(L.T("f.exe"), _exe, L.T("btn.choose"), PickExe));
            form.Children.Add(Pick(L.T("f.icon"), _ico, L.T("btn.choose"), delegate { PickFile(_ico, L.T("flt.ico")); }));

            form.Children.Add(SectionHead(L.T("sec.defaults")));
            WrapPanel opts = new WrapPanel();
            foreach (NdxCheck c in new NdxCheck[] { _menu, _desk, _taskbar, _autostart, _launch, _perMachine })
            {
                c.Margin = new Thickness(0, 0, 28, 0);
                opts.Children.Add(c);
            }
            form.Children.Add(opts);

            if (Edition.IsOpen) form.Children.Add(BuildBrandSection());
            form.Children.Add(BuildLicenseSection());

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = form };
            Grid.SetRow(sv, 1);
            root.Children.Add(sv);

            // voetbalk
            StackPanel foot = new StackPanel { Background = Ui.Br(Ui.Panel) };
            Grid fg = new Grid { Margin = new Thickness(32, 14, 32, 16) };
            fg.ColumnDefinitions.Add(new ColumnDefinition());
            fg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel left = new StackPanel();
            Grid bar = new Grid { Width = BarW, HorizontalAlignment = HorizontalAlignment.Left };
            bar.Children.Add(new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = Ui.Br(Ui.Line) });
            _fill.Height = 8; _fill.Width = 0; _fill.CornerRadius = new CornerRadius(4);
            _fill.Background = Ui.BrandGradient(); _fill.HorizontalAlignment = HorizontalAlignment.Left;
            bar.Children.Add(_fill);
            left.Children.Add(bar);
            _status.Margin = new Thickness(0, 8, 0, 0);
            _status.TextTrimming = TextTrimming.CharacterEllipsis;
            left.Children.Add(_status);
            fg.Children.Add(left);
            StackPanel act = new StackPanel { Orientation = Orientation.Horizontal };
            _preview.Margin = new Thickness(0, 0, 10, 0);
            act.Children.Add(_preview);
            act.Children.Add(_build);
            Grid.SetColumn(act, 1);
            fg.Children.Add(act);
            foot.Children.Add(fg);
            Grid.SetRow(foot, 2);
            root.Children.Add(foot);
            Content = root;

            _build.Click += OnBuild;
            _preview.Click += OnPreview;
            save.Click += OnSave;
            open.Click += OnOpen;
            LoadTranslatorSettings();
            if (translatorSnapshot != null) ApplyTranslator(translatorSnapshot);
            if (snapshot != null) ApplyProject(snapshot);
        }

        // Taalwissel: het venster wordt opnieuw opgebouwd in de nieuwe taal; alles wat is ingevuld blijft staan.
        private void SwitchLanguage(string code)
        {
            ProjectFile snap = ToProject();
            TranslatorConfig tc = CaptureTranslator();
            L.Set(code);
            StudioWindow w = new StudioWindow(snap, tc);
            w.WindowStartupLocation = WindowStartupLocation.Manual;
            w.Left = Left; w.Top = Top; w.Width = Width; w.Height = Height;
            Application.Current.MainWindow = w;
            w.Show();
            Close();
        }

        private TranslatorConfig CaptureTranslator()
        {
            TranslatorConfig c = new TranslatorConfig();
            c.Provider = _tcfg.Provider; c.Key = _tKey.Text; c.Url = _tUrl.Text; c.Model = _tModel.Text; c.Remember = _tRemember.IsChecked;
            return c;
        }

        private void ApplyTranslator(TranslatorConfig c)
        {
            _tcfg.Provider = Array.IndexOf(Translator.Providers, c.Provider) >= 0 ? c.Provider : "DeepL";
            _tKey.Text = c.Key ?? ""; _tUrl.Text = c.Url ?? ""; _tModel.Text = c.Model ?? "";
            _tRemember.IsChecked = c.Remember;
            _provBar.Selected = Math.Max(0, Array.IndexOf(Translator.Providers, _tcfg.Provider));
            UpdateProviderRows();
        }

        // ------------------------------------------------------------------ opbouw

        private static TextBlock SectionHead(string s)
        {
            TextBlock t = Ui.Text(s, 14, Ui.Cyan, true, true);
            t.Margin = new Thickness(0, 26, 0, 6);
            t.TextWrapping = TextWrapping.Wrap;
            return t;
        }

        private static void AddCell(Grid g, int col, string label, UIElement el)
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(Label(label));
            sp.Children.Add(el);
            Grid.SetColumn(sp, col);
            g.Children.Add(sp);
        }

        private static TextBlock Label(string s)
        {
            TextBlock t = Ui.Text(s, 12, Ui.Grey, false, false);
            t.Margin = new Thickness(0, 0, 0, 5);
            return t;
        }

        private static StackPanel Field(string label, UIElement el)
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(Label(label));
            sp.Children.Add(el);
            return sp;
        }

        private static StackPanel Pick(string label, TextBox tb, string btn, Action act)
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(Label(label));
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(tb);
            NdxButton b = new NdxButton(btn, false, true);
            b.Margin = new Thickness(10, 0, 0, 0);
            b.MinWidth = 100;
            b.Click += act;
            Grid.SetColumn(b, 1);
            g.Children.Add(b);
            sp.Children.Add(g);
            return sp;
        }

        private UIElement BuildBrandSection()
        {
            StackPanel s = new StackPanel();
            s.Children.Add(SectionHead(L.T("sec.brand")));
            TextBlock note = Ui.Text(L.T("brand.note"), 12, Ui.Grey, false, false);
            note.TextWrapping = TextWrapping.Wrap;
            s.Children.Add(note);
            s.Children.Add(Pick(L.T("f.logo"), _logo, L.T("btn.choose"), delegate { PickFile(_logo, L.T("flt.img")); }));
            s.Children.Add(Pick(L.T("f.side"), _side, L.T("btn.choose"), delegate { PickFile(_side, L.T("flt.img")); }));
            s.Children.Add(Pick(L.T("f.splash"), _splash, L.T("btn.choose"), delegate { PickFile(_splash, L.T("flt.img")); }));
            Grid cols = new Grid();
            cols.Margin = new Thickness(0, 14, 0, 0);
            for (int i = 0; i < 5; i++) cols.ColumnDefinitions.Add(new ColumnDefinition { Width = i % 2 == 1 ? new GridLength(16) : new GridLength(1, GridUnitType.Star) });
            AddCell(cols, 0, L.T("f.cdark"), _cDark);
            AddCell(cols, 2, L.T("f.caccent"), _cAccent);
            AddCell(cols, 4, L.T("f.cprimary"), _cPrimary);
            s.Children.Add(cols);
            s.Children.Add(Field(L.T("f.tagline"), _tagline));
            _sysFont.Margin = new Thickness(0, 12, 0, 0);
            s.Children.Add(_sysFont);
            return s;
        }

        private UIElement BuildLicenseSection()
        {
            StackPanel s = new StackPanel();
            s.Children.Add(SectionHead(L.T("sec.license")));
            TextBlock note = Ui.Text(L.T("lic.note"), 12, Ui.Grey, false, false);
            note.TextWrapping = TextWrapping.Wrap;
            s.Children.Add(note);

            _srcBar = new ChipBar(L.Names, _licSrc);
            _srcBar.Changed += delegate (int i) { _licSrc = i; _licMt[i] = false; UpdateLicInfo(); };
            s.Children.Add(Field(L.T("lic.srclang"), _srcBar));

            NdxButton load = new NdxButton(L.T("lic.load"), false, true);
            load.HorizontalAlignment = HorizontalAlignment.Left;
            load.Margin = new Thickness(0, 4, 0, 0);
            load.Click += delegate
            {
                OpenFileDialog d = new OpenFileDialog();
                d.Filter = L.T("flt.txt");
                if (d.ShowDialog(this) != true) return;
                try
                {
                    SetLicenseText(_licSrc, File.ReadAllText(d.FileName));
                    _tabBar.Selected = _licSrc;
                    ShowTab(_licSrc);
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, Edition.Name); }
            };
            s.Children.Add(load);

            // vertaaldienst
            _provBar = new ChipBar(new string[] { "DeepL", "LibreTranslate", "Claude (Anthropic)" }, 0);
            _provBar.Changed += delegate (int i) { _tcfg.Provider = Translator.Providers[i]; UpdateProviderRows(); };
            s.Children.Add(Field(L.T("lic.provider"), _provBar));
            s.Children.Add(Field(L.T("lic.key"), _tKey));
            _tUrlRow = Field(L.T("lic.url"), _tUrl);
            _tModelRow = Field(L.T("lic.model"), _tModel);
            s.Children.Add(_tUrlRow);
            s.Children.Add(_tModelRow);
            _tRemember.Margin = new Thickness(0, 8, 0, 0);
            s.Children.Add(_tRemember);
            TextBlock priv = Ui.Text(L.T("lic.privacy"), 12, Ui.Grey, false, false);
            priv.TextWrapping = TextWrapping.Wrap;
            priv.Margin = new Thickness(0, 6, 0, 0);
            s.Children.Add(priv);
            _translate.HorizontalAlignment = HorizontalAlignment.Left;
            _translate.Margin = new Thickness(0, 12, 0, 0);
            _translate.Click += OnTranslate;
            s.Children.Add(_translate);

            // teksten per taal
            _tabBar = new ChipBar(L.Names, _licCur);
            _tabBar.Changed += delegate (int i) { ShowTab(i); };
            s.Children.Add(Field(L.T("lic.tabs"), _tabBar));
            _licBox.AcceptsReturn = true;
            _licBox.TextWrapping = TextWrapping.Wrap;
            _licBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _licBox.Height = 240;
            _licBox.VerticalContentAlignment = VerticalAlignment.Top;
            _licBox.TextChanged += delegate
            {
                if (_licLoading) return;
                _licText[_licCur] = _licBox.Text;
                if (_licCur != _licSrc) _licMt[_licCur] = false;   // zelf aangepast: geen machinevertaling meer
                UpdateLicInfo();
            };
            s.Children.Add(_licBox);
            _licInfo.Margin = new Thickness(0, 6, 0, 0);
            _licInfo.TextWrapping = TextWrapping.Wrap;
            s.Children.Add(_licInfo);
            UpdateProviderRows();
            UpdateLicInfo();
            return s;
        }

        private void UpdateProviderRows()
        {
            _tUrlRow.Visibility = _tcfg.Provider == "LibreTranslate" ? Visibility.Visible : Visibility.Collapsed;
            _tModelRow.Visibility = _tcfg.Provider == "Claude" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetLicenseText(int idx, string text)
        {
            _licText[idx] = text ?? "";
            if (idx == _licCur)
            {
                _licLoading = true;
                _licBox.Text = _licText[idx];
                _licLoading = false;
            }
            UpdateLicInfo();
        }

        private void ShowTab(int i)
        {
            _licCur = i;
            _licLoading = true;
            _licBox.Text = _licText[i];
            _licLoading = false;
            UpdateLicInfo();
        }

        private void UpdateLicInfo()
        {
            List<string> have = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                if (_licText[i].Trim().Length > 0) have.Add(L.Names[i] + (i == _licSrc ? L.T("lic.orig") : (_licMt[i] ? L.T("lic.autotr") : "")));
            }
            _licInfo.Text = have.Count == 0 ? L.T("lic.none") : L.T("lic.in", string.Join(", ", have));
        }

        // ------------------------------------------------------------------ vertalen

        private string SettingsFile
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Edition.LogName, "translator.json"); }
        }

        private void LoadTranslatorSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    TranslatorConfig c = JsonSerializer.Deserialize<TranslatorConfig>(File.ReadAllText(SettingsFile));
                    if (c != null)
                    {
                        _tcfg.Provider = Array.IndexOf(Translator.Providers, c.Provider) >= 0 ? c.Provider : "DeepL";
                        _tUrl.Text = c.Url ?? "";
                        _tModel.Text = string.IsNullOrWhiteSpace(c.Model) ? "claude-sonnet-5-5" : c.Model;
                        _tKey.Text = c.Remember ? (c.Key ?? "") : "";
                        _tRemember.IsChecked = c.Remember;
                    }
                }
            }
            catch (Exception) { }
            if (_tModel.Text.Length == 0) _tModel.Text = "claude-sonnet-5-5";
            _provBar.Selected = Math.Max(0, Array.IndexOf(Translator.Providers, _tcfg.Provider));
            UpdateProviderRows();
        }

        private void SaveTranslatorSettings()
        {
            try
            {
                TranslatorConfig c = new TranslatorConfig();
                c.Provider = _tcfg.Provider; c.Url = _tUrl.Text.Trim(); c.Model = _tModel.Text.Trim();
                c.Remember = _tRemember.IsChecked; c.Key = c.Remember ? _tKey.Text.Trim() : "";
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception) { }
        }

        private async void OnTranslate()
        {
            string original = _licText[_licSrc];
            if (original.Trim().Length == 0)
            {
                MessageBox.Show(this, L.T("msg.needlic"), Edition.Name);
                return;
            }
            bool overwrite = false;
            for (int i = 0; i < 5; i++) if (i != _licSrc && _licText[i].Trim().Length > 0) overwrite = true;
            if (overwrite && MessageBox.Show(this, L.T("msg.overwrite"), Edition.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            _tcfg.Key = _tKey.Text.Trim(); _tcfg.Url = _tUrl.Text.Trim(); _tcfg.Model = _tModel.Text.Trim();
            SaveTranslatorSettings();
            _translate.IsEnabled = false;
            List<string> failed = new List<string>();
            string from = L.Codes[_licSrc];
            for (int i = 0; i < 5; i++)
            {
                if (i == _licSrc) continue;
                _status.Text = L.T("st.translating", L.Names[i]);
                try
                {
                    string tr = await Translator.TranslateAsync(_tcfg, original, from, L.Codes[i]);
                    SetLicenseText(i, tr);
                    _licMt[i] = true;
                }
                catch (Exception ex)
                {
                    failed.Add(L.Names[i] + ": " + ex.Message);
                }
            }
            _translate.IsEnabled = true;
            _status.Text = failed.Count == 0 ? L.T("st.translated") : L.T("st.translated.err", failed.Count);
            UpdateLicInfo();
            if (failed.Count > 0) MessageBox.Show(this, string.Join("\n\n", failed), Edition.Name);
        }

        // ------------------------------------------------------------------ keuzes

        private void PickFolder()
        {
            using (System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog())
            {
                d.Description = L.T("dlg.folder");
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                _src.Text = d.SelectedPath;
                if (string.IsNullOrWhiteSpace(_name.Text)) _name.Text = Path.GetFileName(d.SelectedPath.TrimEnd('\\'));
                AutoDetect();
            }
        }

        private void AutoDetect()
        {
            try
            {
                if (!Directory.Exists(_src.Text)) return;
                string[] exes = Directory.GetFiles(_src.Text, "*.exe", SearchOption.TopDirectoryOnly);
                string best = exes.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();
                if (best != null && string.IsNullOrWhiteSpace(_exe.Text)) _exe.Text = Path.GetFileName(best);
                string[] icos = Directory.GetFiles(_src.Text, "*.ico", SearchOption.AllDirectories);
                if (icos.Length > 0 && string.IsNullOrWhiteSpace(_ico.Text)) _ico.Text = icos[0];
            }
            catch (Exception) { }
        }

        private void PickExe()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = L.T("flt.exe");
            if (Directory.Exists(_src.Text)) d.InitialDirectory = _src.Text;
            if (d.ShowDialog(this) != true) return;
            string root = Path.GetFullPath(_src.Text).TrimEnd('\\') + "\\";
            string full = Path.GetFullPath(d.FileName);
            _exe.Text = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : Path.GetFileName(full);
        }

        private void PickFile(TextBox target, string filter)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = filter;
            if (d.ShowDialog(this) == true) target.Text = d.FileName;
        }

        // ------------------------------------------------------------------ project

        private BrandSpec ToBrand()
        {
            BrandSpec b = new BrandSpec();
            b.Dark = _cDark.Hex; b.Accent = _cAccent.Hex; b.Primary = _cPrimary.Hex;
            b.Tagline = _tagline.Text.Trim();
            b.Font = _sysFont.IsChecked ? "system" : "montserrat";
            b.HasLogo = _logo.Text.Trim().Length > 0;
            b.HasSide = _side.Text.Trim().Length > 0;
            b.HasSplash = _splash.Text.Trim().Length > 0;
            return b;
        }

        private ProjectFile ToProject()
        {
            ProjectFile p = new ProjectFile();
            p.Name = _name.Text.Trim(); p.Version = _ver.Text.Trim(); p.Publisher = _pub.Text.Trim();
            p.SourceDir = _src.Text.Trim(); p.MainExe = _exe.Text.Trim();
            p.IconFile = _ico.Text.Trim();
            p.PerMachine = _perMachine.IsChecked; p.StartMenu = _menu.IsChecked; p.Desktop = _desk.IsChecked; p.Launch = _launch.IsChecked; p.Taskbar = _taskbar.IsChecked; p.Autostart = _autostart.IsChecked;
            p.LicenseSource = L.Codes[_licSrc];
            p.LicenseTexts = new Dictionary<string, string>();
            p.LicenseMt = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                if (_licText[i].Trim().Length == 0) continue;
                p.LicenseTexts[L.Codes[i]] = _licText[i];
                if (_licMt[i]) p.LicenseMt.Add(L.Codes[i]);
            }
            p.Brand = ToBrand();
            p.LogoFile = _logo.Text.Trim(); p.SideFile = _side.Text.Trim(); p.SplashFile = _splash.Text.Trim();
            return p;
        }

        private void OnSave()
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = L.T("flt.proj", Edition.Name);
            d.FileName = InstallEngine.SafeName(_name.Text) + ".nexus";
            if (d.ShowDialog(this) != true) return;
            File.WriteAllText(d.FileName, JsonSerializer.Serialize(ToProject(), new JsonSerializerOptions { WriteIndented = true }));
            _status.Text = L.T("st.saved");
        }

        private void OnOpen()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = L.T("flt.proj", Edition.Name);
            if (d.ShowDialog(this) != true) return;
            try
            {
                ProjectFile p = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(d.FileName));
                ApplyProject(p);
                _status.Text = L.T("st.opened");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("msg.readfail", ex.Message), Edition.Name);
            }
        }

        private void ApplyProject(ProjectFile p)
        {
                _name.Text = p.Name ?? ""; _ver.Text = p.Version ?? ""; _pub.Text = p.Publisher ?? "";
                _src.Text = p.SourceDir ?? ""; _exe.Text = p.MainExe ?? ""; _ico.Text = p.IconFile ?? "";
                _perMachine.IsChecked = p.PerMachine; _menu.IsChecked = p.StartMenu; _desk.IsChecked = p.Desktop; _launch.IsChecked = p.Launch; _taskbar.IsChecked = p.Taskbar; _autostart.IsChecked = p.Autostart;

                _licSrc = Math.Max(0, Array.IndexOf(L.Codes, p.LicenseSource ?? "nl"));
                _srcBar.Selected = _licSrc;
                for (int i = 0; i < 5; i++)
                {
                    string tx;
                    _licText[i] = (p.LicenseTexts != null && p.LicenseTexts.TryGetValue(L.Codes[i], out tx)) ? tx : "";
                    _licMt[i] = p.LicenseMt != null && p.LicenseMt.Contains(L.Codes[i]);
                }
                _licCur = _licSrc;
                _tabBar.Selected = _licCur;
                ShowTab(_licCur);

                if (p.Brand != null)
                {
                    _cDark.Hex = p.Brand.Dark ?? "#070B19"; _cAccent.Hex = p.Brand.Accent ?? "#0088FF"; _cPrimary.Hex = p.Brand.Primary ?? "#F2A900";
                    _tagline.Text = p.Brand.Tagline ?? ""; _sysFont.IsChecked = string.Equals(p.Brand.Font, "system", StringComparison.OrdinalIgnoreCase);
                }
                _logo.Text = p.LogoFile ?? ""; _side.Text = p.SideFile ?? ""; _splash.Text = p.SplashFile ?? "";
        }

        private string Validate(ProjectFile p)
        {
            if (p.Name.Length == 0) return L.T("msg.name");
            if (p.Version.Length == 0) return L.T("msg.ver");
            if (!Directory.Exists(p.SourceDir)) return L.T("msg.src");
            if (p.MainExe.Length == 0 || !File.Exists(Path.Combine(p.SourceDir, p.MainExe))) return L.T("msg.exe");
            if (p.IconFile.Length > 0 && !File.Exists(p.IconFile)) return L.T("msg.icon");
            if (p.LicenseTexts.Count > 0 && !p.LicenseTexts.ContainsKey(p.LicenseSource)) return L.T("msg.licsrc", L.Names[_licSrc]);
            if (Edition.IsOpen)
            {
                foreach (string f in new string[] { p.LogoFile, p.SideFile, p.SplashFile })
                {
                    if (!string.IsNullOrEmpty(f) && !File.Exists(f)) return L.T("msg.img", f);
                }
                if (Ui.Contrast(Theme.ParseColor(p.Brand.Primary, Colors.Gray), Colors.White) < 1.2 && Ui.Contrast(Theme.ParseColor(p.Brand.Primary, Colors.Gray), Theme.ParseColor(p.Brand.Dark, Colors.Black)) < 1.2)
                    return L.T("msg.contrast");
            }
            return null;
        }

        private Manifest ToManifest(ProjectFile p)
        {
            Manifest m = new Manifest();
            m.Name = p.Name; m.Version = p.Version; m.Publisher = p.Publisher.Length == 0 ? (Edition.IsOpen ? p.Name : "NDX") : p.Publisher;
            m.Id = Builder.MakeId(m.Publisher, m.Name);
            m.MainExe = p.MainExe.Replace('\\', '/');
            m.PerMachine = p.PerMachine; m.StartMenu = p.StartMenu; m.DesktopShortcut = p.Desktop; m.LaunchAfter = p.Launch; m.TaskbarPin = p.Taskbar; m.Autostart = p.Autostart;
            m.LicenseSource = p.LicenseSource;
            m.LicenseMt = new List<string>(p.LicenseMt);
            return m;
        }

        private Dictionary<string, byte[]> BrandEntries(ProjectFile p)
        {
            Dictionary<string, byte[]> x = new Dictionary<string, byte[]>();
            if (!Edition.IsOpen) return x;
            x[Theme.BrandFile] = JsonSerializer.SerializeToUtf8Bytes(p.Brand);
            if (p.Brand.HasLogo) x[Theme.LogoFile] = Imaging.ToPng(p.LogoFile, 800);
            if (p.Brand.HasSide) x[Theme.SideFile] = Imaging.ToPng(p.SideFile, 800);
            if (p.Brand.HasSplash) x[Theme.SplashFile] = Imaging.ToPng(p.SplashFile, 1920);
            return x;
        }

        // ------------------------------------------------------------------ voorbeeld en bouwen

        private async void OnPreview()
        {
            ProjectFile p = ToProject();
            if (p.Name.Length == 0) p.Name = L.T("s.sample");
            if (p.Version.Length == 0) p.Version = "1.0.0";
            _preview.IsEnabled = false;
            try
            {
                Manifest m = ToManifest(p);
                m.MainExe = string.IsNullOrEmpty(p.MainExe) ? "app.exe" : m.MainExe;
                m.TotalBytes = 48L * 1024 * 1024;
                m.HasLicense = p.LicenseTexts.Count > 0;
                m.LicenseLangs = new List<string>(p.LicenseTexts.Keys);
                Dictionary<string, byte[]> entries = BrandEntries(p);
                foreach (KeyValuePair<string, string> kv in p.LicenseTexts) entries["license." + kv.Key + ".txt"] = new UTF8Encoding(false).GetBytes(kv.Value);
                if (p.IconFile.Length > 0 && File.Exists(p.IconFile)) entries["icon.ico"] = File.ReadAllBytes(p.IconFile);
                Payload pay = Payload.CreatePreview(m, entries);

                Theme.ApplyFromPayload(pay);
                await SplashWindow.ShowAsync(p.Name, L.T("version", p.Version), false, 1300);
                InstallerWindow w = new InstallerWindow(pay);
                w.Owner = this;
                w.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("msg.previewfail", ex.Message), Edition.Name);
            }
            finally
            {
                Theme.Reset();
                _preview.IsEnabled = true;
            }
        }

        private async void OnBuild()
        {
            ProjectFile p = ToProject();
            string err = Validate(p);
            if (err != null) { MessageBox.Show(this, err, Edition.Name); return; }
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = L.T("flt.setup");
            d.FileName = "Setup_" + InstallEngine.SafeName(p.Name).Replace(' ', '_') + "_" + p.Version + ".exe";
            if (d.ShowDialog(this) != true) return;

            BuildSpec spec = new BuildSpec();
            try
            {
                spec.Manifest = ToManifest(p);
                spec.SourceDir = p.SourceDir; spec.OutputFile = d.FileName;
                spec.IconFile = p.IconFile.Length > 0 ? p.IconFile : null;
                foreach (KeyValuePair<string, string> kv in p.LicenseTexts) spec.Licenses[kv.Key] = kv.Value;
                spec.Extra = BrandEntries(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("msg.brandfail", ex.Message), Edition.Name);
                return;
            }

            _build.IsEnabled = false;
            _lastOutput = d.FileName;
            try
            {
                await Task.Run(delegate
                {
                    Builder.Build(spec, delegate (double f, string t)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate { _fill.Width = BarW * f; _status.Text = t; }));
                    });
                });
                _fill.Width = BarW;
                _status.Text = L.T("st.done", Path.GetFileName(_lastOutput));
                if (MessageBox.Show(this, L.T("msg.built"), Edition.Name, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + _lastOutput + "\"");
                }
            }
            catch (Exception ex)
            {
                _status.Text = L.T("st.failed");
                MessageBox.Show(this, L.T("msg.buildfail", ex.Message), Edition.Name);
            }
            _build.IsEnabled = true;
        }
    }
}
