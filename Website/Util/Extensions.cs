using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Website.Data;
using Website.Model;

namespace Website.Util
{
#pragma warning disable SYSLIB0022
    public static class Extensions
    {
        // Get cookie -> decrypt -> deserialize -> return data
        public static bool TryGetData<T>(this HttpRequest request, string? id, out T? result)
        {
            result = default;
            try
            {
                // Decrypt the cookie and deserialize it into the provided type
                var decryptString = DecryptString(request.Cookies["__webdata_" + id]!, Program.Key.Key);
                result = JsonSerializer.Deserialize<T>(decryptString)!;
            }
            catch (Exception e)
            {
                // If decryption or deserialization fails, return false
                return false;
            }
            return true;
        }

        public static bool RequiresAuthorization(this Endpoint e)
        {
            if (e?.Metadata == null)
                return false;

            if (e.Metadata.Any(m => m is Auth))
                return true;

            var handler = e.Metadata.FirstOrDefault(m => m is MethodInfo) as MethodInfo;
            if (handler != null)
            {
                var declaringType = handler.DeclaringType;
                if (declaringType != null && declaringType.GetCustomAttribute<Auth>() != null)
                {
                    return true;
                }
            }

            return false;
        }
        public static void RedirToAuth(this HttpResponse response, string redir = "")
        {
            response.Redirect("/login" + (!string.IsNullOrWhiteSpace(redir) ? $"?redir={redir}" : ""));
        }
        public static string GetIssueTypeIconPath(this IssueType type)
        {
            return type switch
            {
                IssueType.None => "/none.png",
                IssueType.Bug => "/bug.png",
                IssueType.FeatureRequest => "/feature_request.png",
                IssueType.Improvement => "/improvement.png",
                IssueType.Critical => "/critical.png",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        public static string GetIssueTypeName(this IssueType type)
        {
            return type switch
            {
                IssueType.None => "Не задано",
                IssueType.Bug => "Ошибка",
                IssueType.FeatureRequest => "Запрос фичи",
                IssueType.Improvement => "Улучшение",
                IssueType.Critical => "Критическая ошибка",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static string GetIssueSolutionName(this IssueSolution solution)
        {
            return solution switch
            {
                IssueSolution.None => "Нет решения",
                IssueSolution.Duplicate => "Дубликат",
                IssueSolution.AwaitingResponse => "Ожидает ответа",
                IssueSolution.Invalid => "Некорректная задача",
                IssueSolution.Fixed => "Исправлено",
                IssueSolution.WontFix => "Не будет исправлено",
                IssueSolution.Deferred => "Отложено",
                IssueSolution.NotABug => "Не является багом",
                _ => "Неизвестное решение" // На случай, если будет добавлено новое значение
            };
        }

        public static string GetIssueStatusName(this IssueStatus status)
        {
            return status switch
            {
                IssueStatus.Open => "Открыто",
                IssueStatus.Resolved => "Решено",
                IssueStatus.InProgress => "В работе",
                IssueStatus.Confirmed => "Подтверждено",
                IssueStatus.Closed => "Закрыто",
                IssueStatus.AwaitingConfirmation => "Ожидает подтверждения",
                IssueStatus.AwaitingTesting => "Ожидает тестирования",
                IssueStatus.Deferred => "Отложено",
                IssueStatus.CannotReproduce => "Не удается воспроизвести",
                IssueStatus.Cancelled => "Отменено",
                IssueStatus.WontFix => "Не будет исправлено",

                _ => "Неизвестно"
            };
        }

        public static string GetIssueConfirmation(this ConfirmationStatus status)
        {
            return status switch
            {
                ConfirmationStatus.None => "Не задано",
                ConfirmationStatus.Confirmed => "Подтверждено",
                ConfirmationStatus.AwaitingConfirmation => "Ожидает подтверждения",
                ConfirmationStatus.Rejected => "Отклонено",
                ConfirmationStatus.InProgress => "В работе",
                ConfirmationStatus.NeedsReview => "Требует проверки",
                ConfirmationStatus.CannotReproduce => "Не удается воспроизвести",
                _ => "Неизвестно"
            };
        }
        public static string SetData(this HttpResponse response, string? id, dynamic data)
        {
            // Serialize and encrypt data before setting it in the cookie
            string res;
            response.Cookies.Append("__webdata_" + id, res = EncryptString(JsonSerializer.Serialize(data), Program.Key.Key));
            return res;
        }
        [Obsolete("Use Enum.HasFlag(Enum) Instead")]
        public static bool HasRight(this UserRights rights, UserRights right)
        {
            return (rights & right) != 0;
        }
        public static bool TryAuthenticate(this HttpRequest context, out UserModel model)
        {
            model = null!;
            try
            {
                if (context.HttpContext.User.Identity is { IsAuthenticated: true })
                {
                    // Fetch user from the database based on JWT claims
                    model = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>()
                        .Users
                        .Include(s => s.UserRank)
                        .First(s =>
                            s.Username == context.HttpContext.User.FindFirst(ClaimTypes.Name)!.Value
                            && s.Email == context.HttpContext.User.FindFirst(ClaimTypes.Email)!.Value);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static bool TryAuthenticate(this HttpRequest context, Func<UserModel, bool> pred, out UserModel model)
        {
            model = null!;
            try
            {
                // Fetch user based on a predicate
                model = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>().Users.First(pred);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private const string initVector = "aaaaaaaaaaaaaaaa";  // Initialization vector for AES encryption
        private const int keysize = 256;  // Key size for encryption

        // Encrypt a string using AES encryption with a passphrase
        public static string EncryptString(string plainText, byte[] passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Use Rfc2898DeriveBytes to generate the key bytes
            using (var password = new Rfc2898DeriveBytes(Encoding.UTF8.GetString(passPhrase), [0x5A, 0x6F, 0x6E, 0xE4, 0x5D, 0x72]))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
                    using (MemoryStream memoryStream = new MemoryStream())
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        byte[] cipherTextBytes = memoryStream.ToArray();
                        return Convert.ToBase64String(cipherTextBytes);
                    }
                }
            }
        }

        // Decrypt an encrypted string
        public static string DecryptString(string cipherText, byte[] passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            // Use Rfc2898DeriveBytes to generate the key bytes
            using (var password = new Rfc2898DeriveBytes(Encoding.UTF8.GetString(passPhrase), new byte[] { 0x5A, 0x6F, 0x6E, 0xE4, 0x5D, 0x72 }))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                    using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                    }
                }
            }
        }
    }
}
