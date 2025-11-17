using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CoinCraft.Services
{
    public sealed class IntegrityService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _publicKeyXml;

        public IntegrityService(HttpClient http, string baseUrl, string? publicKeyXml = null)
        {
            _http = http;
            _baseUrl = baseUrl.TrimEnd('/');
            _publicKeyXml = publicKeyXml ?? string.Empty;
        }

        public async Task<bool> VerifyExecutableAsync(string version)
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) return false;
                var hash = ComputeSha256File(exe);
                var url = $"{_baseUrl}/api/protection/checksum";
                var payload = new { version, sha256 = hash };
                var res = await _http.PostAsJsonAsync(url, payload);
                if (!res.IsSuccessStatusCode) return false;
                var ok = await res.Content.ReadFromJsonAsync<bool>();
                return ok;
            }
            catch { return false; }
        }

        public static string ComputeSha256File(string path)
        {
            using var fs = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash);
        }

        public bool VerifyLocalManifest()
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) return false;
                var folder = Path.GetDirectoryName(exe)!;
                var manifestPath = Path.Combine(folder, "checksum.json");
                var sigPath = Path.Combine(folder, "checksum.sig");
                if (!File.Exists(manifestPath) || !File.Exists(sigPath)) return false;

                var manifest = File.ReadAllText(manifestPath, Encoding.UTF8);
                var sig = Convert.FromBase64String(File.ReadAllText(sigPath).Trim());

                if (string.IsNullOrWhiteSpace(_publicKeyXml)) return false;
                using var rsa = RSA.Create();
                rsa.FromXmlString(_publicKeyXml);
                using var sha = SHA256.Create();
                var data = Encoding.UTF8.GetBytes(manifest);
                var ok = rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                if (!ok) return false;

                var obj = System.Text.Json.JsonSerializer.Deserialize<ChecksumManifest>(manifest);
                if (obj is null || string.IsNullOrWhiteSpace(obj.Sha256)) return false;
                var actual = ComputeSha256File(exe);
                return string.Equals(obj.Sha256, actual, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private sealed class ChecksumManifest
        {
            public string Version { get; set; } = string.Empty;
            public string Sha256 { get; set; } = string.Empty;
        }
    }
}