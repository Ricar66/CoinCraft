using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("license_key")]
        public string? LicenseKey { get; set; }
    }

    public class ClaimLicenseRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("appId")]
        public int AppId { get; set; }

        [JsonPropertyName("hardwareId")]
        public string HardwareId { get; set; } = string.Empty;
    }

    public class LicenseService
    {
        private const string BaseUrl = "https://codecraftgenz.com.br";
        private const int AppId = 1;

        public async Task<LicenseVerifyResponse> VerifyLicenseAsync(string email, string hardwareId)
        {
            string url = $"{BaseUrl}/api/verify-license";
            try
            {
                using (var client = new HttpClient())
                {
                    // Headers de Segurança e Tracking
                    client.DefaultRequestHeaders.Add("x-device-id", hardwareId);
                    // Idealmente um ID de sessão/rastreio único por execução, mas aqui usaremos um novo GUID
                    client.DefaultRequestHeaders.Add("x-tracking-id", Guid.NewGuid().ToString());

                    var payload = new LicenseVerifyRequest
                    {
                        Email = email,
                        AppId = AppId,
                        HardwareId = hardwareId
                    };
                    
                    string jsonString = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                    
                    // Log da tentativa (usando Console/Debug se LogService não for injetado aqui, ou assumindo integração futura)
                    // Para simplificar e não quebrar dependências, logamos no Console que pode ser capturado
                    Console.WriteLine($"[LicenseService] Verifying: {email}, HW: {hardwareId}, AppID: {AppId}");

                    var response = await client.PostAsync(url, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        var result = JsonSerializer.Deserialize<LicenseVerifyResponse>(responseBody);
                        return result ?? new LicenseVerifyResponse { Success = false, Message = "Resposta vazia" };
                    }
                    catch
                    {
                        return new LicenseVerifyResponse { Success = false, Message = "Erro ao processar resposta do servidor" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new LicenseVerifyResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<string?> ClaimByEmailAsync(string email, string hardwareId)
        {
            string url = $"{BaseUrl}/api/licenses/claim-by-email";
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-device-id", hardwareId);
                    client.DefaultRequestHeaders.Add("x-tracking-id", Guid.NewGuid().ToString());

                    var payload = new ClaimLicenseRequest
                    {
                        Email = email,
                        AppId = AppId,
                        HardwareId = hardwareId
                    };

                    string jsonString = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    Console.WriteLine($"[LicenseService] Claiming: {email}, HW: {hardwareId}");

                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            if (doc.RootElement.TryGetProperty("license_key", out JsonElement keyEl))
                            {
                                return keyEl.GetString();
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        // Método legado mantido para compatibilidade
        public async Task<bool> VerificarLicenca(string email, string idPc)
        {
            var result = await VerifyLicenseAsync(email, idPc);
            
            if (result.Success) return true;
            
            if (result.Code == "LICENSE_LIMIT")
            {
                var key = await ClaimByEmailAsync(email, idPc);
                if (!string.IsNullOrEmpty(key))
                {
                    var retry = await VerifyLicenseAsync(email, idPc);
                    return retry.Success;
                }
            }

            return false;
        }
    }
}