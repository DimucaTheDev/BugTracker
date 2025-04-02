using Website.Util;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
namespace WebUnitTest
{
    [TestClass]
    public class FunctionTest
    {
        [TestMethod]
        public void CompareTwoPasswordUniqueHashes()
        {
            var password = "TheBestPassword_4ever";
            var hash1 = PasswordHelper.GetHash(password);
            var hash2 = PasswordHelper.GetHash(password);
            //Salt must be different for every generated password
            AreNotEqual(hash1.Salt, hash2.Salt);
            AreNotEqual(hash1.Hash, hash2.Hash);
        }
        [TestMethod]
        public void CompareTwoPasswordHashesWithDefinedSalt()
        {
            var password = "TheBestPassword_4ever";
            var passwordHash = "9920cd6a610ea9becfcea3b6f310ba30261beb6804d5d3f51829bddeb2eb766538c5e5ad70e98ba1d87cbdaad81a36b36822e3ed21379cb1449085698c327b07";
            var salt = "SaltyString";
            var hash1 = PasswordHelper.GetHash(password, salt);
            AreEqual(hash1.Hash, passwordHash);
        }
    }
}