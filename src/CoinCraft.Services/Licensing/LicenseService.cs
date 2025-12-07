using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoinCraft.Services.Licensing
{
    public class LicenseService
    {
        private const string Endpoint = "https://codecraftgenz.com.br/api/compat/license-check";
        private const int AppId = 103;

        public async Task<bool> VerificarLicenca(string email, string hardwareId)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                string urlFinal =
                    $"{Endpoint}?email={Uri.EscapeDataString(email)}&id_pc={Uri.EscapeDataString(hardwareId)}&app_id={AppId}";

                var response = await client.GetAsync(urlFinal);
                var text = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                var ok = root.TryGetProperty("success", out var s) && s.GetBoolean();
                var msg = root.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";
                // Se quiser exibir mensagem:
                // Console.WriteLine(msg);
                return ok;
            }
            catch
            {
                return false;
            }
        }
    }
}