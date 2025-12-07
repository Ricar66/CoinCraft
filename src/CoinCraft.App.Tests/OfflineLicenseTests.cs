using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CoinCraft.Services.Licensing;
using FluentAssertions;
using Xunit;

namespace CoinCraft.App.Tests
{
    public class OfflineLicenseTests
    {
        [StaFact]
        public void OfflineActivation_SucceedsWithRsaXml()
        {
            using var rsa = RSA.Create(2048);
            using var rsaCsp = new RSACryptoServiceProvider();
            rsaCsp.ImportParameters(rsa.ExportParameters(false));
            var publicXml = rsaCsp.ToXmlString(false);
            var tempXml = Path.Combine(Path.GetTempPath(), $"public_test_{Guid.NewGuid():N}.xml");
            File.WriteAllText(tempXml, publicXml);

            Environment.SetEnvironmentVariable("COINCRAFT_PUBLICKEY_XML_PATH", tempXml);
            Environment.SetEnvironmentVariable("COINCRAFT_ALLOW_OFFLINE", "1");

            try
            {
                var licenseService = new LicenseService();
                using var licensing = new LicensingService(licenseService);

                var fp = licensing.CurrentFingerprint;
                var data = Encoding.UTF8.GetBytes(fp);
                var sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var licenseKey = Convert.ToBase64String(sig);

                var res = licensing.EnsureLicensedAsync(() => Task.FromResult<string?>(licenseKey)).GetAwaiter().GetResult();

                res.IsValid.Should().BeTrue();
                licensing.CurrentState.Should().Be(LicenseState.Active);
            }
            finally
            {
                Environment.SetEnvironmentVariable("COINCRAFT_PUBLICKEY_XML_PATH", null);
                Environment.SetEnvironmentVariable("COINCRAFT_ALLOW_OFFLINE", null);
                try { File.Delete(tempXml); } catch { }
                try { File.Delete(LicensingStorage.StoragePath); } catch { }
            }
        }
    }
}
