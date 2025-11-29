using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CoinCraft.Services.Licensing;

class Program
{
    static int Main(string[] args)
    {
        Console.Write("Hardware ID: ");
        var input = Console.ReadLine();
        var hwid = string.IsNullOrWhiteSpace(input) ? MachineIdProvider.ComputeFingerprint() : input!.Trim();
        var path = Environment.GetEnvironmentVariable("COINCRAFT_PRIVATE_XML_PATH");
        if (string.IsNullOrWhiteSpace(path)) path = Path.Combine(AppContext.BaseDirectory, "private.xml");
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("Arquivo private.xml não encontrado: " + path);
            return 1;
        }
        var keyText = File.ReadAllText(path);
        var data = Encoding.UTF8.GetBytes(hwid);
        byte[] sig;
        if (keyText.Contains("<RSAKeyValue>"))
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(keyText);
            sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        else if (keyText.Contains("BEGIN EC") || keyText.Contains("BEGIN PRIVATE KEY") || keyText.Contains("BEGIN EC PRIVATE KEY"))
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(keyText);
                sig = ecdsa.SignData(data, HashAlgorithmName.SHA256);
            }
            catch
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(keyText);
                sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
        else
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(keyText);
            sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        var license = Convert.ToBase64String(sig);
        Console.WriteLine("License:");
        Console.WriteLine(license);
        return 0;
    }
}
