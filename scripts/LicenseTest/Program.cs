using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LicenseTest
{
    // Models
    public class LicenseVerifyRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("app_id")]
        public int AppId { get; set; }
        [JsonPropertyName("hardware_id")]
        public string HardwareId { get; set; }
    }

    public class LicenseVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("license_key")]
        public string LicenseKey { get; set; }
    }

    public class ClaimLicenseRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("appId")]
        public int AppId { get; set; }
        [JsonPropertyName("hardwareId")]
        public string HardwareId { get; set; }
    }

    class Program
    {
        private const string BaseUrl = "https://codecraftgenz.com.br";
        private const int AppId = 103; // Ajustado para 103 conforme orientação
        private const string TestEmail = "testuser+1765039970031@testdomain.com";
        
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== RE-TESTE DE LICENCIAMENTO ===\n");
            Console.WriteLine($"Email Alvo: {TestEmail}");
            
            using var client = new HttpClient();

            // 1. Tenta ativar com um novo Hardware ID
            string hwId = GenerateHardwareId();
            Console.WriteLine($"\n--- Tentativa 1: Novo Hardware ID ({hwId.Substring(0, 8)}...) ---");
            var result = await TestVerify(client, TestEmail, hwId, "Verify Inicial");

            // Se falhar por limite, tenta o Claim (que força uma nova ativação se permitido)
            if (!result.Success && result.Code == "LICENSE_LIMIT")
            {
                Console.WriteLine("\n>> Limite atingido detectado. Tentando endpoint de Claim (Recuperação/Nova Ativação)...");
                await TestClaim(client, TestEmail, hwId);
            }
            else if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n>> SUCESSO! A licença foi ativada corretamente.");
                Console.ResetColor();
            }

            Console.WriteLine("\n=== TESTE FINALIZADO ===");
        }

        static string GenerateHardwareId()
        {
            // Simula um hash SHA256 de hardware
            var bytes = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        static async Task<LicenseVerifyResponse> TestVerify(HttpClient client, string email, string hwId, string scenario)
        {
            string url = $"{BaseUrl}/api/verify-license";
            var payload = new LicenseVerifyRequest { Email = email, AppId = AppId, HardwareId = hwId };
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[DEBUG] POST {url}");
            Console.WriteLine($"[DEBUG] Payload: {json}");

            try 
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[DEBUG] Response Status: {response.StatusCode}");
                Console.WriteLine($"[DEBUG] Response Body: {body}");

                var result = JsonSerializer.Deserialize<LicenseVerifyResponse>(body);

                Console.ForegroundColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"[{scenario}] HTTP {response.StatusCode}");
                Console.WriteLine($"[{scenario}] Response Code: {result.Code}");
                Console.WriteLine($"[{scenario}] Message: {result.Message}");
                if (result.Success) Console.WriteLine($"[{scenario}] License Key: {result.LicenseKey?.Substring(0, 15)}...");
                Console.ResetColor();
                
                return result;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{scenario}] ERRO: {ex.Message}");
                Console.ResetColor();
                return new LicenseVerifyResponse { Success = false, Message = ex.Message };
            }
        }

        static async Task TestClaim(HttpClient client, string email, string hwId)
        {
            string url = $"{BaseUrl}/api/licenses/claim-by-email";
            var payload = new ClaimLicenseRequest { Email = email, AppId = AppId, HardwareId = hwId };
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[DEBUG-CLAIM] POST {url}");
            Console.WriteLine($"[DEBUG-CLAIM] Payload: {json}");

            try 
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[DEBUG-CLAIM] Response Status: {response.StatusCode}");
                Console.WriteLine($"[DEBUG-CLAIM] Response Body: {body}");
                
                if (response.IsSuccessStatusCode)
                {
                     Console.ForegroundColor = ConsoleColor.Green;
                     Console.WriteLine("[Claim] SUCESSO! O servidor gerou/retornou uma chave.");
                     Console.ResetColor();
                     
                     // Validação final
                     Console.WriteLine(">> Validando a nova chave com Verify...");
                     await TestVerify(client, email, hwId, "Verify Pós-Claim");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Claim] FALHA. O servidor recusou a ativação.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Claim] Erro: {ex.Message}");
            }
        }
    }
}
