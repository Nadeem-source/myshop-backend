using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RegisterApi.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly string Key;

        // Static constructor (runs once)
        static EncryptionHelper()
        {
            Key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");

            if (string.IsNullOrEmpty(Key) || Key.Length != 32)
                throw new Exception("ENCRYPTION_KEY must be exactly 32 chars long.");
        }

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.GenerateIV(); // auto IV

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // prepend IV
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            var allBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);

            // Extract IV
            var iv = new byte[16];
            Array.Copy(allBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(allBytes, 16, allBytes.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        // ================= SAFE DECRYPT =================
        // 🔥 Kabhi exception throw nahi karega
        public static string SafeDecrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            try
            {
                return Decrypt(value); // encrypted case
            }
            catch
            {
                // plain "0" / "1" case
                return value;
            }
        }
    }
}
