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
        private readonly LicenseService _licenseService;
        private readonly string _fingerprint;
        private readonly Timer _timer;
        private License? _license;
        private LicenseState _state = LicenseState.Unknown;

        public LicensingService(LicenseService licenseService)
        {
            _licenseService = licenseService;
            _fingerprint = HardwareHelper.GetHardwareId();
            _timer = new Timer(_ => _ = PeriodicValidateAsync(), null, Timeout.Infinite, Timeout.Infinite);
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
                // Verifica a licença usando o novo serviço
                var result = await _licenseService.VerificarLicenca(existing.Email, _fingerprint);
                
                if (result)
                {
                    UpdateState(new License { LicenseKey = existing.LicenseKey }, true);
                    StartPeriodicValidation();
                    return new LicenseValidationResult { IsValid = true, License = new License { LicenseKey = existing.LicenseKey } };
                }
                else if (AllowOffline() && TryVerifyOffline(existing.LicenseKey))
                {
                    UpdateState(new License { LicenseKey = existing.LicenseKey }, true);
                    StartPeriodicValidation();
                    return new LicenseValidationResult { IsValid = true, License = new License { LicenseKey = existing.LicenseKey }, Message = "Offline valid" };
                }
            }

            // Se chegou aqui, não há licença válida local. Tenta usar chave fornecida (offline).
            try
            {
                var providedKey = await (licenseKeyProvider?.Invoke() ?? Task.FromResult<string?>(null));
                if (!string.IsNullOrWhiteSpace(providedKey) && AllowOffline() && TryVerifyOffline(providedKey.Trim()))
                {
                    var record = new InstallationRecord
                    {
                        LicenseKey = providedKey.Trim(),
                        Email = existing?.Email ?? string.Empty,
                        MachineFingerprint = _fingerprint,
                        InstalledAtIso8601 = DateTimeOffset.UtcNow.ToString("O"),
                        Notes = "Offline activation"
                    };
                    LicensingStorage.Save(record);
                    UpdateState(new License { LicenseKey = record.LicenseKey }, true);
                    StartPeriodicValidation();
                    return new LicenseValidationResult { IsValid = true, License = new License { LicenseKey = record.LicenseKey }, Message = "Offline valid" };
                }
            }
            catch { }

            // Fluxo online deve ser conduzido pela UI (ActivationMethodWindow)
            return new LicenseValidationResult { IsValid = false, Message = "Nenhuma licença válida encontrada." };
        }

        public async Task<LicenseValidationResult> ValidateExistingAsync()
        {
            var existing = LicensingStorage.Load();
            if (existing == null)
                return new LicenseValidationResult { IsValid = false, Message = "Sem licença local" };

            var result = await _licenseService.VerificarLicenca(existing.Email, _fingerprint);
            
            if (result)
            {
                UpdateState(new License { LicenseKey = existing.LicenseKey }, true);
                return new LicenseValidationResult { IsValid = true, License = new License { LicenseKey = existing.LicenseKey } };
            }
            
            if (AllowOffline() && TryVerifyOffline(existing.LicenseKey))
            {
                UpdateState(new License { LicenseKey = existing.LicenseKey }, true);
                return new LicenseValidationResult { IsValid = true, License = new License { LicenseKey = existing.LicenseKey }, Message = "Offline valid" };
            }
            
            UpdateState(null, false);
            return new LicenseValidationResult { IsValid = false, Message = "Licença expirada ou inválida" };
        }

        public async Task<bool> TransferAsync(string toFingerprint)
        {
            // Funcionalidade de transferência via API ainda não implementada no novo backend
            // Retorna false por enquanto
            await Task.Yield();
            return false;
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
