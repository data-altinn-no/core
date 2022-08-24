using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using Dan.Core.UnitTest.Settings;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ConsentRequirement = Nadobe.Common.Models.ConsentRequirement;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class EvidenceStatusServiceTest
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
        private readonly Mock<IConsentService> _mockConsentService = new();
        private readonly Mock<IAvailableEvidenceCodesService> _mockAvailableEvidenceCodesService = new();
        private readonly Mock<IRequestContextService> _mockRequestContextService = new();

        private const string CONSENT_DENIED = "denied";

        private const string EVIDENCECODE_OPEN = "EvidenceCodeOpen";
        private const string EVIDENCECODE_LEGALBASIS = "EvidenceCodeRequiringLegalBasis";
        private const string EVIDENCECODE_CONSENT = "EvidenceCodeRequiringConsent";
        private const string EVIDENCECODE_CONSENT_WITH_REQUIREMENTS = "EvidenceCodeRequiringConsentWithRequirements";
        private const string EVIDENCECODE_LEGALBASISORCONSENT_WITH_LEGALBASIS = "EvidenceCodeRequiringLegalBasisOrConsentWithLegalBasis";
        private const string EVIDENCECODE_LEGALBASISORCONSENT_WITHOUT_LEGALBASIS = "EvidenceCodeRequiringLegalBasisOrConsentWithoutLegalBasis";
        private const string EVIDENCECODE_ASYNC = "EvidenceCodeAsync";

        [TestInitialize]
        public void Initialize()
        {
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(TestHelpers.GetHttpClientMock("[{}]"));
            _mockAvailableEvidenceCodesService.Setup(_ => _.GetAvailableEvidenceCodes(It.IsAny<bool>()))
                .Returns(Task.FromResult(GetAvailableEvidenceCodes()));
            _mockConsentService.Setup(_ => _.EvidenceCodeRequiresConsent(It.IsAny<EvidenceCode>()))
                .Returns(true);
            _mockConsentService.Setup(_ => _.Check(It.IsAny<Accreditation>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(ConsentStatus.Pending));
            _mockRequestContextService.SetupProperty(_ => _.Request,
                new Mock<HttpRequestData>(new Mock<FunctionContext>().Object).Object);
        }

        [TestMethod]
        public async Task Status_Open()
        {
            // Setup
            _mockConsentService.Setup(_ => _.EvidenceCodeRequiresConsent(It.IsAny<EvidenceCode>()))
                .Returns(false);

            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1));
            var evidenceHarvesterService = new EvidenceStatusService(_mockAvailableEvidenceCodesService.Object,
                _mockConsentService.Object, _mockRequestContextService.Object, _mockHttpClientFactory.Object, _loggerFactory);

            // Act
            var response = await evidenceHarvesterService.GetEvidenceStatusAsync(accreditation, new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_OPEN }, true);

            // Verify
            Assert.AreEqual((int)StatusCodeId.Available, response.Status.Code);
        }

        [TestMethod]
        public async Task Status_Pending_With_AccessMethod()
        {
            // Setup
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1), null, new List<string> { EVIDENCECODE_CONSENT });
            var evidenceHarvesterService = new EvidenceStatusService(_mockAvailableEvidenceCodesService.Object,
                _mockConsentService.Object, _mockRequestContextService.Object, _mockHttpClientFactory.Object, _loggerFactory);

            // Act
            var response = await evidenceHarvesterService.GetEvidenceStatusAsync(accreditation, new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_CONSENT }, true);

            // Verify
            Assert.AreEqual((int)StatusCodeId.PendingConsent, response.Status.Code);
        }

        [TestMethod]
        public async Task Status_Pending_With_Requirements()
        {
            // Setup
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1), null, new List<string> { EVIDENCECODE_CONSENT_WITH_REQUIREMENTS });
            var evidenceHarvesterService = new EvidenceStatusService(_mockAvailableEvidenceCodesService.Object,
                _mockConsentService.Object, _mockRequestContextService.Object, _mockHttpClientFactory.Object, _loggerFactory);

            // Act
            var response = await evidenceHarvesterService.GetEvidenceStatusAsync(accreditation, new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_CONSENT_WITH_REQUIREMENTS }, true);

            // Verify
            Assert.AreEqual((int)StatusCodeId.PendingConsent, response.Status.Code);
        }

        [TestMethod]
        public async Task Status_Unavailable()
        {
            // Setup
            Accreditation accreditation = MakeAccreditation("aid", Certificates.DEFAULT_ORG, DateTime.Now.AddDays(-1), null, new List<string> { "nolongeravailable" });
            var evidenceHarvesterService = new EvidenceStatusService(_mockAvailableEvidenceCodesService.Object,
                _mockConsentService.Object, _mockRequestContextService.Object, _mockHttpClientFactory.Object, _loggerFactory);

            // Act
            var response = await evidenceHarvesterService.GetEvidenceStatusAsync(accreditation, new EvidenceCode() { EvidenceCodeName = "nolongeravailable" }, true);

            // Verify
            Assert.AreEqual((int)StatusCodeId.Unavailable, response.Status.Code);
        }

        private static Accreditation MakeAccreditation(string id, string org, DateTime? validTo = null, string authorizationCode = null, List<string> evidenceCodes = null, List<string> evidenceCodesWithVerifiedLegalBasis = null)
        {
            evidenceCodes ??= new List<string>()
            {
                EVIDENCECODE_OPEN,
                EVIDENCECODE_CONSENT,
                EVIDENCECODE_LEGALBASIS,
                EVIDENCECODE_LEGALBASISORCONSENT_WITH_LEGALBASIS
            };

            var accreditationEvidenceCodes =
                GetAvailableEvidenceCodes().Where(x => evidenceCodes.Contains(x.EvidenceCodeName)).ToList();

            return new Accreditation()
            {
                AccreditationId = id,
                Subject = "test",
                AuthorizationCode = authorizationCode ?? Guid.NewGuid().ToString(),
                EvidenceCodes = accreditationEvidenceCodes,
                Requestor = org,
                Owner = org,
                ValidTo = validTo ?? DateTime.Now.AddDays(10)
            };
        }

        private static List<EvidenceCode> GetAvailableEvidenceCodes(string provider = "unittest")
        {
            return new List<EvidenceCode>()
            {
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_OPEN, EvidenceSource = provider, RequiredScopes = "foo" },
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_LEGALBASIS, EvidenceSource = provider },
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_LEGALBASISORCONSENT_WITH_LEGALBASIS, EvidenceSource = provider },
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_LEGALBASISORCONSENT_WITHOUT_LEGALBASIS, EvidenceSource = provider, ServiceCode = "1", ServiceEditionCode = 1 },
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_CONSENT, EvidenceSource = provider, ServiceCode = "1", ServiceEditionCode = 1, },
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_CONSENT_WITH_REQUIREMENTS, EvidenceSource = provider, ServiceCode = "1", ServiceEditionCode = 1, AuthorizationRequirements = new List<Requirement> { new  ConsentRequirement() } },
                new EvidenceCode() { EvidenceCodeName = EVIDENCECODE_ASYNC, IsAsynchronous = true, EvidenceSource = "unittest" },
            };
        }
    }
}
