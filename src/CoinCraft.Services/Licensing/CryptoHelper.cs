using System;
using System.Security.Cryptography;
using System.Text;

namespace CoinCraft.Services.Licensing
{
    public static class CryptoHelper
    {
        public static byte[] Protect(byte[] data)
        {
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }

        public static byte[] Unprotect(byte[] encrypted)
        {
            return ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        }

        public static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}