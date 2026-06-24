using Dan.Common;
using Dan.Common.Models;
using Dan.Core.Models.Notifications;
using Dan.Core.ServiceContextTexts;
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
    public class Altinn3NotificationsServiceTest
    {
        private readonly IHttpClientFactory _mockHttpClientFactory = A.Fake<IHttpClientFactory>();
        private readonly ITokenRequesterService _mockTokenRequesterService = A.Fake<ITokenRequesterService>();
        private readonly IEntityRegistryService _mockEntityRegistryService = A.Fake<IEntityRegistryService>();
        private readonly ILogger<Altinn3NotificationsService> _mockLogger = A.Fake<ILogger<Altinn3NotificationsService>>();

        [TestInitialize]
        public void Initialize()
        {
            A.CallTo(() => _mockTokenRequesterService.GetAltinnExchangedToken(A<string>._, A<string>._))
                .Returns(Task.FromResult("{\"access_token\":\"test-token\"}"));

            A.CallTo(() => _mockEntityRegistryService.Get(A<string>._))
                .Returns(Task.FromResult(new SimpleEntityRegistryUnit { Name = "Test Organization" }));
        }

        [TestMethod]
        public async Task SendReminder_OrganizationSubject_PostsEmailAndSmsOrder_ReturnsSentReceipts()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;
            var orderId = Guid.NewGuid();

            var httpClient = TestHelpers.GetHttpClientMock(request =>
            {
                capturedRequest = request;
                capturedBody = request.Content?.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent($"{{\"notificationOrderId\":\"{orderId}\",\"notification\":{{\"shipmentId\":\"{Guid.NewGuid()}\"}}}}")
                };
            });
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.Altinn3NotificationsHttpClient)).Returns(httpClient);

            var service = CreateService();
            var accreditation = GetAccreditation();

            // Act
            var result = await service.SendReminder(accreditation, GetServiceContext());

            // Assert: two receipts, both sent
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(r => r.Success));
            CollectionAssert.AreEquivalent(new[] { "Email", "SMS" }, result.Select(r => r.NotificationType).ToList());

            // Assert: posted to the future/orders endpoint with a bearer token
            Assert.IsNotNull(capturedRequest);
            Assert.AreEqual(HttpMethod.Post, capturedRequest!.Method);
            StringAssert.EndsWith(capturedRequest.RequestUri!.AbsoluteUri, "/notifications/api/v1/future/orders");
            Assert.AreEqual("Bearer test-token", capturedRequest.GetHeader("Authorization"));

            // Assert: request body targets the organization with EmailAndSms and rendered content
            var order = JsonConvert.DeserializeObject<NotificationOrderChainRequest>(capturedBody!);
            Assert.IsNotNull(order);
            Assert.AreEqual(accreditation.AccreditationId, order!.SendersReference);
            Assert.IsNotNull(order.Recipient.RecipientOrganization);
            Assert.IsNull(order.Recipient.RecipientPerson);
            Assert.AreEqual("910402021", order.Recipient.RecipientOrganization!.OrgNumber);
            Assert.AreEqual(NotificationChannel.EmailAndSms, order.Recipient.RecipientOrganization.ChannelSchema);
            Assert.IsFalse(string.IsNullOrWhiteSpace(order.Recipient.RecipientOrganization.EmailSettings!.Subject));
            Assert.IsFalse(string.IsNullOrWhiteSpace(order.Recipient.RecipientOrganization.EmailSettings.Body));
            Assert.IsFalse(string.IsNullOrWhiteSpace(order.Recipient.RecipientOrganization.SmsSettings!.Body));
            Assert.AreEqual("urn:altinn:resource:digdir-data-altinn-no-melding", order.Recipient.RecipientOrganization.ResourceId);
            Assert.AreEqual("read", order.Recipient.RecipientOrganization.ResourceAction);
        }

        [TestMethod]
        public async Task SendReminder_PersonSubject_UsesRecipientPerson()
        {
            // Arrange
            string? capturedBody = null;
            var httpClient = TestHelpers.GetHttpClientMock(request =>
            {
                capturedBody = request.Content?.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent($"{{\"notificationOrderId\":\"{Guid.NewGuid()}\"}}")
                };
            });
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.Altinn3NotificationsHttpClient)).Returns(httpClient);

            var service = CreateService();
            var accreditation = GetAccreditation();
            accreditation.SubjectParty = new Party { NorwegianSocialSecurityNumber = "08075412345" };

            // Act
            var result = await service.SendReminder(accreditation, GetServiceContext());

            // Assert
            Assert.IsTrue(result.All(r => r.Success));
            var order = JsonConvert.DeserializeObject<NotificationOrderChainRequest>(capturedBody!);
            Assert.IsNotNull(order!.Recipient.RecipientPerson);
            Assert.IsNull(order.Recipient.RecipientOrganization);
            Assert.AreEqual("08075412345", order.Recipient.RecipientPerson!.NationalIdentityNumber);
            Assert.AreEqual(NotificationChannel.EmailAndSms, order.Recipient.RecipientPerson.ChannelSchema);
            Assert.AreEqual("urn:altinn:resource:digdir-data-altinn-no-melding", order.Recipient.RecipientPerson.ResourceId);
            Assert.AreEqual("read", order.Recipient.RecipientPerson.ResourceAction);
        }

        [TestMethod]
        public async Task SendReminder_ApiFailure_ReturnsFailedReceipts()
        {
            // Arrange
            var httpClient = TestHelpers.GetHttpClientMock(_ =>
                new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") });
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.Altinn3NotificationsHttpClient)).Returns(httpClient);

            var service = CreateService();

            // Act
            var result = await service.SendReminder(GetAccreditation(), GetServiceContext());

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(r => !r.Success));
            Assert.IsTrue(result.All(r => r.RecipientCount == 0));
        }

        [TestMethod]
        public async Task GetOrderStatus_NotFound_ReturnsNull()
        {
            // Arrange
            var httpClient = TestHelpers.GetHttpClientMock(_ =>
                new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") });
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.Altinn3NotificationsHttpClient)).Returns(httpClient);

            var service = CreateService();

            // Act
            var result = await service.GetOrderStatus(Guid.NewGuid());

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task CreateInstantSmsOrder_PostsToInstantSmsEndpoint()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var httpClient = TestHelpers.GetHttpClientMock(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent($"{{\"notificationOrderId\":\"{Guid.NewGuid()}\"}}")
                };
            });
            A.CallTo(() => _mockHttpClientFactory.CreateClient(Constants.Altinn3NotificationsHttpClient)).Returns(httpClient);

            var service = CreateService();
            var order = new InstantSmsNotificationOrderRequest
            {
                IdempotencyId = Guid.NewGuid().ToString(),
                RecipientSms = new ShortMessageDeliveryDetails
                {
                    PhoneNumber = "+4799999999",
                    TimeToLiveInSeconds = 3600,
                    ShortMessageContent = new ShortMessageContent { Body = "hi" }
                }
            };

            // Act
            await service.CreateInstantSmsOrder(order);

            // Assert
            Assert.IsNotNull(capturedRequest);
            StringAssert.EndsWith(capturedRequest!.RequestUri!.AbsoluteUri, "/notifications/api/v1/future/orders/instant/sms");
        }

        private Altinn3NotificationsService CreateService()
        {
            return new Altinn3NotificationsService(
                _mockHttpClientFactory,
                _mockTokenRequesterService,
                _mockEntityRegistryService,
                _mockLogger);
        }

        private static ServiceContext GetServiceContext()
        {
            return new ServiceContext
            {
                Id = "ebevis-product",
                Name = "eBevis",
                ValidLanguages = new List<string> { Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>(),
                ServiceContextTextTemplate = new EBevisServiceContextTextTemplate()
            };
        }

        private static Accreditation GetAccreditation()
        {
            return new Accreditation
            {
                AccreditationId = Guid.NewGuid().ToString(),
                Subject = "910402021",
                Requestor = "958935420",
                SubjectParty = new Party { NorwegianOrganizationNumber = "910402021" },
                RequestorParty = new Party { NorwegianOrganizationNumber = "958935420" },
                AuthorizationCode = "",
                ValidTo = DateTime.Now.AddDays(1),
                ConsentReference = "2019-2312",
                ExternalReference = "externalreference",
                LanguageCode = Constants.LANGUAGE_CODE_NORWEGIAN_NB,
                EvidenceCodes = new List<EvidenceCode>
                {
                    new EvidenceCode
                    {
                        EvidenceCodeName = "TestEvidenceCode",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new ConsentRequirement { AltinnResource = "test-consent-resource" }
                        }
                    }
                }
            };
        }
    }
}
