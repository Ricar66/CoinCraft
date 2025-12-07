using System;
using System.Threading.Tasks;
using CoinCraft.Services.Licensing;

namespace TestLicense
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- Teste de Validação de Licença ---");
            var email = "camilapimentasouza@gmail.com";
            
            try 
            {
                var hwId = HardwareHelper.GetHardwareId();
                Console.WriteLine($"Hardware ID Gerado: {hwId}");
                
                var service = new LicenseService();
                Console.WriteLine($"Verificando e-mail: {email} na API...");
                
                var resultado = await service.VerificarLicenca(email, hwId);
                
                if (resultado)
                {
                    Console.WriteLine("SUCESSO: A licença é VÁLIDA.");
                }
                else
                {
                    Console.WriteLine("FALHA: A licença é INVÁLIDA ou não encontrada.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}