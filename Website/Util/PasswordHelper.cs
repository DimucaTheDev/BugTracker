using BC = BCrypt.Net.BCrypt;

namespace Website.Util
{
    public static class PasswordHelper
    {
        public const int SaltLength = 45;
        private const int PasswordInsertSalt = 4;
        public static string GetHash(string password)
        {
            //if (string.IsNullOrEmpty(salt))
            //Generating random salt value length of SaltLength
            //    salt = new string(Enumerable.Range(0, SaltLength).Select(c => (char)Random.Shared.Next(char.MinValue, char.MaxValue)).ToArray());
            //using var sha512 = SHA512.Create();
            string hash = BC.HashPassword(password); //BitConverter.ToString(sha512.ComputeHash(Encoding.UTF32.GetBytes(password.Insert(PasswordInsertSalt, salt)))).ToLower().Replace("-", "");
            return hash;
        }

        public static bool AreEqual(string password, string hash)
        {
            return BC.Verify(password, hash); //GetHash(password, salt).Hash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
