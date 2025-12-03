using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoinCraft.Services.Licensing
{
    public class LicenseVerifyRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("app_id")]
        public int AppId { get; set; }

        [JsonPropertyName("hardware_id")]
        public string HardwareId { get; set; } = string.Empty;
    }

    public class LicenseVerifyResponse
    {
        [JsonPropertyName("licensed")]
        public bool Licensed { get; set; }

        [JsonPropertyName("license_key")]
        public string? LicenseKey { get; set; }
    }

    public class LicenseService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://coincraft.pro/api/licenses/verify";

        public LicenseService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<LicenseVerifyResponse?> VerifyAsync(string email, string hardwareId)
        {
            try
            {
                var request = new LicenseVerifyRequest
                {
                    Email = email,
                    AppId = 1,
                    HardwareId = hardwareId
                };

                var response = await _httpClient.PostAsJsonAsync(ApiUrl, request);
                if (!response.IsSuccessStatusCode)
                {
                    return new LicenseVerifyResponse { Licensed = false };
                }

                var result = await response.Content.ReadFromJsonAsync<LicenseVerifyResponse>();
                return result;
            }
            catch
            {
                return new LicenseVerifyResponse { Licensed = false };
            }
        }
    }
}
