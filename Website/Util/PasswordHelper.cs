using BC = BCrypt.Net.BCrypt;

namespace Website.Util
{
    public static class PasswordHelper
    {
        public static string GetHash(string password) => BC.HashPassword(password);
        public static bool AreEqual(string password, string hash) => BC.Verify(password, hash);
    }
}
