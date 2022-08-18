using System.Diagnostics.CodeAnalysis;
using Dan.Core.Helpers;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class JwtTest
    {
        [TestMethod]
        public void Jwt_Signature()
        {
            string payload = "this is a payload";
            string token = Jwt.GetDigestJwt(payload);

            Assert.IsFalse(String.IsNullOrEmpty(token));
            Assert.IsTrue(Jwt.VerifyTokenSignature(token)); ;
        }
    }
}
