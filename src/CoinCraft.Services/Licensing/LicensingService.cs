using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CoinCraft.Services.Licensing
{
    public interface ILicensingService
    {
        Task<LicenseValidationResult> EnsureLicensedAsync(Func<Task<string?>> licenseKeyProvider);
        Task<LicenseValidationResult> ValidateExistingAsync();
        Task<bool> TransferAsync(string toFingerprint);
        LicenseState CurrentState { get; }
        License? CurrentLicense { get; }
        string CurrentFingerprint { get; }
    }

    public sealed class LicensingStorage
    {
        public static string StoragePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft", "license.dat");

        public static void Save(InstallationRecord record)
        {
            var dir = Path.GetDirectoryName(StoragePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(record);
            var plain = Encoding.UTF8.GetBytes(json);
            var protectedBytes = CryptoHelper.Protect(plain);
            File.WriteAllBytes(StoragePath, protectedBytes);
        }

        public static InstallationRecord? Load()
        {
            try
            {
                if (!File.Exists(StoragePath)) return null;
                var enc = File.ReadAllBytes(StoragePath);
                var plain = CryptoHelper.Unprotect(enc);
                var json = Encoding.UTF8.GetString(plain);
                return JsonSerializer.Deserialize<InstallationRecord>(json);
            }
            catch { return null; }
        }
    }

    public sealed class LicensingService : ILicensingService, IDisposable
    {
        private readonly ILicenseApiClient _apiClient;
        private readonly Timer _timer;
        private LicenseState _state = LicenseState.Unknown;
        private License? _license;
        private readonly string _fingerprint;

        public LicensingService(ILicenseApiClient apiClient)
        {
            _apiClient = apiClient;
            _fingerprint = HardwareHelper.ComputeHardwareId();
            _timer = new Timer(async _ => await PeriodicValidateAsync(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public LicenseState CurrentState => _state;
        public License? CurrentLicense => _license;
        public string CurrentFingerprint => _fingerprint;

        public async Task<LicenseValidationResult> EnsureLicensedAsync(Func<Task<string?>> licenseKeyProvider)
        {
            // Try existing
            var existing = LicensingStorage.Load();
            if (existing != null)
            {
                var r = await _apiClient.ValidateLicenseAsync(existing.LicenseKey, _fingerprint);
                UpdateState(r.License, r.IsValid);
                if (r.IsValid)
                {
                    StartPeriodicValidation();
                    return r;
                }
                else if (AllowOffline() && TryVerifyOffline(existing.LicenseKey))
                {
                    UpdateState(r.License, true);
                    StartPeriodicValidation();
                    return new LicenseValidationResult { IsValid = true, License = r.License, Message = "Offline valid" };
                }
            }

            // Ask user/app for license key
            var key = await licenseKeyProvider();
            if (string.IsNullOrWhiteSpace(key))
                return new LicenseValidationResult { IsValid = false, Message = "Nenhuma licença fornecida" };

            var result = await _apiClient.ValidateLicenseAsync(key!, _fingerprint);
            if (!result.IsValid)
            {
                if (AllowOffline() && TryVerifyOffline(key!))
                {
                    LicensingStorage.Save(new InstallationRecord
                    {
                        LicenseKey = key!,
                        MachineFingerprint = _fingerprint,
                        InstalledAtIso8601 = DateTimeOffset.UtcNow.ToString("O")
                    });
                    UpdateState(result.License, true);
                    StartPeriodicValidation();
                    return new LicenseValidationResult { IsValid = true, License = result.License, Message = "Offline valid" };
                }
                UpdateState(result.License, false);
                return result;
            }

            // Register installation and persist locally
            var ok = await _apiClient.RegisterInstallationAsync(key!, _fingerprint);
            if (!ok)
                return new LicenseValidationResult { IsValid = false, Message = "Falha ao registrar instalação" };

            LicensingStorage.Save(new InstallationRecord
            {
                LicenseKey = key!,
                MachineFingerprint = _fingerprint,
                InstalledAtIso8601 = DateTimeOffset.UtcNow.ToString("O")
            });

            UpdateState(result.License, true);
            StartPeriodicValidation();
            return result;
        }

        public async Task<LicenseValidationResult> ValidateExistingAsync()
        {
            var existing = LicensingStorage.Load();
            if (existing == null)
                return new LicenseValidationResult { IsValid = false, Message = "Sem licença local" };

            var r = await _apiClient.ValidateLicenseAsync(existing.LicenseKey, _fingerprint);
            UpdateState(r.License, r.IsValid);
            if (!r.IsValid && AllowOffline() && TryVerifyOffline(existing.LicenseKey))
            {
                UpdateState(r.License, true);
                return new LicenseValidationResult { IsValid = true, License = r.License, Message = "Offline valid" };
            }
            return r;
        }

        public async Task<bool> TransferAsync(string toFingerprint)
        {
            var existing = LicensingStorage.Load();
            if (existing == null || string.IsNullOrWhiteSpace(existing.LicenseKey)) return false;
            var ok = await _apiClient.TransferLicenseAsync(existing.LicenseKey, _fingerprint, toFingerprint);
            return ok;
        }

        private void UpdateState(License? license, bool valid)
        {
            _license = license;
            _state = valid ? LicenseState.Active : LicenseState.Inactive;
        }

        private void StartPeriodicValidation()
        {
            // Validate every 24h
            _timer.Change(TimeSpan.FromHours(24), TimeSpan.FromHours(24));
        }

        private async Task PeriodicValidateAsync()
        {
            try
            {
                var res = await ValidateExistingAsync();
                if (!res.IsValid)
                {
                    _state = LicenseState.Inactive;
                }
            }
            catch { /* swallow to avoid crashing */ }
        }

        private static bool AllowOffline()
        {
            var allowEnv = string.Equals(Environment.GetEnvironmentVariable("COINCRAFT_ALLOW_OFFLINE"), "1", StringComparison.OrdinalIgnoreCase);
            var baseDir = AppContext.BaseDirectory;
            var xmlPathEnv = Environment.GetEnvironmentVariable("COINCRAFT_PUBLICKEY_XML_PATH");
            var pemPathEnv = Environment.GetEnvironmentVariable("COINCRAFT_PUBLICKEY_PEM_PATH");
            var xmlPath = string.IsNullOrWhiteSpace(xmlPathEnv) ? Path.Combine(baseDir, "public.xml") : xmlPathEnv!;
            var pemPath = string.IsNullOrWhiteSpace(pemPathEnv) ? Path.Combine(baseDir, "public.pem") : pemPathEnv!;
            var hasPublicKeyFile = File.Exists(xmlPath) || File.Exists(pemPath);
            return allowEnv || hasPublicKeyFile;
        }

        private bool TryVerifyOffline(string licenseKey)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var xmlPathEnv = Environment.GetEnvironmentVariable("COINCRAFT_PUBLICKEY_XML_PATH");
                var pemPathEnv = Environment.GetEnvironmentVariable("COINCRAFT_PUBLICKEY_PEM_PATH");
                var xmlPath = string.IsNullOrWhiteSpace(xmlPathEnv) ? Path.Combine(baseDir, "public.xml") : xmlPathEnv!;
                var pemPath = string.IsNullOrWhiteSpace(pemPathEnv) ? Path.Combine(baseDir, "public.pem") : pemPathEnv!;
                string? keyText = null;
                if (File.Exists(xmlPath)) keyText = File.ReadAllText(xmlPath);
                else if (File.Exists(pemPath)) keyText = File.ReadAllText(pemPath);
                if (string.IsNullOrWhiteSpace(keyText)) return false;
                var sig = Convert.FromBase64String(licenseKey);
                var data = Encoding.UTF8.GetBytes(_fingerprint);
                if (keyText.Contains("<RSAKeyValue>"))
                {
                    using var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(keyText);
                    return rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                else
                {
                    try
                    {
                        using var ecdsa = ECDsa.Create();
                        ecdsa.ImportFromPem(keyText);
                        return ecdsa.VerifyData(data, sig, HashAlgorithmName.SHA256);
                    }
                    catch
                    {
                        using var rsa = RSA.Create();
                        rsa.ImportFromPem(keyText);
                        return rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }
                }
            }
            catch { return false; }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
