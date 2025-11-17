using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace CoinCraft.Services
{
    public sealed class OfflineUpdateService
    {
        private readonly string _publicKeyXml;
        public OfflineUpdateService(string publicKeyXml) { _publicKeyXml = publicKeyXml; }

        public bool VerifyPackage(string zipPath, out string version)
        {
            version = string.Empty;
            try
            {
                if (!File.Exists(zipPath)) return false;
                using var zip = ZipFile.OpenRead(zipPath);
                var manifestEntry = zip.GetEntry("manifest.json");
                var sigEntry = zip.GetEntry("manifest.sig");
                if (manifestEntry is null || sigEntry is null) return false;
                using var ms = new MemoryStream();
                using var msSig = new MemoryStream();
                manifestEntry.Open().CopyTo(ms);
                sigEntry.Open().CopyTo(msSig);
                var manifest = Encoding.UTF8.GetString(ms.ToArray());
                var sig = Convert.FromBase64String(Encoding.UTF8.GetString(msSig.ToArray()).Trim());

                using var rsa = RSA.Create();
                rsa.FromXmlString(_publicKeyXml);
                var ok = rsa.VerifyData(Encoding.UTF8.GetBytes(manifest), sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                if (!ok) return false;

                var obj = System.Text.Json.JsonSerializer.Deserialize<PackageManifest>(manifest);
                if (obj is null) return false;
                version = obj.Version ?? string.Empty;
                if (string.IsNullOrWhiteSpace(obj.Version) || string.IsNullOrWhiteSpace(obj.Sha256)) return false;

                // Verificar SHA256 do executável principal no payload
                var exeEntry = zip.GetEntry("payload/CoinCraft.App.exe") ?? zip.GetEntry("CoinCraft.App.exe");
                if (exeEntry is null) return false;
                using var exeStream = exeEntry.Open();
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(exeStream);
                var actualSha = Convert.ToHexString(hash);
                return string.Equals(actualSha, obj.Sha256, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private sealed class PackageManifest
        {
            public string Version { get; set; } = string.Empty;
            public string Sha256 { get; set; } = string.Empty;
        }
    }
}