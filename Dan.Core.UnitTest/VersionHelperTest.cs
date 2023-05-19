using System.Diagnostics.CodeAnalysis;
using Dan.Core.Helpers;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class VersionHelperTest
    {
        [TestMethod]
        public void VersionHelperReturnsValidData()
        {
            var versionInfo = VersionHelper.GetVersionInfo();

            Assert.IsNotNull(versionInfo);
            Assert.IsNotNull(versionInfo.Name);
            Assert.IsNotNull(versionInfo.Built);
            Assert.IsNotNull(versionInfo.Commit);
            Assert.IsNotNull(versionInfo.CommitDate);
        }
    }
}
