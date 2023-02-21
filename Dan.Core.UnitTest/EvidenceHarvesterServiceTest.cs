using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using Dan.Core.UnitTest.Settings;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class EvidenceHarvesterServiceTest
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
        private readonly Mock<IConsentService> _mockConsentService = new();
        private readonly Mock<IEvidenceStatusService> _mockEvidenceStatusService = new();
        private readonly Mock<ITokenRequesterService> _mockTokenRequesterService = new();
        private readonly Mock<IRequestContextService> _mockRequestContextService = new();

        private const string CONSENT_DENIED = "denied";

        private const string EVIDENCECODE_OPEN = "EvidenceCodeOpen";
        private const string EVIDENCECODE_LEGALBASIS = "EvidenceCodeRequiringLegalBasis";
        private const string EVIDENCECODE_CONSENT = "EvidenceCodeRequiringConsent";
        private const string EVIDENCECODE_LEGALBASISORCONSENT_WITH_LEGALBASIS = "EvidenceCodeRequiringLegalBasisOrConsentWithLegalBasis";
        private const string EVIDENCECODE_ASYNC = "EvidenceCodeAsync";
        private const string EVIDENCECODE_SCOPE = "EvidenceCodeScope";
        private const string EVIDENCECODE_STREAM = "EvidenceCodeStream";

        private const string MOCK_HTTP_CLIENT_RESPONSE_BODY = "[{}]";

        [TestInitialize]
        public void Initialize()
        {
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(TestHelpers.GetHttpClientMock(MOCK_HTTP_CLIENT_RESPONSE_BODY));
            _mockTokenRequesterService.Setup(_ => _.GetMaskinportenToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult("{\"access_token\":\"\"}"));
            _mockRequestContextService.SetupProperty(_ => _.Request,
                new Mock<HttpRequestData>(new Mock<FunctionContext>().Object).Object);
        }

        [TestMethod]
        public async Task Harvest_Success_Open()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Available
                    }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);
            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var response = await evidenceHarvesterService.Harvest(EVIDENCECODE_OPEN, accreditation);

            Assert.AreEqual((int)StatusCodeId.Available, response.EvidenceStatus.Status.Code);
            Assert.IsNotNull(response.EvidenceValues);
        }

        [TestMethod]
        public async Task Harvest_Success_Consent()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Available
                    }
                )
            );
            _mockConsentService.Setup(_ => _.GetJwt(It.IsAny<Accreditation>())).Returns(Task.FromResult("somejwt"));

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);
            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var response = await evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation);

            Assert.AreEqual((int)StatusCodeId.Available, response.EvidenceStatus.Status.Code);
            Assert.IsNotNull(response.EvidenceValues);
        }

        [TestMethod]
        public void Harvest_Failure_ConsentRequestPending()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.PendingConsent
                    }
                )
            );
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, null);

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var actual = Assert.ThrowsExceptionAsync<RequiresConsentException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation));
            StringAssert.Contains(actual.Result.Message, "pending a reply to the consent request");
        }

        [TestMethod]
        public void Harvest_Failure_ConsentDenied()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Denied
                    }
                )
            );
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, null, CONSENT_DENIED);

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var actual = Assert.ThrowsExceptionAsync<RequiresConsentException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation));
            StringAssert.Contains(actual.Result.Message, "evidence code has been denied or revoked");
        }

        [TestMethod]
        public void Harvest_Failure_ConsentExpired()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Expired
                    }
                )
            );
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var actual = Assert.ThrowsExceptionAsync<RequiresConsentException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation));
            StringAssert.Contains(actual.Result.Message, "evidence code has expired");
        }

        [TestMethod]
        public void Harvest_Failure_AsyncWaiting()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Waiting
                    }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var actual = Assert.ThrowsExceptionAsync<AsyncEvidenceStillWaitingException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_ASYNC, accreditation));
            StringAssert.Contains(actual.Result.Message, "The data for the requested evidence is not yet available");
        }

        [TestMethod]
        public void Harvest_Failure_MissingScope()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Available
                    }
                )
            );

            _mockTokenRequesterService.Setup(_ => _.GetMaskinportenToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(""));

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var actual = Assert.ThrowsExceptionAsync<ServiceNotAvailableException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_OPEN, accreditation));
            StringAssert.Contains(actual.Result.Message, "unable to retrieve authentication token");
        }

        [TestMethod]
        public void Harvest_Failure_AsyncWaitingWithRetry()
        {
            _mockEvidenceStatusService.Setup(_ =>
                _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = new EvidenceStatusCode()
                        {
                            Code = (int)StatusCodeId.Waiting,
                            RetryAt = DateTime.Now
                        }
                    }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);

            var actual = Assert.ThrowsExceptionAsync<AsyncEvidenceStillWaitingException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_ASYNC, accreditation));
            StringAssert.Contains(actual.Result.Message, "The data for the requested evidence is not yet available");
        }

        [TestMethod]
        public async Task Harvest_Success_AsyncOpen()
        {
            _mockEvidenceStatusService.Setup(_ =>
            _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
            .Returns(
                Task.FromResult(new EvidenceStatus()
                {
                    Status = EvidenceStatusCode.Available
                }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);
            var response = await evidenceHarvesterService.Harvest(EVIDENCECODE_ASYNC, accreditation);

            Assert.AreEqual((int)StatusCodeId.Available, response.EvidenceStatus.Status.Code);
            Assert.IsNotNull(response.EvidenceValues);
        }
        
        [TestMethod]
        public async Task Harvest_Success_Stream()
        {
            _mockEvidenceStatusService.Setup(_ =>
                    _.GetEvidenceStatusAsync(It.IsAny<Accreditation>(), It.IsAny<EvidenceCode>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                        {
                            Status = EvidenceStatusCode.Available
                        }
                    )
                );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(_loggerFactory, _mockHttpClientFactory.Object, _mockConsentService.Object, _mockEvidenceStatusService.Object, _mockTokenRequesterService.Object, _mockRequestContextService.Object);
            var response = await evidenceHarvesterService.HarvestStream(EVIDENCECODE_STREAM, accreditation);

            var sr = new StreamReader(response);
            var responseString = await sr.ReadToEndAsync();
            
            Assert.AreEqual(responseString, MOCK_HTTP_CLIENT_RESPONSE_BODY);
        }

        private static Accreditation MakeAccreditation(string id, string org, DateTime? validTo = null, string authorizationCode = null, List<string> evidenceCodes = null)
        {
            if (evidenceCodes == null)
            {
                evidenceCodes = new List<string>() {
                    EVIDENCECODE_LEGALBASIS,
                    EVIDENCECODE_LEGALBASISORCONSENT_WITH_LEGALBASIS
                };
            }

            return new Accreditation()
            {
                AccreditationId = id,
                Subject = "test",
                AuthorizationCode = authorizationCode ?? Guid.NewGuid().ToString(),
                EvidenceCodes = GetEvidenceCodes(),
                Requestor = org,
                Owner = org,
                ValidTo = validTo ?? DateTime.Now.AddDays(10)
            };
        }

        private static List<EvidenceCode> GetEvidenceCodes(string provider = "unittest")
        {
            return new List<EvidenceCode>
            {
                new() { EvidenceCodeName = EVIDENCECODE_OPEN, EvidenceSource = provider, RequiredScopes = "foo" },
                new() { EvidenceCodeName = EVIDENCECODE_LEGALBASIS, EvidenceSource = provider },
                new() { EvidenceCodeName = EVIDENCECODE_CONSENT, EvidenceSource = provider, ServiceCode = "1", ServiceEditionCode = 1, },
                new() { EvidenceCodeName = EVIDENCECODE_ASYNC, IsAsynchronous = true, EvidenceSource = "unittest" },
                new() { EvidenceCodeName = EVIDENCECODE_SCOPE, EvidenceSource = "unittest", RequiredScopes = "foo"},
                new() { EvidenceCodeName = EVIDENCECODE_STREAM, EvidenceSource = "unittest", Values = new List<EvidenceValue> { new() { EvidenceValueName = "streamtest", ValueType = EvidenceValueType.Binary }}},
            };
        }

    }
}
