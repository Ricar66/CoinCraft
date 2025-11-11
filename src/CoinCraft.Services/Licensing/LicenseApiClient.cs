using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoinCraft.Services.Licensing
{
    public interface ILicenseApiClient
    {
        Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey, string machineFingerprint);
        Task<License?> PurchaseLicenseAsync(string purchaserUserId);
        Task<bool> RegisterInstallationAsync(string licenseKey, string machineFingerprint);
        Task<bool> TransferLicenseAsync(string licenseKey, string fromFingerprint, string toFingerprint);
        Task<bool> DeactivateInstallationAsync(string licenseKey, string machineFingerprint);
    }

    public sealed class LicenseApiClient : ILicenseApiClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public LicenseApiClient(HttpClient httpClient, string baseUrl)
        {
            _http = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey, string machineFingerprint)
        {
            try
            {
                var url = $"{_baseUrl}/api/licenses/validate";
                var payload = new { licenseKey, machineFingerprint };
                // Placeholder: adjust to your server contract
                var response = await _http.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode)
                    return new LicenseValidationResult { IsValid = false, Message = $"HTTP {(int)response.StatusCode}" };
                var result = await response.Content.ReadFromJsonAsync<LicenseValidationResult>();
                return result ?? new LicenseValidationResult { IsValid = false, Message = "Resposta inválida" };
            }
            catch (Exception ex)
            {
                return new LicenseValidationResult { IsValid = false, Message = ex.Message };
            }
        }

        public async Task<License?> PurchaseLicenseAsync(string purchaserUserId)
        {
            try
            {
                var url = $"{_baseUrl}/api/licenses/purchase";
                var payload = new { purchaserUserId };
                var response = await _http.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<License>();
            }
            catch { return null; }
        }

        public async Task<bool> RegisterInstallationAsync(string licenseKey, string machineFingerprint)
        {
            try
            {
                var url = $"{_baseUrl}/api/licenses/register";
                var payload = new { licenseKey, machineFingerprint };
                var response = await _http.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> TransferLicenseAsync(string licenseKey, string fromFingerprint, string toFingerprint)
        {
            try
            {
                var url = $"{_baseUrl}/api/licenses/transfer";
                var payload = new { licenseKey, fromFingerprint, toFingerprint };
                var response = await _http.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeactivateInstallationAsync(string licenseKey, string machineFingerprint)
        {
            try
            {
                var url = $"{_baseUrl}/api/licenses/deactivate";
                var payload = new { licenseKey, machineFingerprint };
                var response = await _http.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}