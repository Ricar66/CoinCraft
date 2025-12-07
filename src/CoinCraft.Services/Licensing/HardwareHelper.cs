using System;
using System.Management;
using System.Text;
using System.Security.Cryptography;

namespace CoinCraft.Services.Licensing
{
    public static class HardwareHelper
    {
        public static string GetProcessorId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    return obj["ProcessorId"]?.ToString() ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GetMotherboardSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString() ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }

        public static string ComputeHardwareId()
        {
            var processorId = GetProcessorId();
            var motherboardSerial = GetMotherboardSerial();
            var composite = $"PROC={processorId};MB={motherboardSerial}";
            var fullHash = CryptoHelper.ComputeSha256(composite);
            
            // O PHP aceita no máximo 32 caracteres. Cortamos para 30 para segurança.
            return fullHash.Substring(0, 64).ToUpper();
        }

        public static string GetHardwareId()
        {
            return ComputeHardwareId();
        }
    }
}
