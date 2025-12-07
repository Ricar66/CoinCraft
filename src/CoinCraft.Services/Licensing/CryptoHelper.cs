using System;
using System.Security.Cryptography;
using System.Text;

namespace CoinCraft.Services.Licensing
{
    /// <summary>
    /// Auxiliar para operações criptográficas:
    /// 1. Proteção de dados locais (DPAPI)
    /// 2. Hashing para geração de Fingerprint
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// Protege dados sensíveis usando DPAPI (Data Protection API) do Windows.
        /// Os dados só podem ser descriptografados pelo mesmo usuário na mesma máquina.
        /// </summary>
        public static byte[] Protect(byte[] data)
        {
            // DataProtectionScope.CurrentUser garante que apenas o usuário logado pode descriptografar
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }

        /// <summary>
        /// Descriptografa dados protegidos com DPAPI.
        /// </summary>
        public static byte[] Unprotect(byte[] encrypted)
        {
            return ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        }

        /// <summary>
        /// Calcula o hash SHA-256 de uma string.
        /// Usado para gerar o identificador único (Fingerprint) da máquina de forma consistente.
        /// </summary>
        public static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}