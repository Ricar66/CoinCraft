using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using CoinCraft.Services.Licensing;

namespace TestLicenseIntegration
{
    class Program
    {
        // Credenciais de Teste Fornecidas
        private const string TestEmail = "camilapimentasouza@gmail.com";
        private const string TestHardwareId = "d2a3c00c7b27daea8b218943fbc02801";
        private const string TestLicenseKeyExpected = "LIC-1765030382223-5";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=========================================================");
            Console.WriteLine("  TESTE DE INTEGRAÇÃO DE LICENCIAMENTO - COINCRAFT");
            Console.WriteLine("=========================================================");
            Console.WriteLine($"Data/Hora: {DateTime.Now}");
            Console.WriteLine($"Hardware ID (Simulado): {TestHardwareId}");
            Console.WriteLine($"E-mail: {TestEmail}");
            Console.WriteLine("---------------------------------------------------------");

            // 1. Teste de Comunicação Bruta com a API (Para inspeção de resposta)
            Console.WriteLine("\n[1] TESTE DE COMUNICAÇÃO API (RAW)");
            await TestRawApiAsync();

            // 2. Teste do Serviço de Licença (Integração)
            Console.WriteLine("\n[2] TESTE DO SERVIÇO LicenseService");
            bool isLicensed = await TestLicenseServiceAsync();

            // 3. Teste de Persistência (Simulação do App.xaml.cs)
            Console.WriteLine("\n[3] TESTE DE PERSISTÊNCIA LOCAL");
            TestPersistence(isLicensed);

            Console.WriteLine("\n=========================================================");
            Console.WriteLine("  FIM DOS TESTES");
            Console.WriteLine("=========================================================");
        }

        static async Task TestRawApiAsync()
        {
            var url = "https://codecraftgenz.com.br/api/verify-license";
            var payload = new { email = TestEmail, hardware_id = TestHardwareId };
            var json = JsonSerializer.Serialize(payload);
            
            Console.WriteLine($"POST {url}");
            Console.WriteLine($"Payload: {json}");

            using var client = new HttpClient();
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                stopwatch.Stop();

                Console.WriteLine($"Status Code: {response.StatusCode} ({(int)response.StatusCode})");
                Console.WriteLine($"Tempo de Resposta: {stopwatch.ElapsedMilliseconds}ms");

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Corpo da Resposta:");
                Console.WriteLine(responseBody);

                // Análise do conteúdo
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("success", out var successEl))
                    {
                        Console.WriteLine($"Campo 'success': {successEl.GetBoolean()}");
                    }
                    if (doc.RootElement.TryGetProperty("license_key", out var keyEl))
                    {
                        var key = keyEl.GetString();
                        Console.WriteLine($"Campo 'license_key': {key}");
                        if (key == TestLicenseKeyExpected)
                            Console.WriteLine(">>> CONFIRMAÇÃO: Chave de licença corresponde à esperada.");
                        else
                            Console.WriteLine($">>> AVISO: Chave recebida difere da esperada ({TestLicenseKeyExpected}).");
                    }
                }
                else
                {
                    Console.WriteLine(">>> ERRO: Requisição falhou.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> EXCEÇÃO: {ex.Message}");
            }
        }

        static async Task<bool> TestLicenseServiceAsync()
        {
            var service = new LicenseService();
            Console.WriteLine("Chamando LicenseService.VerificarLicenca()...");
            
            var stopwatch = Stopwatch.StartNew();
            var result = await service.VerificarLicenca(TestEmail, TestHardwareId);
            stopwatch.Stop();

            Console.WriteLine($"Resultado do Serviço: {result}");
            Console.WriteLine($"Tempo de Execução: {stopwatch.ElapsedMilliseconds}ms");

            if (result)
                Console.WriteLine(">>> SUCESSO: O serviço validou a licença corretamente.");
            else
                Console.WriteLine(">>> FALHA: O serviço retornou false.");

            return result;
        }

        static void TestPersistence(bool isLicensed)
        {
            if (!isLicensed)
            {
                Console.WriteLine("Pulei teste de persistência pois a licença não foi validada.");
                return;
            }

            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft");
            var licenseFile = Path.Combine(appData, "license.dat");

            Console.WriteLine($"Caminho do arquivo: {licenseFile}");

            // Simular a lógica de salvamento do App.xaml.cs / ViewModel
            try
            {
                if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
                
                // Salvar
                File.WriteAllText(licenseFile, TestEmail);
                Console.WriteLine("Arquivo gravado (simulação).");

                // Verificar
                if (File.Exists(licenseFile))
                {
                    var content = File.ReadAllText(licenseFile);
                    Console.WriteLine($"Conteúdo lido do arquivo: {content}");
                    
                    if (content.Trim() == TestEmail)
                        Console.WriteLine(">>> SUCESSO: Persistência de e-mail verificada.");
                    else
                        Console.WriteLine(">>> FALHA: Conteúdo do arquivo incorreto.");
                }
                else
                {
                    Console.WriteLine(">>> FALHA: Arquivo não encontrado após gravação.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> ERRO NA PERSISTÊNCIA: {ex.Message}");
            }
        }
    }
}