using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Helpers;
using Dan.Core.ServiceContextTexts;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using ConsentRequirement = Nadobe.Common.Models.ConsentRequirement;

namespace Dan.Core.UnitTest
{
    public class ConsentServiceTest
    {
        [TestClass]
        [ExcludeFromCodeCoverage]
        public class AuthorizationRequestValidatorServiceTest
        {
            private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();

            private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            private readonly Mock<Services.Interfaces.IEntityRegistryService> _mockEntityRegistryService =
                new Mock<Services.Interfaces.IEntityRegistryService>();

            private readonly Mock<IAvailableEvidenceCodesService> _mockAvailableEvidenceCodesService =
                new Mock<IAvailableEvidenceCodesService>();

            private readonly Mock<IConsentService> _mockConsentService = new Mock<IConsentService>();

            private readonly Mock<IEvidenceStatusService> _mockEvidenceStatusService =
                new Mock<IEvidenceStatusService>();

            private readonly Mock<ITokenRequesterService> _mockTokenRequesterService =
                new Mock<ITokenRequesterService>();

            private readonly Mock<IRequirementValidationService> _mockRequirementValidatorService =
                new Mock<IRequirementValidationService>();

            private readonly Mock<IServiceContextService> _mockServiceContextService =
                new Mock<IServiceContextService>();

            private readonly Mock<IRequestContextService> _mockRequestContextService =
                new Mock<IRequestContextService>();

            private Accreditation accreditation;

            private const string EVIDENCECODE_OPEN = "EvidenceCodeOpen";
            private const string EVIDENCECODE_REQUIRING_CONSENT = "EvidenceRequiringConsent";
            private const string EVIDENCECODE_FAILURE_ACTION_SKIP = "EvidenceCodeFailureActionSkip";

            private const string EVIDENCEPARAM_NUMBER = "numberparam";
            private const string EVIDENCEPARAM_STRING = "stringparam";
            private const string EVIDENCEPARAM_DATETIME = "datetimeparam";
            private const string EVIDENCEPARAM_BOOLEAN = "booleanparam";
            private const string EVIDENCEPARAM_ATTACHMENT = "attachmentparam";

            [TestInitialize]
            public void Initialize()
            {

                accreditation = GetAccreditation();

                _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>()))
                    .Returns(TestHelpers.GetHttpClientMock("[{}]"));

                _mockTokenRequesterService.Setup(_ => _.GetMaskinportenToken(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult("{\"access_token\":\"\"}"));

                _mockEntityRegistryService.Setup(_ => _.Get(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(Task.FromResult(GetBrEntry()));

                _mockAvailableEvidenceCodesService.Setup(_ => _.GetAvailableEvidenceCodes(It.IsAny<bool>()))
                    .Returns(Task.FromResult(GetAvailableEvidenceCodes()));

                _mockRequirementValidatorService.Setup(_ => _.ValidateRequirements(
                        It.IsAny<Dictionary<string, List<Requirement>>>(), It.IsAny<AuthorizationRequest>()))
                    .Returns(Task.FromResult(new List<string>()));

                _mockRequirementValidatorService.Setup(_ => _.GetSkippedEvidenceCodes())
                    .Returns(new Dictionary<string, Requirement>()
                    {
                        { EVIDENCECODE_FAILURE_ACTION_SKIP, new AltinnRoleRequirement() }
                    });

                _mockRequestContextService.SetupAllProperties();
                _mockRequestContextService.SetupGet(_ => _.AuthenticatedOrgNumber).Returns("912345678");
                _mockRequestContextService.SetupGet(_ => _.Scopes).Returns(new List<string>() { "a", "b" });
                _mockRequestContextService.SetupGet(_ => _.ServiceContext).Returns(new ServiceContext()
                {
                    Id = "ebevis-product",
                    Name = "eBevis",
                    ValidLanguages = new List<string>() { Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                    AuthorizationRequirements = new List<Requirement>(),
                    ServiceContextTextTemplate = new EBevisServiceContextTextTemplate()
                });
            }
            /*
            [TestMethod]
            public async Task TestInitiateSuccess()
            {
                
            }

            public async Task TestInitiateSuccess()
            {
                
            }

            [TestMethod]
            public async Task TestInitiate()
            {

            }
            */

            [TestMethod]
            public void CheckConsentDelegationTexts()
            {
                var renderedTexts = TextTemplateProcessor.ProcessConsentRequestMacros(_mockRequestContextService.Object.ServiceContext.ServiceContextTextTemplate.ConsentDelegationContexts, accreditation, "REQUESTOR_NAME", "SUBJECT_NAME", "https://www.vg.no/consenturl");
                Assert.IsFalse(renderedTexts.En.Contains("#"));
                Assert.IsFalse(renderedTexts.NoNn.Contains("#"));
                Assert.IsFalse(renderedTexts.NoNb.Contains("#"));
            }

            [TestMethod]
            public void CheckAllTextReplacement()
            {
                var renderedTexts = TextTemplateProcessor.GetRenderedTexts(_mockRequestContextService.Object.ServiceContext, accreditation, "REQUESTOR_NAME", "SUBJECT_NAME", "https://www.vg.no/consenturl");

                Assert.IsFalse(renderedTexts.ConsentDelegationContexts.En.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentDelegationContexts.NoNn.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentDelegationContexts.NoNb.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentGivenReceiptText.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentButtonText.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentDeniedReceiptText.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentTitleText.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceBody.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceSender.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceSummary.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceTitle.Contains("#"));
                Assert.IsFalse(renderedTexts.EmailNotificationContent.Contains("#"));
                Assert.IsFalse(renderedTexts.SMSNotificationContent.Contains("#"));
                Assert.IsFalse(renderedTexts.EmailNotificationSubject.Contains("#"));
            }

            private List<EvidenceCode> GetAvailableEvidenceCodes()
            {
                return new List<EvidenceCode>
                {
                    new EvidenceCode
                    {
                        EvidenceCodeName = EVIDENCECODE_OPEN, BelongsToServiceContexts = new List<string> { "ebevis-product" }
                    },
                    new EvidenceCode
                    {
                        EvidenceCodeName = EVIDENCECODE_REQUIRING_CONSENT, BelongsToServiceContexts = new List<string> { "ebevis-product" }, AuthorizationRequirements = new List<Requirement>
                        {
                            new ConsentRequirement()
                        }
                    }
                };
            }

            private List<EvidenceParameter> GetParameterListForDefinition(bool withRequiredParams = false)
            {
                var paramList = GetParameterList(withRequiredParams);
                foreach (var evidenceParameter in paramList)
                {
                    evidenceParameter.Value = null;
                }

                return paramList;
            }

            private List<EvidenceParameter> GetParameterList(bool withRequiredParams = false)
            {
                return new List<EvidenceParameter>
                {
                    new EvidenceParameter
                    {
                        EvidenceParamName = EVIDENCEPARAM_BOOLEAN, Value = true, ParamType = EvidenceParamType.Boolean
                    },
                    new EvidenceParameter
                    {
                        EvidenceParamName = EVIDENCEPARAM_NUMBER, Value = 123.456, ParamType = EvidenceParamType.Number,
                        Required = withRequiredParams
                    },
                    new EvidenceParameter
                    {
                        EvidenceParamName = EVIDENCEPARAM_STRING, Value = "stringvalue",
                        ParamType = EvidenceParamType.String, Required = withRequiredParams
                    },
                    new EvidenceParameter
                    {
                        EvidenceParamName = EVIDENCEPARAM_DATETIME, Value = new DateTime(2018, 1, 1),
                        ParamType = EvidenceParamType.DateTime
                    },
                    new EvidenceParameter
                    {
                        EvidenceParamName = EVIDENCEPARAM_ATTACHMENT, Value = "somebase64",
                        ParamType = EvidenceParamType.Attachment
                    }
                };
            }

            private List<EvidenceParameter> GetParameterListForRequest()
            {
                var paramList = GetParameterList();
                foreach (var evidenceParameter in paramList)
                {
                    evidenceParameter.ParamType = null;
                    evidenceParameter.Required = null;
                }

                return paramList;
            }

            private SimpleEntityRegistryUnit GetBrEntry()
            {
                return new SimpleEntityRegistryUnit()
                {
                    IndustrialCodes = new List<string> { "1234" },
                    OrganizationForm = "STAT"
                };
            }

            private AuthorizationRequest GetAuthorizationRequest()
            {
                return new AuthorizationRequest()
                {
                    Requestor = "991825827",
                    Subject = "991825827",
                    ValidTo = DateTime.Now.AddDays(3),
                    EvidenceRequests = new List<EvidenceRequest>()
                    {
                        new EvidenceRequest()
                        {
                            EvidenceCodeName = EVIDENCECODE_OPEN
                        }
                    }
                };
            }

            private Accreditation GetAccreditation()
            {
                return new Accreditation
                {
                    AccreditationId = Guid.NewGuid().ToString(),
                    Subject = "910402021",
                    Requestor = "958935420",
                    AuthorizationCode = "",
                    ValidTo = DateTime.Now.AddDays(1),
                    ConsentReference = "2019-2312",
                    ExternalReference = "externalreference",
                    LanguageCode = Constants.LANGUAGE_CODE_NORWEGIAN_NB,
                    EvidenceCodes = new List<EvidenceCode>
                    {
                        new EvidenceCode
                        {
                            EvidenceCodeName = "test",
                            ServiceCode = "201701",
                            ServiceEditionCode = 4758,
                            ServiceContext = "eBevis"
                        }
                    }
                };
            }
        }
    }
}
