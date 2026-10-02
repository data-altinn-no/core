using Dan.Common;
using Dan.Core.Exceptions;
using Dan.Core.Models.Events;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class AltinnEventsServiceTest
    {
        private readonly IHttpClientFactory _mockHttpClientFactory = A.Fake<IHttpClientFactory>();
        private readonly ITokenRequesterService _mockTokenRequesterService = A.Fake<ITokenRequesterService>();
        private readonly ILogger<AltinnEventsService> _mockLogger = A.Fake<ILogger<AltinnEventsService>>();

        [TestInitialize]
        public void Initialize()
        {
            A.CallTo(() => _mockTokenRequesterService.GetAltinnExchangedToken(A<string>._, A<string>._))
                .Returns(Task.FromResult("{\"access_token\":\"test-token\"}"));
        }

        [TestMethod]
        public async Task Publish_PostsCloudEvent_WithBearerTokenAndIdempotencyKey()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;
            string? capturedContentType = null;

            var httpClient = TestHelpers.GetHttpClientMock(request =>
            {
                capturedRequest = request;
                capturedBody = request.Content?.ReadAsStringAsync().Result;
                capturedContentType = request.Content?.Headers.ContentType?.MediaType;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.AltinnEventsHttpClient)).Returns(httpClient);

            var service = new AltinnEventsService(_mockHttpClientFactory, _mockTokenRequesterService, _mockLogger);
            var cloudEvent = new AltinnCloudEvent
            {
                Id = "11111111-1111-5111-8111-111111111111",
                Type = ConsentEventTypes.Granted,
                Source = "https://test.data.altinn.no/v1/consent",
                Subject = "urn:altinn:organization:identifier-no:910402021",
                Resource = "urn:altinn:resource:digdir-data-altinn-no-consent",
                ResourceInstance = "aid-1",
                Time = DateTime.UtcNow
            };

            // Act
            await service.Publish(cloudEvent, cloudEvent.Id);

            // Assert
            Assert.IsNotNull(capturedRequest);
            Assert.AreEqual(HttpMethod.Post, capturedRequest!.Method);
            Assert.AreEqual("https://test.altinn.no/events/api/v1/events", capturedRequest.RequestUri!.AbsoluteUri);
            Assert.AreEqual("Bearer test-token", capturedRequest.GetHeader("Authorization"));
            Assert.AreEqual(cloudEvent.Id, capturedRequest.GetHeader("Idempotency-Key"));
            Assert.AreEqual("application/cloudevents+json", capturedContentType);

            A.CallTo(() => _mockTokenRequesterService.GetAltinnExchangedToken("altinn:events.publish altinn:serviceowner", A<string>._))
                .MustHaveHappenedOnceExactly();

            var sent = JsonConvert.DeserializeObject<AltinnCloudEvent>(capturedBody!);
            Assert.IsNotNull(sent);
            Assert.AreEqual("1.0", sent!.SpecVersion);
            Assert.AreEqual(cloudEvent.Type, sent.Type);
            Assert.AreEqual(cloudEvent.Subject, sent.Subject);
            Assert.AreEqual(cloudEvent.Resource, sent.Resource);
            Assert.AreEqual(cloudEvent.ResourceInstance, sent.ResourceInstance);
        }

        [TestMethod]
        public async Task Publish_ServerError_ThrowsServiceNotAvailable()
        {
            // Arrange
            var httpClient = TestHelpers.GetHttpClientMock(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.AltinnEventsHttpClient)).Returns(httpClient);

            var service = new AltinnEventsService(_mockHttpClientFactory, _mockTokenRequesterService, _mockLogger);

            // Act / Assert
            await Assert.ThrowsExactlyAsync<ServiceNotAvailableException>(() =>
                service.Publish(new AltinnCloudEvent { Id = "x", Type = ConsentEventTypes.Denied }, "x"));
        }

        [TestMethod]
        public async Task Publish_TokenMissing_ThrowsServiceNotAvailable()
        {
            // Arrange
            A.CallTo(() => _mockTokenRequesterService.GetAltinnExchangedToken(A<string>._, A<string>._))
                .Returns(Task.FromResult("{}"));
            var httpClient = TestHelpers.GetHttpClientMock(_ => new HttpResponseMessage(HttpStatusCode.OK));
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.AltinnEventsHttpClient)).Returns(httpClient);

            var service = new AltinnEventsService(_mockHttpClientFactory, _mockTokenRequesterService, _mockLogger);

            // Act / Assert
            await Assert.ThrowsExactlyAsync<ServiceNotAvailableException>(() =>
                service.Publish(new AltinnCloudEvent { Id = "x", Type = ConsentEventTypes.Denied }, "x"));
        }
    }
}
