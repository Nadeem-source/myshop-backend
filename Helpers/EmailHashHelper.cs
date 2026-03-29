using System.Security.Cryptography;
using System.Text;

namespace RegisterApi.Helpers
{
    public static class EmailHashHelper
    {
        private static readonly string SecretKey = "replace-with-64-char-super-secret-key";

        public static string HashEmail(string email)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(email.ToLower().Trim()));
                return Convert.ToHexString(hash);   // deterministic hex
            }
        }
    }
}
