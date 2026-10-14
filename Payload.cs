using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NdxNexus
{
    // Gegevens over het te installeren programma (staat als manifest.json in de installer).
    internal sealed class Manifest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string MainExe { get; set; }          // relatief pad binnen de programmamap
        public bool PerMachine { get; set; }
        public bool StartMenu { get; set; }
        public bool DesktopShortcut { get; set; }
        public bool LaunchAfter { get; set; }
        public bool TaskbarPin { get; set; }          // standaardkeuze op de optiepagina
        public bool Autostart { get; set; }           // standaardkeuze op de optiepagina
        public bool HasLicense { get; set; }
        public string LicenseSource { get; set; }                       // taalcode van de originele licentietekst
        public List<string> LicenseLangs { get; set; }                  // talen waarvoor license.<taal>.txt bestaat
        public List<string> LicenseMt { get; set; }                     // talen die machinaal vertaald zijn
        public bool HasIcon { get; set; }
        public long TotalBytes { get; set; }
    }

    // Log van wat er is geïnstalleerd; de uninstaller gebruikt dit om netjes op te ruimen.
    internal sealed class InstallLog
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string Dir { get; set; }
        public bool PerMachine { get; set; }
        public bool Autostart { get; set; }
        public string Lang { get; set; }
        public List<string> Files { get; set; }
        public List<string> Dirs { get; set; }
        public List<string> Shortcuts { get; set; }
    }

    // Alleen-lezen venster op een deel van een bestand (voor het zip-gedeelte achter de exe).
    internal sealed class OffsetStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _start;
        private readonly long _length;
        private long _pos;

        public OffsetStream(Stream inner, long start, long length)
        {
            _inner = inner;
            _start = start;
            _length = length;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _length; } }
        public override long Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long left = _length - _pos;
            if (left <= 0) return 0;
            if (count > left) count = (int)left;
            _inner.Seek(_start + _pos, SeekOrigin.Begin);
            int n = _inner.Read(buffer, offset, count);
            _pos += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin) _pos = offset;
            else if (origin == SeekOrigin.Current) _pos += offset;
            else _pos = _length + offset;
            return _pos;
        }

        public override void Flush() { }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }

    // Een installer is: [exe][zip][8 bytes lengte van de zip][8 bytes "NDXNEXUS"].
    internal sealed class Payload
    {
        public const string Magic = "NDXNEXUS";
        public string ExePath;
        public long ZipStart;
        public long ZipLength;
        public Manifest Manifest;
        public bool IsPreview;                                          // voorbeeld in de Studio: niets wordt geinstalleerd
        private Dictionary<string, byte[]> _mem;

        public static Payload CreatePreview(Manifest m, Dictionary<string, byte[]> entries)
        {
            Payload p = new Payload();
            p.Manifest = m;
            p.IsPreview = true;
            p._mem = entries;
            p.ExePath = "";
            return p;
        }

        public long StubLength { get { return ZipStart; } }

        public static Payload TryOpen(string exePath)
        {
            try
            {
                using (FileStream fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length < 16 + 22) return null;
                    fs.Seek(-16, SeekOrigin.End);
                    byte[] tail = new byte[16];
                    fs.ReadExactly(tail, 0, 16);
                    if (Encoding.ASCII.GetString(tail, 8, 8) != Magic) return null;
                    long len = BitConverter.ToInt64(tail, 0);
                    long start = fs.Length - 16 - len;
                    if (len <= 0 || start <= 0) return null;

                    Payload p = new Payload();
                    p.ExePath = exePath;
                    p.ZipStart = start;
                    p.ZipLength = len;
                    using (ZipArchive za = p.OpenZip())
                    {
                        ZipArchiveEntry e = za.GetEntry("manifest.json");
                        if (e == null) return null;
                        using (Stream s = e.Open())
                        {
                            p.Manifest = System.Text.Json.JsonSerializer.Deserialize<Manifest>(s);
                        }
                    }
                    return p.Manifest == null ? null : p;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ZipArchive OpenZip()
        {
            FileStream fs = new FileStream(ExePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new ZipArchive(new OffsetStream(fs, ZipStart, ZipLength), ZipArchiveMode.Read);
        }

        public byte[] ReadEntry(string name)
        {
            if (_mem != null)
            {
                byte[] hit;
                return _mem.TryGetValue(name, out hit) ? hit : null;
            }
            using (ZipArchive za = OpenZip())
            {
                ZipArchiveEntry e = za.GetEntry(name);
                if (e == null) return null;
                using (Stream s = e.Open())
                using (MemoryStream ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
