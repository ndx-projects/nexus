using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NdxNexus
{
    internal sealed class TranslatorConfig
    {
        public string Provider { get; set; }   // "DeepL", "LibreTranslate" of "Claude"
        public string Key { get; set; }
        public string Url { get; set; }        // alleen LibreTranslate, bv. https://libretranslate.example.com
        public string Model { get; set; }      // alleen Claude
        public bool Remember { get; set; }

        public TranslatorConfig()
        {
            Provider = "DeepL"; Key = ""; Url = ""; Model = "claude-sonnet-5-5";
        }
    }

    // Vertaalt de licentietekst via een externe dienst. De tekst verlaat daarbij de computer; dat staat ook in de Studio vermeld.
    internal static class Translator
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            HttpClient c = new HttpClient();
            c.Timeout = TimeSpan.FromMinutes(4);
            return c;
        }

        public static readonly string[] Providers = { "DeepL", "LibreTranslate", "Claude" };

        public static async Task<string> TranslateAsync(TranslatorConfig cfg, string text, string fromCode, string toCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            if (cfg.Provider == "Claude") return await ViaClaude(cfg, lines, fromCode, toCode);

            // DeepL en LibreTranslate: alleen niet-lege regels vertalen en de lege regels terugzetten.
            List<int> idx = new List<int>();
            List<string> parts = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Length > 0) { idx.Add(i); parts.Add(lines[i]); }
            }
            string[] result = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++) result[i] = lines[i];

            int pos = 0;
            while (pos < parts.Count)
            {
                List<string> chunk = new List<string>();
                int size = 0;
                while (pos + chunk.Count < parts.Count && chunk.Count < 40 && size < 60000)
                {
                    string p = parts[pos + chunk.Count];
                    chunk.Add(p);
                    size += p.Length;
                }
                List<string> tr = cfg.Provider == "DeepL"
                    ? await ViaDeepL(cfg, chunk, fromCode, toCode)
                    : await ViaLibre(cfg, chunk, fromCode, toCode);
                if (tr.Count != chunk.Count) throw new InvalidOperationException(L.T("tr.count"));
                for (int k = 0; k < chunk.Count; k++) result[idx[pos + k]] = tr[k];
                pos += chunk.Count;
            }
            return string.Join("\r\n", result);
        }

        private static string Snippet(string s)
        {
            if (s == null) return "";
            return s.Length > 300 ? s.Substring(0, 300) : s;
        }

        private static async Task<string> Send(HttpRequestMessage req)
        {
            HttpResponseMessage resp = await Http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new InvalidOperationException(L.T("tr.http", (int)resp.StatusCode, Snippet(body)));
            return body;
        }

        private static string DeepLTarget(string code)
        {
            switch (code)
            {
                case "en": return "EN-GB";
                default: return code.ToUpperInvariant();
            }
        }

        private static async Task<List<string>> ViaDeepL(TranslatorConfig cfg, List<string> lines, string from, string to)
        {
            string key = (cfg.Key ?? "").Trim();
            if (key.Length == 0) throw new InvalidOperationException(L.T("tr.nokey.deepl"));
            string url = key.EndsWith(":fx", StringComparison.Ordinal) ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate";
            var body = new Dictionary<string, object>();
            body["text"] = lines;
            body["source_lang"] = from.ToUpperInvariant();
            body["target_lang"] = DeepLTarget(to);
            body["preserve_formatting"] = true;
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", key);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            string json = await Send(req);
            List<string> outp = new List<string>();
            using (JsonDocument d = JsonDocument.Parse(json))
            {
                foreach (JsonElement t in d.RootElement.GetProperty("translations").EnumerateArray())
                {
                    outp.Add(t.GetProperty("text").GetString());
                }
            }
            return outp;
        }

        private static async Task<List<string>> ViaLibre(TranslatorConfig cfg, List<string> lines, string from, string to)
        {
            string baseUrl = (cfg.Url ?? "").Trim().TrimEnd('/');
            if (baseUrl.Length == 0) throw new InvalidOperationException(L.T("tr.nourl"));
            var body = new Dictionary<string, object>();
            body["q"] = lines;
            body["source"] = from;
            body["target"] = to;
            body["format"] = "text";
            if (!string.IsNullOrWhiteSpace(cfg.Key)) body["api_key"] = cfg.Key.Trim();
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/translate");
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            string json = await Send(req);
            List<string> outp = new List<string>();
            using (JsonDocument d = JsonDocument.Parse(json))
            {
                JsonElement t = d.RootElement.GetProperty("translatedText");
                if (t.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement e in t.EnumerateArray()) outp.Add(e.GetString());
                }
                else
                {
                    outp.Add(t.GetString());
                }
            }
            return outp;
        }

        private static string LangName(string code)
        {
            int i = Array.IndexOf(L.Codes, code);
            string[] en = { "Dutch", "English", "German", "French", "Croatian" };
            return i >= 0 ? en[i] : code;
        }

        private static async Task<string> ViaClaude(TranslatorConfig cfg, string[] lines, string from, string to)
        {
            string key = (cfg.Key ?? "").Trim();
            if (key.Length == 0) throw new InvalidOperationException(L.T("tr.nokey.claude"));
            string model = string.IsNullOrWhiteSpace(cfg.Model) ? "claude-sonnet-5-5" : cfg.Model.Trim();

            // in brokken van circa 6000 tekens, op regelgrenzen
            List<string> chunks = new List<string>();
            StringBuilder sb = new StringBuilder();
            foreach (string line in lines)
            {
                if (sb.Length > 0 && sb.Length + line.Length > 6000) { chunks.Add(sb.ToString()); sb.Clear(); }
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
            if (sb.Length > 0) chunks.Add(sb.ToString());

            string system = "You are a professional legal translator. Translate the user's text faithfully and completely. "
                + "Keep the structure exactly: same line breaks, numbering, headings and punctuation style. Do not add, summarize or comment. "
                + "Do not translate product names or company names. Output only the translation.";
            List<string> outp = new List<string>();
            foreach (string chunk in chunks)
            {
                var body = new Dictionary<string, object>();
                body["model"] = model;
                body["max_tokens"] = 8000;
                body["system"] = system;
                body["messages"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        { "role", "user" },
                        { "content", "Translate from " + LangName(from) + " to " + LangName(to) + ":\n\n" + chunk }
                    }
                };
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                req.Headers.Add("x-api-key", key);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                string json = await Send(req);
                using (JsonDocument d = JsonDocument.Parse(json))
                {
                    StringBuilder t = new StringBuilder();
                    foreach (JsonElement c in d.RootElement.GetProperty("content").EnumerateArray())
                    {
                        JsonElement tx;
                        if (c.TryGetProperty("text", out tx)) t.Append(tx.GetString());
                    }
                    outp.Add(t.ToString().Trim('\n', '\r'));
                }
            }
            return string.Join("\r\n", outp).Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }
}
