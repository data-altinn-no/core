using System.Diagnostics.CodeAnalysis;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using Dan.Core.UnitTest.Settings;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class EvidenceHarvesterServiceTest
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        private readonly IHttpClientFactory _mockHttpClientFactory = A.Fake<IHttpClientFactory>();
        private readonly IConsentService _mockConsentService = A.Fake<IConsentService>();
        private readonly IEvidenceStatusService _mockEvidenceStatusService = A.Fake<IEvidenceStatusService>();
        private readonly ITokenRequesterService _mockTokenRequesterService = A.Fake<ITokenRequesterService>();
        private readonly IRequestContextService _mockRequestContextService = A.Fake<IRequestContextService>();
        private readonly IAvailableEvidenceCodesService _mockAvailableEvidenceCodesService = A.Fake<IAvailableEvidenceCodesService>();

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
            A.CallTo(() => _mockHttpClientFactory.CreateClient(A<string>._)).Returns(TestHelpers.GetHttpClientMock(MOCK_HTTP_CLIENT_RESPONSE_BODY));
            A.CallTo(() => _mockTokenRequesterService.GetMaskinportenToken(A<string>._, A<string>._))
                .Returns(Task.FromResult("{\"access_token\":\"\"}"));
        }

        [TestMethod]
        public async Task Harvest_Success_Open()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Available
                    }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);
            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var response = await evidenceHarvesterService.Harvest(EVIDENCECODE_OPEN, accreditation);

            Assert.AreEqual((int)StatusCodeId.Available, response.EvidenceStatus.Status.Code);
            Assert.IsNotNull(response.EvidenceValues);
        }

        [TestMethod]
        public async Task Harvest_Success_Consent()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Available
                    }
                )
            );
            A.CallTo(() => _mockConsentService.GetJwt(A<Accreditation>._)).Returns(Task.FromResult("somejwt"));

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);
            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var response = await evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation);

            Assert.AreEqual((int)StatusCodeId.Available, response.EvidenceStatus.Status.Code);
            Assert.IsNotNull(response.EvidenceValues);
        }

        [TestMethod]
        public void Harvest_Failure_ConsentRequestPending()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.PendingConsent
                    }
                )
            );
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, null);

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var actual =  Assert.ThrowsAsync<RequiresConsentException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation));
            StringAssert.Contains(actual.Result.Message, "pending a reply to the consent request");
        }

        [TestMethod]
        public void Harvest_Failure_ConsentDenied()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Denied
                    }
                )
            );
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, null, CONSENT_DENIED);

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var actual = Assert.ThrowsAsync<RequiresConsentException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation));
            StringAssert.Contains(actual.Result.Message, "evidence code has been denied or revoked");
        }

        [TestMethod]
        public void Harvest_Failure_ConsentExpired()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Expired
                    }
                )
            );
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var actual = Assert.ThrowsAsync<RequiresConsentException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_CONSENT, accreditation));
            StringAssert.Contains(actual.Result.Message, "evidence code has expired");
        }

        [TestMethod]
        public void Harvest_Failure_AsyncWaiting()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Waiting
                    }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var actual = Assert.ThrowsAsync<AsyncEvidenceStillWaitingException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_ASYNC, accreditation));
            StringAssert.Contains(actual.Result.Message, "The data for the requested evidence is not yet available");
        }

        [TestMethod]
        public void Harvest_Failure_MissingScope()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                    {
                        Status = EvidenceStatusCode.Available
                    }
                )
            );

            A.CallTo(() => _mockTokenRequesterService.GetMaskinportenToken(A<string>._, A<string>._))
                .Returns(Task.FromResult(""));

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG);

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var actual = Assert.ThrowsAsync<ServiceNotAvailableException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_OPEN, accreditation));
            StringAssert.Contains(actual.Result.Message, "unable to retrieve authentication token");
        }

        [TestMethod]
        public void Harvest_Failure_AsyncWaitingWithRetry()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
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

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);

            var actual = Assert.ThrowsAsync<AsyncEvidenceStillWaitingException>(() => evidenceHarvesterService.Harvest(EVIDENCECODE_ASYNC, accreditation));
            StringAssert.Contains(actual.Result.Message, "The data for the requested evidence is not yet available");
        }

        [TestMethod]
        public async Task Harvest_Success_AsyncOpen()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
            .Returns(
                Task.FromResult(new EvidenceStatus()
                {
                    Status = EvidenceStatusCode.Available
                }
                )
            );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);
            var response = await evidenceHarvesterService.Harvest(EVIDENCECODE_ASYNC, accreditation);

            Assert.AreEqual((int)StatusCodeId.Available, response.EvidenceStatus.Status.Code);
            Assert.IsNotNull(response.EvidenceValues);
        }
        
        [TestMethod]
        public async Task Harvest_Success_Stream()
        {
            A.CallTo(() =>
                    _mockEvidenceStatusService.GetEvidenceStatusAsync(A<Accreditation>._, A<EvidenceCode>._, A<bool>._))
                .Returns(
                    Task.FromResult(new EvidenceStatus()
                        {
                            Status = EvidenceStatusCode.Available
                        }
                    )
                );

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));

            var evidenceHarvesterService = new EvidenceHarvesterService(
                _loggerFactory, 
                _mockHttpClientFactory, 
                _mockConsentService, 
                _mockEvidenceStatusService, 
                _mockTokenRequesterService, 
                _mockRequestContextService,
                _mockAvailableEvidenceCodesService);
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
