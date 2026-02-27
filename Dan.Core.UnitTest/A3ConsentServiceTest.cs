using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Helpers;
using Dan.Core.ServiceContextTexts;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using Altinn.Dd.Correspondence.Models;
using Altinn.Dd.Correspondence.Services;
using ConsentRequirement = Nadobe.Common.Models.ConsentRequirement;
using CorrespondenceResult = Altinn.Dd.Correspondence.Models.CorrespondenceResult;

namespace Dan.Core.UnitTest
{
    public class A3ConsentServiceTest
    {
        [TestClass]
        [ExcludeFromCodeCoverage]
        public class Altinn3ConsentServiceTest
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

            private readonly IDdCorrespondenceService _mockDdCorrespondenceService =
                A.Fake<IDdCorrespondenceService>();

            private readonly IAltinnServiceOwnerApiService _mockAltinnServiceOwnerApiService =
                A.Fake<IAltinnServiceOwnerApiService>();

            private readonly ILogger<ConsentService> _mockLogger =
                A.Fake<ILogger<ConsentService>>();

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

                A.CallTo(() => _mockTokenRequesterService.GetMaskinportenConsentToken(A<string>._, A<string>._))
                    .Returns(Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJWYWxpZFRvRGF0ZSI6IjE3MDAwMDAwMDAiLCJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ."));

                A.CallTo(() => _mockEntityRegistryService.Get(A<string>._, A<bool>._, A<bool>._, A<bool>._))
                    .Returns(Task.FromResult(GetBrEntry()));

                A.CallTo(() => _mockEntityRegistryService.Get(A<string>._))
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
                var renderedTexts = TextTemplateProcessor.GetRenderedTexts(_mockRequestContextService.ServiceContext, accreditation, "REQUESTOR_NAME", "SUBJECT_NAME", "https://www.vg.no/consenturl", true);

                Assert.IsFalse(renderedTexts.ConsentDelegationContexts.En.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentDelegationContexts.NoNn.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentDelegationContexts.NoNb.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentGivenReceiptText.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentButtonText.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentDeniedReceiptText.Contains("#"));
                Assert.IsFalse(renderedTexts.ConsentTitleText.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceBodyA3.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceSender.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceSummary.Contains("#"));
                Assert.IsFalse(renderedTexts.CorrespondenceTitle.Contains("#"));
                Assert.IsFalse(renderedTexts.EmailNotificationContent.Contains("#"));
                Assert.IsFalse(renderedTexts.SMSNotificationContent.Contains("#"));
                Assert.IsFalse(renderedTexts.EmailNotificationSubject.Contains("#"));
            }

            [TestMethod]
            public async Task TestInitiateConsentRequest()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{\"id\":\"test-id-123\",\"viewUri\":\"https://altinn.no/consent/test\",\"status\":\"created\"}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                A.CallTo(() => _mockEntityRegistryService.Get(A<string>._))
                    .Returns(Task.FromResult(new SimpleEntityRegistryUnit { Name = "Test Organization" }));

                // Create a CorrespondenceResult using FormatterServices since it has no public constructors
                var correspondenceResultType = typeof(CorrespondenceResult);
                var successResult = (CorrespondenceResult)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(correspondenceResultType);
                
                // Use reflection to set the IsSuccess property to true
                var isSuccessProperty = correspondenceResultType.GetProperty("IsSuccess");
                if (isSuccessProperty != null && isSuccessProperty.CanWrite)
                {
                    isSuccessProperty.SetValue(successResult, true);
                }
                else
                {
                    // If property is not writable, try to find and set the backing field
                    var isSuccessField = correspondenceResultType.GetField("<IsSuccess>k__BackingField", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (isSuccessField != null)
                    {
                        isSuccessField.SetValue(successResult, true);
                    }
                }

                A.CallTo(() => _mockDdCorrespondenceService.SendCorrespondence(A<DdCorrespondenceDetails>._))
                    .Returns(Task.FromResult(successResult));

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();

                // Act
                await consentService.Initiate(testAccreditation, skipAltinnNotification: false);

                // Assert
                Assert.IsNotNull(testAccreditation.Altinn3ConsentId);
                Assert.IsNotNull(testAccreditation.AltinnConsentUrl);
                Assert.AreEqual("test-id-123", testAccreditation.Altinn3ConsentId);
                Assert.AreEqual("https://altinn.no/consent/test", testAccreditation.AltinnConsentUrl);

                // Verify that correspondence was sent
                A.CallTo(() => _mockDdCorrespondenceService.SendCorrespondence(A<DdCorrespondenceDetails>._))
                    .MustHaveHappenedOnceExactly();
            }

            [TestMethod]
            public async Task TestInitiateConsentRequest_SkipNotification()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{\"id\":\"test-id-456\",\"viewUri\":\"https://altinn.no/consent/test2\",\"status\":\"created\"}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                A.CallTo(() => _mockEntityRegistryService.Get(A<string>._))
                    .Returns(Task.FromResult(new SimpleEntityRegistryUnit { Name = "Test Organization" }));

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();

                // Act
                await consentService.Initiate(testAccreditation, skipAltinnNotification: true);

                // Assert
                Assert.IsNotNull(testAccreditation.Altinn3ConsentId);
                Assert.IsNotNull(testAccreditation.AltinnConsentUrl);

                // Verify that correspondence was NOT sent
                A.CallTo(() => _mockDdCorrespondenceService.SendCorrespondence(A<DdCorrespondenceDetails>._))
                    .MustNotHaveHappened();
            }

            [TestMethod]
            public async Task TestInitiateConsentRequest_ThrowsWhenNoConsentRequired()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.EvidenceCodes = new List<EvidenceCode>
                {
                    new EvidenceCode
                    {
                        EvidenceCodeName = "test-no-consent",
                        AuthorizationRequirements = new List<Requirement>()
                    }
                };

                // Act & Assert - should throw ArgumentException
                var exception = await Assert.ThrowsAsync<ArgumentException>(
                    async () => await consentService.Initiate(testAccreditation, skipAltinnNotification: false));
                
                Assert.IsNotNull(exception);
            }

            [TestMethod]
            public async Task TestCheck_ReturnsPending_WhenAltinn3ConsentStatusIsNull()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.Altinn3ConsentStatus = null;

                // Act
                var result = await consentService.Check(testAccreditation, onlyLocalCheck: true);

                // Assert
                Assert.AreEqual(ConsentStatus.Pending, result);
            }

            [TestMethod]
            public async Task TestCheck_ReturnsDenied_WhenAltinn3ConsentStatusIsDenied()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.Altinn3ConsentStatus = "denied";

                // Act
                var result = await consentService.Check(testAccreditation, onlyLocalCheck: true);

                // Assert
                Assert.AreEqual(ConsentStatus.Denied, result);
            }

            [TestMethod]
            public async Task TestCheck_ReturnsExpired_WhenValidToIsInPast()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.Altinn3ConsentStatus = "granted";
                testAccreditation.ValidTo = DateTime.Now.AddDays(-1);

                // Act
                var result = await consentService.Check(testAccreditation, onlyLocalCheck: true);

                // Assert
                Assert.AreEqual(ConsentStatus.Expired, result);
            }

            [TestMethod]
            public async Task TestCheck_ReturnsGranted_WithOnlyLocalCheck()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.Altinn3ConsentStatus = "granted";
                testAccreditation.ValidTo = DateTime.Now.AddDays(1);

                // Act
                var result = await consentService.Check(testAccreditation, onlyLocalCheck: true);

                // Assert
                Assert.AreEqual(ConsentStatus.Granted, result);
            }

            [TestMethod]
            public async Task TestGetJwt_ReturnsToken_WhenSuccessful()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");
                
                var expectedToken = "test-jwt-token-123";
                A.CallTo(() => _mockTokenRequesterService.GetMaskinportenConsentToken(A<string>._, A<string>._))
                    .Returns(Task.FromResult(expectedToken));

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.Altinn3ConsentId = "consent-id-456";

                // Act
                var result = await consentService.GetJwt(testAccreditation);

                // Assert
                Assert.AreEqual(expectedToken, result);
                A.CallTo(() => _mockTokenRequesterService.GetMaskinportenConsentToken("consent-id-456", "910402021"))
                    .MustHaveHappenedOnceExactly();
            }

            [TestMethod]
            public async Task TestGetJwt_ThrowsRequiresConsentException_WhenConsentIdIsMissing()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.Altinn3ConsentId = null;

                // Act & Assert
                var exception = await Assert.ThrowsAsync<RequiresConsentException>(
                    async () => await consentService.GetJwt(testAccreditation));
                
                Assert.IsNotNull(exception);
            }

            [TestMethod]
            public void TestEvidenceCodeRequiresConsent_ReturnsTrue_WhenConsentRequirementExists()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var evidenceCode = new EvidenceCode
                {
                    EvidenceCodeName = "test",
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new ConsentRequirement
                        {
                            AltinnResource = "test-resource",
                            AppliesToServiceContext = new List<string>()
                        }
                    }
                };

                // Act
                var result = consentService.EvidenceCodeRequiresConsent(evidenceCode);

                // Assert
                Assert.IsTrue(result);
            }

            [TestMethod]
            public void TestEvidenceCodeRequiresConsent_ReturnsFalse_WhenNoConsentRequirement()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var evidenceCode = new EvidenceCode
                {
                    EvidenceCodeName = "test",
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new AltinnRoleRequirement()
                    }
                };

                // Act
                var result = consentService.EvidenceCodeRequiresConsent(evidenceCode);

                // Assert
                Assert.IsFalse(result);
            }

            [TestMethod]
            public void TestEvidenceCodeRequiresConsent_ReturnsFalse_WhenConsentRequirementDoesNotApplyToServiceContext()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var evidenceCode = new EvidenceCode
                {
                    EvidenceCodeName = "test",
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new ConsentRequirement
                        {
                            AltinnResource = "test-resource",
                            AppliesToServiceContext = new List<string> { "other-context" }
                        }
                    }
                };

                // Act
                var result = consentService.EvidenceCodeRequiresConsent(evidenceCode);

                // Assert
                Assert.IsFalse(result);
            }

            [TestMethod]
            public void TestGetEvidenceCodesRequiringConsentForActiveContext_FiltersCorrectly()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                testAccreditation.EvidenceCodes = new List<EvidenceCode>
                {
                    new EvidenceCode
                    {
                        EvidenceCodeName = "requires-consent",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new ConsentRequirement
                            {
                                AltinnResource = "test-resource",
                                AppliesToServiceContext = new List<string>()
                            }
                        }
                    },
                    new EvidenceCode
                    {
                        EvidenceCodeName = "no-consent",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new AltinnRoleRequirement()
                        }
                    }
                };

                // Act
                var result = consentService.GetEvidenceCodesRequiringConsentForActiveContext(testAccreditation);

                // Assert
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("requires-consent", result[0].EvidenceCodeName);
            }

            [TestMethod]
            public async Task TestLogUse_ReturnsTrue()
            {
                // Arrange
                var httpClient = TestHelpers.GetHttpClientMock("{}");
                var noCertHttpClient = TestHelpers.GetHttpClientMock("[{}]");

                var consentService = new Altinn3ConsentService(
                    httpClient,
                    noCertHttpClient,
                    _mockLogger,
                    _mockDdCorrespondenceService,
                    _mockEntityRegistryService,
                    _mockAltinnServiceOwnerApiService,
                    _mockRequestContextService,
                    _mockTokenRequesterService);

                var testAccreditation = GetAccreditation();
                var evidenceCode = new EvidenceCode { EvidenceCodeName = "test" };

                // Act
                var result = await consentService.LogUse(testAccreditation, evidenceCode, DateTime.Now);

                // Assert
                Assert.IsTrue(result);
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
                            {
                                AltinnResource = "test-resource"
                            }
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
                    SubjectParty = new Party { NorwegianOrganizationNumber = "910402021" },
                    RequestorParty = new Party { NorwegianOrganizationNumber = "958935420" },
                    AuthorizationCode = "",
                    ValidTo = DateTime.Now.AddDays(1),
                    ConsentReference = "2019-2312",
                    ExternalReference = "externalreference",
                    LanguageCode = Constants.LANGUAGE_CODE_NORWEGIAN_NB,
                    Altinn3ConsentId = "test-consent-id-123",
                    EvidenceCodes = new List<EvidenceCode>
                    {
                        new EvidenceCode
                        {
                            EvidenceCodeName = "test",
                            ServiceCode = "201701",
                            ServiceEditionCode = 4758,
                            ServiceContext = "eBevis",
                            AuthorizationRequirements = new List<Requirement>
                            {
                                new ConsentRequirement()
                                {
                                    AltinnResource = "test-resource",
                                    ServiceCode = "201701",
                                    ServiceEdition = 4758,
                                    RequiresSrr = false
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}
