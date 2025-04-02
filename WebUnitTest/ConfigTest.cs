using Website.Data;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace WebUnitTest
{
    [TestClass]
    public class ConfigTest
    {
        [TestMethod]
        public void CheckIfConfigIsNull()
        {
            IsNotNull(Config.Instance);
        }
    }
}
