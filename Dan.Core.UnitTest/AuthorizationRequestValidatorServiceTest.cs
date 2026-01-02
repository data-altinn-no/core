using System.Diagnostics.CodeAnalysis;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class AuthorizationRequestValidatorServiceTest
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        private readonly IHttpClientFactory _mockHttpClientFactory = A.Fake<IHttpClientFactory>();
        private readonly IEntityRegistryService _mockEntityRegistryService = A.Fake<IEntityRegistryService>();
        private readonly IAvailableEvidenceCodesService _mockAvailableEvidenceCodesService = A.Fake<IAvailableEvidenceCodesService>();
        private readonly ITokenRequesterService _mockTokenRequesterService = A.Fake<ITokenRequesterService>();
        private readonly IRequirementValidationService _mockRequirementValidatorService = A.Fake<IRequirementValidationService>();
        private readonly IRequestContextService _mockRequestContextService = A.Fake<IRequestContextService>();

        private const string EVIDENCECODE_OPEN = "EvidenceCodeOpen";
        private const string EVIDENCECODE_REQUIRING_LEGALBASIS_1 = "EvidenceCodeRequiringLegalBasis1";
        private const string EVIDENCECODE_REQUIRING_LEGALBASIS_2 = "EvidenceCodeRequiringLegalBasis2";
        private const string EVIDENCECODE_REQUIRING_LEGALBASIS_3 = "EvidenceCodeRequiringLegalBasis3";
        private const string EVIDENCECODE_REQUIRING_CONSENT = "EvidenceRequiringConsent";
        private const string EVIDENCECODE_ASYNC_OPEN = "EvidenceAsyncOpen";
        private const string EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_1 = "EvidenceAsyncOpenFailInit1";
        private const string EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_2 = "EvidenceAsyncOpenFailInit2";
        private const string EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_3 = "EvidenceAsyncOpenFailInit3";
        private const string EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_4 = "EvidenceAsyncOpenFailInit4";
        private const string EVIDENCECODE_PARAMS_OPTIONAL_1 = "EvidenceParamsOptional1";
        private const string EVIDENCECODE_PARAMS_OPTIONAL_2 = "EvidenceParamsOptional2";
        private const string EVIDENCECODE_PARAMS_OPTIONAL_3 = "EvidenceParamsOptional3";
        private const string EVIDENCECODE_PARAMS_REQUIRED_1 = "EvidenceParamsRequired1";
        private const string EVIDENCECODE_PARAMS_REQUIRED_2 = "EvidenceParamsRequired2";
        private const string EVIDENCECODE_MAX_VALID_DAYS_1 = "EvidenceMaxValidDays1";
        private const string EVIDENCECODE_MAX_VALID_DAYS_2 = "EvidenceMaxValidDays2";
        private const string EVIDENCECODE_FAILURE_ACTION_SKIP = "EvidenceCodeFailureActionSkip";

        private const string EVIDENCEPARAM_NUMBER = "numberparam";
        private const string EVIDENCEPARAM_STRING = "stringparam";
        private const string EVIDENCEPARAM_DATETIME = "datetimeparam";
        private const string EVIDENCEPARAM_BOOLEAN = "booleanparam";
        private const string EVIDENCEPARAM_ATTACHMENT = "attachmentparam";

        [TestInitialize]
        public void Initialize()
        {
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

            A.CallTo(() => _mockRequirementValidatorService.GetSkippedEvidenceCodes())
                .Returns(new Dictionary<string, Requirement>()
                {
                    { EVIDENCECODE_FAILURE_ACTION_SKIP, new AltinnRoleRequirement() }
                });

            _mockRequestContextService.AuthenticatedOrgNumber = "912345678";
            _mockRequestContextService.Scopes = new List<string> {"a", "b"};
            _mockRequestContextService.ServiceContext = new ServiceContext { Id = "ebevis-product", Name = "eBevis", ValidLanguages = new List<string>() { "no" }, AuthorizationRequirements = [] };
        }

        [TestMethod]
        public async Task ValidateTestSuccess()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            // Act
            await arvs.Validate(GetAuthorizationRequest());

            // Verify
            Assert.IsNotNull(arvs.GetAuthorizationRequest());
            Assert.IsNotNull(arvs.GetAuthorizationRequest().RequestorParty);
            Assert.IsNotNull(arvs.GetAuthorizationRequest().SubjectParty);
            Assert.IsEmpty(arvs.GetEvidenceCodeNamesWithVerifiedLegalBasis());
            Assert.IsTrue(arvs.GetValidTo() > DateTime.Now);
        }

        [TestMethod]
        public async Task ValidateTestSuccessWithUpisSubjectAndRequestor()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            // Act
            await arvs.Validate(GetAuthorizationRequest(subject: "iso6523-actorid-upis::0192:991825827", requestor: "iso6523-actorid-upis::9999:blabla-123"));

            // Verify
            Assert.IsNotNull(arvs.GetAuthorizationRequest());
            Assert.IsNotNull(arvs.GetAuthorizationRequest().RequestorParty);
            Assert.IsNotNull(arvs.GetAuthorizationRequest().SubjectParty);
            Assert.IsEmpty(arvs.GetEvidenceCodeNamesWithVerifiedLegalBasis());
            Assert.IsTrue(arvs.GetValidTo() > DateTime.Now);
        }

        [TestMethod]
       
        public async Task ValidateTestFailureWithInvalidSubjectOrgNo()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            // Act
            await Assert.ThrowsAsync<InvalidSubjectException>(async () =>
            {
                await arvs.Validate(GetAuthorizationRequest(subject: "123456789"));
            });
        }
           

        [TestMethod]
        public async Task ValidateTestFailureWithInvalidRequestorOrgNo()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            // Act
            await Assert.ThrowsAsync<InvalidRequestorException>(async () =>
            {
                await arvs.Validate(GetAuthorizationRequest(requestor: "123456789"));
            });
        }

        [TestMethod]
        public async Task ValidateTestSuccessWithSsnSubjectAndRequestor()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            // Act
            await arvs.Validate(GetAuthorizationRequest(subject: "03065001488", requestor: "03065001488"));

            // Verify
            Assert.IsNotNull(arvs.GetAuthorizationRequest());
            Assert.IsNotNull(arvs.GetAuthorizationRequest().RequestorParty);
            Assert.IsNotNull(arvs.GetAuthorizationRequest().SubjectParty);
            Assert.IsEmpty(arvs.GetEvidenceCodeNamesWithVerifiedLegalBasis());
            Assert.IsTrue(arvs.GetValidTo() > DateTime.Now);
        }


        [TestMethod]
        public async Task ValidateLegalBasisTestSuccess()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService
                );

            var ar = GetAuthorizationRequest();
            ar.LegalBasisList = new List<LegalBasis> {
                new LegalBasis { Id = "foo", Type = LegalBasisType.Espd, Content = "foo" }
            };
            ar.EvidenceRequests.First().LegalBasisId = "foo";

            // Act
            await arvs.Validate(ar);

            // Verify
            Assert.IsNotNull(arvs.GetAuthorizationRequest());
            Assert.HasCount(1, arvs.GetEvidenceCodeNamesWithVerifiedLegalBasis());
            Assert.IsTrue(arvs.GetValidTo() > DateTime.Now);
        }

        [TestMethod]        
        public async Task ValidateWrongCaseEvidenceCodeNameFailure()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var ar = GetAuthorizationRequest();
            ar.EvidenceRequests.First().EvidenceCodeName = ar.EvidenceRequests.First().EvidenceCodeName.ToLowerInvariant();

            // Act
            await Assert.ThrowsAsync<UnknownEvidenceCodeException>(async () =>
            {
                await arvs.Validate(ar);
            });
        }

        [TestMethod]
        public async Task ValidateAuthRequestAndRequestIsNullFailure()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            // Act
            await Assert.ThrowsAsync<InvalidAuthorizationRequestException>(async () =>
            {
                await arvs.Validate(null);
            });
        }

        [TestMethod]
        public async Task ValidateRequirementValidatorHasErrorsFailure()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            A.CallTo(() => _mockRequirementValidatorService.ValidateRequirements(
                    A<Dictionary<string, List<Requirement>>>._, A<AuthorizationRequest>._))
                .Returns(Task.FromResult(new List<string>() { "someerror" }));

            // Act
            await Assert.ThrowsAsync<AuthorizationFailedException>(async () =>
            {
                await arvs.Validate(GetAuthorizationRequest());
            }); 
        }

        [TestMethod]
        public async Task ValidateSoftRequirementEvidenceCodeIsRemoved()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authorizationRequest = GetAuthorizationRequest();
            authorizationRequest.EvidenceRequests.Add(new EvidenceRequest() { EvidenceCodeName = EVIDENCECODE_FAILURE_ACTION_SKIP });

            // Act
            await arvs.Validate(authorizationRequest);

            // Verify
            Assert.HasCount(1, arvs.GetEvidenceCodes());
            Assert.HasCount(1, arvs.GetSkippedEvidenceCodes());
            Assert.AreEqual(EVIDENCECODE_FAILURE_ACTION_SKIP, arvs.GetSkippedEvidenceCodes().First().Key);
            Assert.IsInstanceOfType(arvs.GetSkippedEvidenceCodes().First().Value, typeof(AltinnRoleRequirement));
        }

        [TestMethod]
        public async Task ValidateWrongServiceContext()
        {
            // Setup
            var mockRequestContextServiceWithDifferentServiceContext = A.Fake<IRequestContextService>();
            mockRequestContextServiceWithDifferentServiceContext.ServiceContext = new ServiceContext() { Id = "someothercontext", Name = "someothercontext-id", ValidLanguages = new List<string>() { "no" }, AuthorizationRequirements = new List<Requirement>() };

            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                mockRequestContextServiceWithDifferentServiceContext);


            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestException>(async () => 
            {
                await arvs.Validate(GetAuthorizationRequest());
            });

        }


        [TestMethod]
        public async Task ValidParams()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_2, Parameters = GetParameterListForRequest() });

            // Act
            await arvs.Validate(authRequest);
        }

        [TestMethod]
        public async Task InvalidParamNotANumber()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.Find(x => x.EvidenceParamName == EVIDENCEPARAM_NUMBER).Value = "abcd";
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_3, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });

        }

        [TestMethod]
        public async Task InvalidParamNotADateTime()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.Find(x => x.EvidenceParamName == EVIDENCEPARAM_DATETIME).Value = "abcd";
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_3, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });

        }

        [TestMethod]
        public async Task InvalidParamNotABoolean()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.Find(x => x.EvidenceParamName == EVIDENCEPARAM_BOOLEAN).Value = "abcd";
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_3, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });
        }

        [TestMethod]
        public async Task InvalidParamTooFew()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.RemoveAll(x => x.EvidenceParamName == EVIDENCEPARAM_NUMBER);
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_2, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });
        }

        [TestMethod]
        public async Task InvalidParamTooFewIsNull()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_2 });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });
        }

        [TestMethod]
        public async Task InvalidParamTooMany()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.Add(new EvidenceParameter { EvidenceParamName = "toomuch" });
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_2, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            }); 

        }

        [TestMethod]
        public async Task InvalidParamMissingRequired()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.Find(x => x.EvidenceParamName == EVIDENCEPARAM_NUMBER).EvidenceParamName = EVIDENCEPARAM_ATTACHMENT;
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_2, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });
        }

        [TestMethod]
        public async Task InvalidParamUnknown()
        {
            // Setup
            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();
            var invalidParamList = GetParameterListForRequest();
            invalidParamList.Add(new EvidenceParameter { EvidenceParamName = "unknown" });
            invalidParamList.RemoveAll(x => x.EvidenceParamName == EVIDENCEPARAM_ATTACHMENT);
            authRequest.EvidenceRequests.Add(new EvidenceRequest { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_3, Parameters = invalidParamList });

            // Act
            await Assert.ThrowsAsync<InvalidEvidenceRequestParameterException>(async () =>
            {
                await arvs.Validate(authRequest);
            });
        }

        [TestMethod]
        public async Task CheckEvidenceCodeWithEmptyBelongsToServiceContext()
        {
            // Setup
            var ae = GetAvailableEvidenceCodes().Take(1).ToList();
            ae.First().BelongsToServiceContexts = new List<string>();
            ae.First().ServiceContext = null;

            A.CallTo(() => _mockAvailableEvidenceCodesService.GetAvailableEvidenceCodes(A<bool>._))
                .Returns(Task.FromResult(ae));

            var arvs = new AuthorizationRequestValidatorService(
                _loggerFactory,
                _mockEntityRegistryService,
                _mockAvailableEvidenceCodesService,
                _mockRequirementValidatorService,
                _mockRequestContextService);

            var authRequest = GetAuthorizationRequest();

            // Act
            await arvs.Validate(authRequest);

            // Assert no exception thrown
        }

        private AuthorizationRequest GetAuthorizationRequest(string requestor = null, string subject = null)
        {
            return new AuthorizationRequest()
            {
                Requestor = requestor ?? "991825827",
                Subject = subject ?? "991825827",
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

        private List<EvidenceCode> GetAvailableEvidenceCodes()
        {
            return new List<EvidenceCode>
            {
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_OPEN, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_REQUIRING_LEGALBASIS_1, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_REQUIRING_LEGALBASIS_2, ServiceCode = "1", ServiceEditionCode = 1, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_REQUIRING_LEGALBASIS_3,ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_REQUIRING_CONSENT, ServiceCode = "1", ServiceEditionCode = 1, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_ASYNC_OPEN, IsAsynchronous = true, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_1, IsAsynchronous = true, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_2, IsAsynchronous = true, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_3, IsAsynchronous = true, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_ASYNC_OPEN_FAIL_INIT_4, IsAsynchronous = true, ServiceContext = "eBevis"},
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_1, Parameters = GetParameterListForDefinition(), ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_2, Parameters = GetParameterListForDefinition(), ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_PARAMS_OPTIONAL_3, Parameters = GetParameterListForDefinition(), ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_1, Parameters = GetParameterListForDefinition(true) , ServiceContext = "eBevis"},
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_PARAMS_REQUIRED_2, Parameters = GetParameterListForDefinition(true) , ServiceContext = "eBevis"},
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_MAX_VALID_DAYS_1, MaxValidDays = 5, ServiceContext = "eBevis" },
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_MAX_VALID_DAYS_2, MaxValidDays = 10 , ServiceContext = "eBevis"},
                new EvidenceCode { EvidenceCodeName = EVIDENCECODE_FAILURE_ACTION_SKIP, MaxValidDays = 10 , ServiceContext = "eBevis", AuthorizationRequirements = new List<Requirement>() { new Requirement() } },
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
                new EvidenceParameter { EvidenceParamName = EVIDENCEPARAM_BOOLEAN, Value = true, ParamType = EvidenceParamType.Boolean },
                new EvidenceParameter { EvidenceParamName = EVIDENCEPARAM_NUMBER, Value = 123.456, ParamType = EvidenceParamType.Number, Required = withRequiredParams },
                new EvidenceParameter { EvidenceParamName = EVIDENCEPARAM_STRING, Value = "stringvalue", ParamType = EvidenceParamType.String, Required = withRequiredParams },
                new EvidenceParameter { EvidenceParamName = EVIDENCEPARAM_DATETIME, Value = "2022-01-12", ParamType = EvidenceParamType.DateTime },
                new EvidenceParameter { EvidenceParamName = EVIDENCEPARAM_ATTACHMENT, Value = "somebase64", ParamType = EvidenceParamType.Attachment }
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
    }
}
