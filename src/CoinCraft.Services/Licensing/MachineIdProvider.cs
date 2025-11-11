using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;

namespace CoinCraft.Services.Licensing
{
    public static class MachineIdProvider
    {
        public static string GetMacAddress()
        {
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(n => n.GetPhysicalAddress()?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                return nics.FirstOrDefault() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        public static string GetMotherboardSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(serial)) return serial!;
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GetDiskId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");
                foreach (var obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(serial)) return serial!;
                }
            }
            catch { }
            return string.Empty;
        }

        public static string ComputeFingerprint()
        {
            var mac = GetMacAddress();
            var mb = GetMotherboardSerial();
            var disk = GetDiskId();
            var composite = $"MAC={mac};MB={mb};DISK={disk}";
            return CryptoHelper.ComputeSha256(composite);
        }
    }
}