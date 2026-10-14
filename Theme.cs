using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NdxNexus
{
    // Vormgeving van een setup (Nexus wereldwijd). Wordt als brand.json + afbeeldingen in de setup gezet.
    internal sealed class BrandSpec
    {
        public string Dark { get; set; }       // zijpaneel, splash en koppen (donker)
        public string Accent { get; set; }     // selectie, taalknop, voortgang
        public string Primary { get; set; }    // hoofdknop
        public string Tagline { get; set; }    // optioneel, onder het welkom
        public string Font { get; set; }       // "montserrat" of "system"
        public bool HasLogo { get; set; }
        public bool HasSide { get; set; }
        public bool HasSplash { get; set; }

        public BrandSpec()
        {
            Dark = "#070B19"; Accent = "#0088FF"; Primary = "#F2A900"; Tagline = ""; Font = "montserrat";
        }
    }

    internal static class Theme
    {
        public const string BrandFile = "brand.json";
        public const string LogoFile = "brand-logo.png";
        public const string SideFile = "brand-side.png";
        public const string SplashFile = "brand-splash.png";

        private static readonly Color DNavy = Color.FromRgb(7, 11, 25);
        private static readonly Color DCyan = Color.FromRgb(0, 136, 255);
        private static readonly Color DOker = Color.FromRgb(242, 169, 0);
        private static readonly Color DGrey = Color.FromRgb(226, 232, 240);
        private static readonly Color DPanel = Color.FromRgb(16, 22, 43);
        private static readonly Color DLine = Color.FromRgb(36, 46, 74);
        private static readonly FontFamily DHead = Ui.Head;
        private static readonly FontFamily DBody = Ui.Body;

        public static BrandSpec Brand = new BrandSpec();
        public static byte[] Logo, Side, Splash;
        public static string ProductName = "";

        public static Color ParseColor(string s, Color fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s)) return fallback;
                return (Color)ColorConverter.ConvertFromString(s.Trim().StartsWith("#") ? s.Trim() : "#" + s.Trim());
            }
            catch (Exception) { return fallback; }
        }

        public static string Hex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        // Maakt een kleur donker genoeg voor witte tekst erop (en bruikbaar als koptekstkleur op lichtgrijs).
        public static Color EnsureDark(Color c)
        {
            int guard = 0;
            while (Ui.Contrast(c, Colors.White) < 7.0 && guard++ < 40)
            {
                c = Color.FromRgb((byte)(c.R * 0.9), (byte)(c.G * 0.9), (byte)(c.B * 0.9));
            }
            return c;
        }

        public static Color OnColor(Color bg)
        {
            return Ui.Contrast(bg, Colors.White) >= Ui.Contrast(bg, DNavy) ? Colors.White : DNavy;
        }

        // Zet de kleuren/lettertypen van de interface. Alleen in de Open-editie; de interne editie blijft altijd NDX.
        public static void Apply(BrandSpec b, byte[] logo, byte[] side, byte[] splash, string productName)
        {
            Reset();
            if (!Edition.IsOpen) return;
            Brand = b ?? new BrandSpec();
            Logo = logo; Side = side; Splash = splash; ProductName = productName ?? "";
            Color dark = EnsureDark(ParseColor(Brand.Dark, DNavy));
            Color accent = ParseColor(Brand.Accent, DCyan);
            Color primary = ParseColor(Brand.Primary, DOker);
            Ui.Navy = dark;
            Ui.Panel = Color.FromRgb((byte)Math.Min(255, dark.R + 12), (byte)Math.Min(255, dark.G + 12), (byte)Math.Min(255, dark.B + 20));
            Ui.Line = Color.FromRgb((byte)Math.Min(255, dark.R + 36), (byte)Math.Min(255, dark.G + 38), (byte)Math.Min(255, dark.B + 54));
            Ui.Cyan = accent;
            Ui.Oker = primary;
            Ui.OnOker = OnColor(primary);
            Ui.Custom = true;
            if (string.Equals(Brand.Font, "system", StringComparison.OrdinalIgnoreCase))
            {
                Ui.Head = new FontFamily("Segoe UI Semibold, Segoe UI, Arial");
                Ui.Body = new FontFamily("Segoe UI, Arial");
            }
        }

        public static void Reset()
        {
            Ui.Navy = DNavy; Ui.Cyan = DCyan; Ui.Oker = DOker; Ui.Grey = DGrey; Ui.Panel = DPanel; Ui.Line = DLine;
            Ui.OnOker = DNavy; Ui.Custom = false; Ui.Head = DHead; Ui.Body = DBody;
            Brand = new BrandSpec(); Logo = null; Side = null; Splash = null; ProductName = "";
        }

        public static void ApplyFromPayload(Payload p)
        {
            if (!Edition.IsOpen) return;
            BrandSpec b = null;
            byte[] j = p.ReadEntry(BrandFile);
            if (j != null) { try { b = JsonSerializer.Deserialize<BrandSpec>(j); } catch (Exception) { } }
            Apply(b, p.ReadEntry(LogoFile), p.ReadEntry(SideFile), p.ReadEntry(SplashFile), p.Manifest.Name);
        }

        public static void ApplyFromDir(string dir, string productName)
        {
            if (!Edition.IsOpen) return;
            string d = Path.Combine(dir, "nexus-brand");
            if (!Directory.Exists(d)) { Apply(null, null, null, null, productName); return; }
            BrandSpec b = null;
            try { b = JsonSerializer.Deserialize<BrandSpec>(File.ReadAllText(Path.Combine(d, BrandFile))); } catch (Exception) { }
            Apply(b, Read(Path.Combine(d, LogoFile)), Read(Path.Combine(d, SideFile)), Read(Path.Combine(d, SplashFile)), productName);
        }

        private static byte[] Read(string f)
        {
            try { return File.Exists(f) ? File.ReadAllBytes(f) : null; } catch (Exception) { return null; }
        }

        public static BitmapImage FromBytes(byte[] data)
        {
            if (data == null) return null;
            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = new MemoryStream(data);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception) { return null; }
        }

        // ---- onderdelen die de wizard, verwijderaar en splash gebruiken

        // Logo bovenaan het zijpaneel / de splash. Open-editie zonder logo: de productnaam als tekst.
        public static UIElement LogoElement(double width, string productName, double textSize)
        {
            if (!Edition.IsOpen)
            {
                Image img = new Image();
                img.Source = Ui.Image("ndx-logo-white.png");
                img.Width = width;
                img.HorizontalAlignment = HorizontalAlignment.Left;
                return img;
            }
            BitmapImage bi = FromBytes(Logo);
            if (bi != null)
            {
                Image img = new Image();
                img.Source = bi;
                img.Width = width;
                img.MaxHeight = width * 0.9;
                img.Stretch = Stretch.Uniform;
                img.HorizontalAlignment = HorizontalAlignment.Left;
                return img;
            }
            return null;   // zonder logo toont de wizard alleen de productnaam als tekst
        }

        public static Brush SidePanelBrush()
        {
            if (!Edition.IsOpen) return new ImageBrush(Ui.Image("side-route.png")) { Stretch = Stretch.UniformToFill };
            BitmapImage bi = FromBytes(Side);
            if (bi != null) return new ImageBrush(bi) { Stretch = Stretch.UniformToFill };
            return Ui.Br(Ui.Navy);
        }

        public static Brush SplashBrush()
        {
            if (!Edition.IsOpen) return new ImageBrush(Ui.Image("bg-route.png")) { Stretch = Stretch.UniformToFill };
            BitmapImage bi = FromBytes(Splash);
            if (bi != null) return new ImageBrush(bi) { Stretch = Stretch.UniformToFill };
            return Ui.Br(Ui.Navy);
        }

        // Tagline: intern altijd de NDX-slogan; Open alleen als de maker er een heeft ingevuld.
        public static string TaglineText()
        {
            if (!Edition.IsOpen) return L.T("tagline");
            return string.IsNullOrWhiteSpace(Brand.Tagline) ? null : Brand.Tagline.Trim();
        }
    }

    // Afbeeldingen van de maker omzetten naar een formaat dat de setup altijd kan lezen (PNG, begrensd).
    internal static class Imaging
    {
        public static byte[] ToPng(string path, int maxWidth)
        {
            BitmapImage src = new BitmapImage();
            src.BeginInit();
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            src.EndInit();
            BitmapSource use = src;
            if (src.PixelWidth > maxWidth)
            {
                double s = (double)maxWidth / src.PixelWidth;
                use = new TransformedBitmap(src, new ScaleTransform(s, s));
            }
            PngBitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(use));
            using (MemoryStream ms = new MemoryStream())
            {
                enc.Save(ms);
                return ms.ToArray();
            }
        }
    }
}
