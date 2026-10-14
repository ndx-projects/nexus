using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace NdxNexus
{
    internal sealed class BuildSpec
    {
        public Manifest Manifest;
        public string SourceDir;
        public Dictionary<string, string> Licenses = new Dictionary<string, string>();   // taalcode -> licentietekst
        public Dictionary<string, byte[]> Extra = new Dictionary<string, byte[]>();     // extra bestanden (brand.*)
        public string IconFile;      // optioneel
        public string OutputFile;
    }

    // Maakt een setup.exe: kopieert deze exe (de installer-motor) en plakt er de bestanden achter.
    internal static class Builder
    {
        public static void Build(BuildSpec spec, Action<double, string> progress)
        {
            string self = Environment.ProcessPath;
            if (Payload.TryOpen(self) != null) throw new InvalidOperationException(L.T("b.already", Edition.Name));

            progress(0.02, L.T("b.collect"));
            List<string> files = new List<string>(Directory.EnumerateFiles(spec.SourceDir, "*", SearchOption.AllDirectories));
            string outFull = Path.GetFullPath(spec.OutputFile);
            files.RemoveAll(f => string.Equals(Path.GetFullPath(f), outFull, StringComparison.OrdinalIgnoreCase));
            if (files.Count == 0) throw new InvalidOperationException(L.T("b.nofiles"));

            long total = 0;
            foreach (string f in files) total += new FileInfo(f).Length;

            Manifest m = spec.Manifest;
            m.TotalBytes = total;
            m.LicenseLangs = new List<string>(spec.Licenses.Keys);
            m.HasLicense = m.LicenseLangs.Count > 0;
            m.HasIcon = !string.IsNullOrEmpty(spec.IconFile) && File.Exists(spec.IconFile);

            string tmpZip = Path.Combine(Path.GetTempPath(), "ndxnexus_" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                using (FileStream zs = new FileStream(tmpZip, FileMode.Create, FileAccess.ReadWrite))
                using (ZipArchive za = new ZipArchive(zs, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry me = za.CreateEntry("manifest.json", CompressionLevel.Optimal);
                    using (Stream s = me.Open())
                    {
                        byte[] json = JsonSerializer.SerializeToUtf8Bytes(m);
                        s.Write(json, 0, json.Length);
                    }
                    foreach (KeyValuePair<string, string> lic in spec.Licenses)
                    {
                        ZipArchiveEntry le = za.CreateEntry("license." + lic.Key + ".txt", CompressionLevel.Optimal);
                        using (Stream ls = le.Open())
                        {
                            byte[] lb = new UTF8Encoding(false).GetBytes(lic.Value);
                            ls.Write(lb, 0, lb.Length);
                        }
                    }
                    foreach (KeyValuePair<string, byte[]> ex in spec.Extra)
                    {
                        ZipArchiveEntry xe = za.CreateEntry(ex.Key, CompressionLevel.Optimal);
                        using (Stream xs = xe.Open()) { xs.Write(ex.Value, 0, ex.Value.Length); }
                    }
                    if (m.HasIcon) za.CreateEntryFromFile(spec.IconFile, "icon.ico", CompressionLevel.Optimal);

                    string root = Path.GetFullPath(spec.SourceDir).TrimEnd('\\') + "\\";
                    long done = 0;
                    foreach (string f in files)
                    {
                        string rel = Path.GetFullPath(f).Substring(root.Length).Replace('\\', '/');
                        za.CreateEntryFromFile(f, "app/" + rel, CompressionLevel.Optimal);
                        done += new FileInfo(f).Length;
                        progress(0.05 + 0.8 * (total == 0 ? 1.0 : (double)done / total), rel);
                    }
                }

                progress(0.88, L.T("b.assemble"));
                string dir = Path.GetDirectoryName(outFull);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (FileStream output = new FileStream(outFull, FileMode.Create, FileAccess.Write))
                {
                    using (FileStream stub = new FileStream(self, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        stub.CopyTo(output);
                    }
                    long zipLen;
                    using (FileStream zin = new FileStream(tmpZip, FileMode.Open, FileAccess.Read))
                    {
                        zipLen = zin.Length;
                        zin.CopyTo(output);
                    }
                    output.Write(BitConverter.GetBytes(zipLen), 0, 8);
                    byte[] magic = Encoding.ASCII.GetBytes(Payload.Magic);
                    output.Write(magic, 0, magic.Length);
                }
                progress(1.0, L.T("inst.done"));
            }
            finally
            {
                try { File.Delete(tmpZip); } catch (Exception) { }
            }
        }

        public static string MakeId(string publisher, string name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in (publisher ?? "") + "_" + (name ?? ""))
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
