using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NdxNexus
{
    // NDX-huisstijl: donkerblauw #070B19, cyaan #0088FF, oker #F2A900, neutraal #E2E8F0
    internal static class Ui
    {
        public static Color Navy = Color.FromRgb(7, 11, 25);
        public static Color Cyan = Color.FromRgb(0, 136, 255);
        public static Color Oker = Color.FromRgb(242, 169, 0);
        public static Color Grey = Color.FromRgb(226, 232, 240);
        public static Color Panel = Color.FromRgb(16, 22, 43);
        public static Color Line = Color.FromRgb(36, 46, 74);
        public static Color OnOker = Color.FromRgb(7, 11, 25);   // tekstkleur op de primaire knop
        public static bool Custom;                                // true = eigen vormgeving (Nexus wereldwijd)

        public static Color Lighten(Color c, double f)
        {
            return Color.FromRgb((byte)(c.R + (255 - c.R) * f), (byte)(c.G + (255 - c.G) * f), (byte)(c.B + (255 - c.B) * f));
        }

        public static double Luminance(Color c)
        {
            Func<byte, double> lin = delegate (byte v) { double s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); };
            return 0.2126 * lin(c.R) + 0.7152 * lin(c.G) + 0.0722 * lin(c.B);
        }

        public static double Contrast(Color a, Color b)
        {
            double l1 = Luminance(a), l2 = Luminance(b);
            if (l1 < l2) { double t = l1; l1 = l2; l2 = t; }
            return (l1 + 0.05) / (l2 + 0.05);
        }

        // Montserrat (koppen) en Open Sans (tekst) uit de exe zelf; Segoe UI/Arial als vangnet.
        private static readonly Uri FontBase = new Uri("pack://application:,,,/" + typeof(Ui).Assembly.GetName().Name + ";component/Fonts/");
        public static FontFamily Head = new FontFamily(FontBase, "./#Montserrat, Segoe UI Semibold, Segoe UI, Arial");
        public static FontFamily Body = new FontFamily(FontBase, "./#Open Sans, Segoe UI, Arial");

        public static SolidColorBrush Br(Color c)
        {
            SolidColorBrush b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static LinearGradientBrush BrandGradient()
        {
            LinearGradientBrush g = new LinearGradientBrush();
            g.StartPoint = new Point(0, 0);
            g.EndPoint = new Point(1, 0);
            if (Custom)
            {
                g.GradientStops.Add(new GradientStop(Cyan, 0.0));
                g.GradientStops.Add(new GradientStop(Oker, 1.0));
            }
            else
            {
                g.GradientStops.Add(new GradientStop(Navy, 0.0));
                g.GradientStops.Add(new GradientStop(Cyan, 0.55));
                g.GradientStops.Add(new GradientStop(Oker, 1.0));
            }
            return g;
        }

        public static BitmapImage Image(string name)
        {
            using (Stream s = typeof(Ui).Assembly.GetManifestResourceStream("Assets." + name))
            {
                if (s == null) return null;
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = s;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }

        public static ImageSource IconFromBytes(byte[] data)
        {
            if (data == null) return null;
            try
            {
                MemoryStream ms = new MemoryStream(data);
                return BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TextBlock Text(string s, double size, Color fg, bool head, bool bold)
        {
            TextBlock t = new TextBlock();
            t.Text = s;
            t.FontSize = size;
            t.Foreground = Br(fg);
            t.FontFamily = head ? Head : Body;
            if (bold) t.FontWeight = FontWeights.Bold;
            t.TextWrapping = TextWrapping.Wrap;
            return t;
        }

        public static TextBox Input(bool dark)
        {
            TextBox tb = new TextBox();
            tb.FontFamily = Body;
            tb.FontSize = 14;
            tb.Padding = new Thickness(10, 8, 10, 8);
            tb.BorderThickness = new Thickness(1);
            tb.VerticalContentAlignment = VerticalAlignment.Center;
            if (dark)
            {
                tb.Background = Br(Panel);
                tb.Foreground = Br(Grey);
                tb.BorderBrush = Br(Line);
                tb.CaretBrush = Br(Grey);
            }
            else
            {
                tb.Background = Br(Colors.White);
                tb.Foreground = Br(Navy);
                tb.BorderBrush = Br(Color.FromRgb(190, 200, 215));
                tb.CaretBrush = Br(Navy);
            }
            return tb;
        }

        public static Border GradientLine(double height)
        {
            Border b = new Border();
            b.Height = height;
            b.Background = BrandGradient();
            return b;
        }
    }

    // Knop in huisstijl: primair = oker met donkerblauwe tekst (contrast 9,8:1), secundair = omlijnd.
    internal sealed class NdxButton : Border
    {
        public event Action Click;
        private readonly TextBlock _t;
        private readonly bool _primary;
        private readonly bool _dark;

        public NdxButton(string text, bool primary, bool darkSurface)
        {
            _primary = primary;
            _dark = darkSurface;
            _t = new TextBlock();
            _t.Text = text;
            _t.FontFamily = Ui.Head;
            _t.FontWeight = FontWeights.SemiBold;
            _t.FontSize = 14;
            _t.HorizontalAlignment = HorizontalAlignment.Center;
            _t.VerticalAlignment = VerticalAlignment.Center;
            Child = _t;
            Padding = new Thickness(22, 10, 22, 10);
            CornerRadius = new CornerRadius(8);
            BorderThickness = new Thickness(1.5);
            Cursor = Cursors.Hand;
            MinWidth = 110;
            Apply(false);
            MouseEnter += delegate { Apply(true); };
            MouseLeave += delegate { Apply(false); };
            MouseLeftButtonUp += delegate { if (IsEnabled && Click != null) Click(); };
            IsEnabledChanged += delegate { Opacity = IsEnabled ? 1.0 : 0.4; };
        }

        public string Text
        {
            get { return _t.Text; }
            set { _t.Text = value; }
        }

        private void Apply(bool hover)
        {
            if (_primary)
            {
                Background = Ui.Br(hover ? Ui.Lighten(Ui.Oker, 0.14) : Ui.Oker);
                BorderBrush = Brushes.Transparent;
                _t.Foreground = Ui.Br(Ui.OnOker);
            }
            else
            {
                Color edge = _dark ? Ui.Line : Color.FromRgb(190, 200, 215);
                Color fg = _dark ? Ui.Grey : Ui.Navy;
                Background = hover ? Ui.Br(Color.FromArgb(30, Ui.Cyan.R, Ui.Cyan.G, Ui.Cyan.B)) : Brushes.Transparent;
                BorderBrush = Ui.Br(hover ? Ui.Cyan : edge);
                _t.Foreground = Ui.Br(fg);
            }
        }
    }

    // Aankruisvakje in huisstijl.
    internal sealed class NdxCheck : Border
    {
        public event Action Changed;
        private readonly Border _box = new Border();
        private readonly TextBlock _mark = new TextBlock();
        private bool _checked;

        public NdxCheck(string text, bool isChecked, bool darkSurface)
        {
            Color fg = darkSurface ? Ui.Grey : Ui.Navy;
            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;

            _box.Width = 22;
            _box.Height = 22;
            _box.CornerRadius = new CornerRadius(5);
            _box.BorderThickness = new Thickness(2);
            _mark.Text = "✓";
            _mark.FontSize = 15;
            _mark.FontWeight = FontWeights.Bold;
            _mark.Foreground = Ui.Br(Ui.Navy);
            _mark.HorizontalAlignment = HorizontalAlignment.Center;
            _mark.VerticalAlignment = VerticalAlignment.Center;
            _box.Child = _mark;
            row.Children.Add(_box);

            TextBlock label = Ui.Text(text, 14, fg, false, false);
            label.Margin = new Thickness(12, 0, 0, 0);
            label.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(label);

            Child = row;
            Background = Brushes.Transparent;
            Cursor = Cursors.Hand;
            Padding = new Thickness(0, 5, 0, 5);
            IsChecked = isChecked;
            MouseLeftButtonUp += delegate { IsChecked = !IsChecked; if (Changed != null) Changed(); };
        }

        public bool IsChecked
        {
            get { return _checked; }
            set
            {
                _checked = value;
                _box.Background = value ? Ui.Br(Ui.Cyan) : Brushes.Transparent;
                _box.BorderBrush = Ui.Br(value ? Ui.Cyan : Color.FromRgb(120, 135, 165));
                _mark.Visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
}
