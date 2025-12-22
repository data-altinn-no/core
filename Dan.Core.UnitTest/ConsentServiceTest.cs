using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Helpers;
using Dan.Core.ServiceContextTexts;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using ConsentRequirement = Nadobe.Common.Models.ConsentRequirement;

namespace Dan.Core.UnitTest
{
    public class ConsentServiceTest
    {
        [TestClass]
        [ExcludeFromCodeCoverage]
        public class AuthorizationRequestValidatorServiceTest
        {
            private readonly IHttpClientFactory _mockHttpClientFactory = A.Fake<IHttpClientFactory>();

            private readonly IEntityRegistryService _mockEntityRegistryService =
                A.Fake<IEntityRegistryService>();

            private readonly IAvailableEvidenceCodesService _mockAvailableEvidenceCodesService =
                A.Fake<IAvailableEvidenceCodesService>();

            private readonly ITokenRequesterService _mockTokenRequesterService =
                A.Fake<ITokenRequesterService>();

            private readonly IRequirementValidationService _mockRequirementValidatorService =
                A.Fake<IRequirementValidationService>();
            

            private readonly IRequestContextService _mockRequestContextService =
                A.Fake<IRequestContextService>();

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

                A.CallTo(() => _mockHttpClientFactory.CreateClient(A<string>._))
                    .Returns(TestHelpers.GetHttpClientMock("[{}]"));

                A.CallTo(() => _mockTokenRequesterService.GetMaskinportenToken(A<string>._, A<string>._))
                    .Returns(Task.FromResult("{\"access_token\":\"\"}"));

                A.CallTo(() => _mockEntityRegistryService.Get(A<string>._, A<bool>._, A<bool>._, A<bool>._))
                    .Returns(Task.FromResult(GetBrEntry()));

                A.CallTo(() => _mockAvailableEvidenceCodesService.GetAvailableEvidenceCodes(A<bool>._))
                    .Returns(Task.FromResult(GetAvailableEvidenceCodes()));

                A.CallTo(() => _mockRequirementValidatorService.ValidateRequirements(
                        A<Dictionary<string, List<Requirement>>>._, A<AuthorizationRequest>._))
                    .Returns(Task.FromResult(new List<string>()));

                A.CallTo(() =>_mockRequirementValidatorService.GetSkippedEvidenceCodes())
                    .Returns(new Dictionary<string, Requirement>()
                    {
                        { EVIDENCECODE_FAILURE_ACTION_SKIP, new AltinnRoleRequirement() }
                    });

                A.CallTo(() => _mockRequestContextService.AuthenticatedOrgNumber).Returns("912345678");
                A.CallTo(() => _mockRequestContextService.Scopes).Returns(new List<string>() { "a", "b" });
                A.CallTo(() => _mockRequestContextService.ServiceContext).Returns(new ServiceContext()
                {
                    Id = "ebevis-product",
                    Name = "eBevis",
                    ValidLanguages = new List<string>() { Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                    AuthorizationRequirements = new List<Requirement>(),
                    ServiceContextTextTemplate = new EBevisServiceContextTextTemplate()
                });
            }

            [TestMethod]
            public void CheckConsentDelegationTexts()
            {
                var renderedTexts = TextTemplateProcessor.ProcessConsentRequestMacros(_mockRequestContextService.ServiceContext.ServiceContextTextTemplate.ConsentDelegationContexts, accreditation, "REQUESTOR_NAME", "SUBJECT_NAME", "https://www.vg.no/consenturl");
                Assert.IsFalse(renderedTexts.En.Contains("#"));
                Assert.IsFalse(renderedTexts.NoNn.Contains("#"));
                Assert.IsFalse(renderedTexts.NoNb.Contains("#"));
            }

            [TestMethod]
            public void CheckAllTextReplacement()
            {
                var renderedTexts = TextTemplateProcessor.GetRenderedTexts(_mockRequestContextService.ServiceContext, accreditation, "REQUESTOR_NAME", "SUBJECT_NAME", "https://www.vg.no/consenturl");

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
