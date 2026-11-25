using System.Diagnostics.CodeAnalysis;
using System.Net;
using Altinn.Dd.Correspondence.Services;
using Dan.Core.Exceptions;
using Dan.Core.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace Dan.Core.UnitTest
{
    /// <summary>
    /// Covers Altinn3ConsentService.GetConsentRequestStatus, which reads the consent request from Altinn. Unlike the
    /// Maskinporten token endpoint, Altinn tells a rejected request apart from one that is not yet answered.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class A3ConsentRequestStatusTest
    {
        private const string ConsentId = "0197e1a3-6f5c-7c5e-9d0e-2f3b8a1c4d5e";

        private readonly ITokenRequesterService _tokenRequester = A.Fake<ITokenRequesterService>();
        private HttpRequestMessage _sentRequest;

        [TestInitialize]
        public void Initialize()
        {
            A.CallTo(() => _tokenRequester.GetMaskinportenToken("altinn:consentrequests.org", A<string>._))
                .Returns(Task.FromResult("{\"access_token\":\"mp-token\",\"token_type\":\"Bearer\",\"expires_in\":119,\"scope\":\"altinn:consentrequests.org\"}"));
        }

        [TestMethod]
        [DataRow("Created", Altinn3ConsentRequestStatus.Created)]
        [DataRow("Rejected", Altinn3ConsentRequestStatus.Rejected)]
        [DataRow("Accepted", Altinn3ConsentRequestStatus.Accepted)]
        [DataRow("Revoked", Altinn3ConsentRequestStatus.Revoked)]
        [DataRow("Deleted", Altinn3ConsentRequestStatus.Deleted)]
        [DataRow("Expired", Altinn3ConsentRequestStatus.Expired)]
        [DataRow("rejected", Altinn3ConsentRequestStatus.Rejected)]
        public async Task ReturnsTheStatusAltinnReports(string statusFromAltinn, Altinn3ConsentRequestStatus expected)
        {
            var service = CreateService(_ => Ok(ConsentRequest(statusFromAltinn)));

            var result = await service.GetConsentRequestStatus(ConsentId);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public async Task ReturnsUnknown_WhenAltinnReportsAStatusWeDoNotKnow()
        {
            var service = CreateService(_ => Ok(ConsentRequest("SomethingNew")));

            var result = await service.GetConsentRequestStatus(ConsentId);

            Assert.AreEqual(Altinn3ConsentRequestStatus.Unknown, result);
        }

        [TestMethod]
        public async Task ReadsTheConsentRequestByIdWithAMaskinportenToken()
        {
            var service = CreateService(_ => Ok(ConsentRequest("Created")));

            await service.GetConsentRequestStatus(ConsentId);

            Assert.AreEqual(HttpMethod.Get, _sentRequest.Method);
            // Altinn3ConsentUrl in appsettings.unittest.json
            Assert.AreEqual("https://test.altinn.no/api/consent/" + ConsentId, _sentRequest.RequestUri!.ToString());
            Assert.AreEqual("Bearer mp-token", _sentRequest.Headers.Authorization!.ToString());
        }

        [TestMethod]
        public async Task ReturnsNotFound_WhenAltinnDoesNotKnowTheConsentRequest()
        {
            var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"title\":\"Not Found\",\"status\":404}")
            });

            var result = await service.GetConsentRequestStatus(ConsentId);

            Assert.AreEqual(Altinn3ConsentRequestStatus.NotFound, result);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.BadRequest)]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public async Task Throws_WhenAltinnAnswersWithAnError(HttpStatusCode statusCode)
        {
            var service = CreateService(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"title\":\"Not authorized for consent\",\"code\":\"AM-00000\"}")
            });

            await Assert.ThrowsAsync<ServiceNotAvailableException>(() => service.GetConsentRequestStatus(ConsentId));
        }

        private Altinn3ConsentService CreateService(Func<HttpRequestMessage, HttpResponseMessage> altinn)
        {
            var httpClient = TestHelpers.GetHttpClientMock(request =>
            {
                _sentRequest = request;
                return altinn(request);
            });

            return new Altinn3ConsentService(
                httpClient,
                TestHelpers.GetHttpClientMock("[{}]"),
                A.Fake<ILogger<Altinn3ConsentService>>(),
                A.Fake<IDdCorrespondenceService>(),
                A.Fake<IEntityRegistryService>(),
                A.Fake<IAltinnServiceOwnerApiService>(),
                A.Fake<IRequestContextService>(),
                _tokenRequester);
        }

        private static HttpResponseMessage Ok(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }

        /// <summary>The fields of Altinn's consent request details that matter here, in the shape the enterprise API returns.</summary>
        private static string ConsentRequest(string status)
        {
            return "{\"id\":\"" + ConsentId + "\"," +
                   "\"from\":\"urn:altinn:organization:identifier-no:910402021\"," +
                   "\"to\":\"urn:altinn:organization:identifier-no:958935420\"," +
                   "\"handledBy\":\"urn:altinn:organization:identifier-no:991825827\"," +
                   "\"validTo\":\"2030-01-01T00:00:00+00:00\"," +
                   "\"consentRights\":[]," +
                   "\"status\":\"" + status + "\"," +
                   "\"consented\":null," +
                   "\"redirectUrl\":\"https://localhost/api/consent/abc\"," +
                   "\"viewUri\":\"https://am.ui.tt02.altinn.no/accessmanagement/ui/consent/request?id=" + ConsentId + "\"}";
        }
    }
}
