/*using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Dan.Core.UnitTest.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nadobe.Core.Exceptions;
using Nadobe.Core.Extensions.Http;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class HttpRequestMessageExtensionsTest
    {
        private const string CertHeaderName = "X-NADOBE-CERT";
        private readonly ILogger nullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        [TestMethod]
        public void GetQueryParamTest_ParameterFoundReturnsValue()
        {
            // Arrange
            string requestUrl = "http://unittest/api/accreditations?parameter=value";

            var target = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Act
            string actual = target.GetQueryParam("parameter");

            // Assert
            Assert.AreEqual("value", actual);
        }

        [TestMethod]
        public void GetQueryParamTest_ParameterWithWrongCaseFoundReturnsValue()
        {
            // Arrange
            string requestUrl = "http://unittest/api/accreditations?parameter=value";

            var target = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Act
            string actual = target.GetQueryParam("PARAMETER");

            // Assert
            Assert.AreEqual("value", actual);
        }

        [TestMethod]
        public void GetQueryParamTest_ParameterNotFoundReturnsNull()
        {
            // Arrange
            string requestUrl = "http://unittest/api/accreditations";

            var target = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Act
            string actual = target.GetQueryParam("parameter");

            // Assert
            Assert.IsNull(actual);
        }

        [TestMethod]
        public async Task GetCertificateOrgNumberTest_RequestContainsValidCertificateReturnsCorrectOrgNumber()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://unittest/api");
            request.Headers.Add(CertHeaderName, Certificates.UNUSED_CERT);
            var authorizeFilter = new Filters.AuthorizeAttribute();
            await authorizeFilter.OnExecutingAsync(request, new ExecutionContext(), nullLogger);

            // Act
            string actual = request.GetAuthenticatedPartyOrgNumber();

            // Assert
            Assert.AreEqual(Certificates.UNUSED_ORG, actual);
        }

        [TestMethod]
        public void GetCertificateOrgNumberTest_RequestHasNoCertificateThrowsException()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://unittest/api");
            MissingAuthenticationException actual = null;

            // Act
            try
            {
                request.GetAuthenticatedPartyOrgNumber();
            }
            catch (MissingAuthenticationException exception)
            {
                actual = exception;
            }

            // Assert
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Message.Contains("provided"));
        }

        [TestMethod]
        public async Task GetCertificateOrgNumberTest_RequestHeaderCannotBeSerializedAsCertificateThrowsException()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://unittest/api");
            request.Headers.Add(CertHeaderName, "RGVubmUgdGVrc3RlbiBrYW4gaWtrZSBibGkgdG9sa2V0IHNvbSBldCBzZXJ0aWZpa2F0Lg==");
            var authorizeFilter = new Filters.AuthorizeAttribute();

            InvalidCertificateException actual = null;

            // Act
            try
            {
                await authorizeFilter.OnExecutingAsync(request, new ExecutionContext(), nullLogger);
                request.GetAuthenticatedPartyOrgNumber();
            }
            catch (InvalidCertificateException exception)
            {
                actual = exception;
            }

            // Assert
            Assert.IsNotNull(actual);
        }
    }
}
*/