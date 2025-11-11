using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

    internal sealed class LicensingStorage
    {
        private static string StoragePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft", "license.dat");

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
            _fingerprint = MachineIdProvider.ComputeFingerprint();
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
            }

            // Ask user/app for license key
            var key = await licenseKeyProvider();
            if (string.IsNullOrWhiteSpace(key))
                return new LicenseValidationResult { IsValid = false, Message = "Nenhuma licença fornecida" };

            var result = await _apiClient.ValidateLicenseAsync(key!, _fingerprint);
            if (!result.IsValid) { UpdateState(result.License, false); return result; }

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

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}