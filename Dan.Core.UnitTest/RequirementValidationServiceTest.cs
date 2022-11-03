using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Helpers;
using Dan.Core.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ConsentRequirement = Nadobe.Common.Models.ConsentRequirement;

namespace Dan.Core.UnitTest
{
    [TestClass]
    public class RequirementValidationServiceTest
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        private readonly Mock<IEntityRegistryService> _mockEntityRegistryService = new Mock<IEntityRegistryService>();
        private readonly Mock<IAltinnServiceOwnerApiService> _mockAltinnServiceOwnerApiService =
            new Mock<IAltinnServiceOwnerApiService>();
        private readonly Mock<IRequestContextService> _mockRequestContextService =
            new Mock<IRequestContextService>();

        [TestInitialize]
        public void TestInitialize()
        {

            _mockEntityRegistryService.Setup(_ => _.Get(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(GetBrEntry()));

            _mockEntityRegistryService.Setup(_ => _.IsPublicAgency(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            _mockAltinnServiceOwnerApiService.Setup(_ =>
                    _.VerifyAltinnRole(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            _mockAltinnServiceOwnerApiService.Setup(_ =>
                    _.VerifyAltinnRight(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            _mockRequestContextService.SetupAllProperties();
            _mockRequestContextService.SetupGet(_ => _.AuthenticatedOrgNumber).Returns("912345678");
            _mockRequestContextService.SetupGet(_ => _.Scopes).Returns(new List<string>() { "a", "b" });
            _mockRequestContextService.SetupProperty(_ => _.ServiceContext, new Mock<ServiceContext>().Object);
        }

        [TestMethod]
        public async Task EvidenCodesWithoutRequirements()
        {
            string subject = "abc";
            string requestor = "abc";

            var authRequest = GetAuthRequest(subject, requestor);
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                { "ec1", new List<Requirement>() }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);

            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        [ExpectedException(typeof(InternalServerErrorException))]
        public async Task MisnamedEvidenceCodeTest()
        {
            string subject = "abc";
            string requestor = "abc";

            var authRequest = GetAuthRequest(subject, requestor);
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                { "notinrequest", new List<Requirement>() { GetAltinnRoleRequirement(AccreditationPartyTypes.Subject, AccreditationPartyTypes.Requestor, "UTINN") } }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);

            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task AltinnRoleRequirementTest()
        {
            string subject = "abc";
            string requestor = "abc";

            var authRequest = GetAuthRequest(subject, requestor);
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                { "ec1", new List<Requirement>() { GetAltinnRoleRequirement(AccreditationPartyTypes.Subject, AccreditationPartyTypes.Requestor, "UTINN") } }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);

            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task AltinnRightsRequirementTest()
        {
            string subject = "offeredby";
            string requestor = "abc";

            var authRequest = GetAuthRequest(subject, requestor);
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                { "ec1", new List<Requirement>() { GetAltinnRightsRequirement(AccreditationPartyTypes.Subject, AccreditationPartyTypes.Requestor, "UTINN") } }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task AccreditationPartyRequirementTest_RequestorAndOwnerAreEqual()
        {
            var authRequest = GetAuthRequest("erlend", "912345678");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                { "ec1", new List<Requirement>() { GetAccreditationPartyRequirement(AccreditationPartyRequirementType.RequestorAndOwnerAreEqual) } }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task MaskinPortenScopeRequirementTest()
        {
            var authRequest = GetAuthRequest("erlend", "requestor");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                { "ec1", new List<Requirement>() {GetMaskinportenScopeRequirement("a", "b") } }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task PublicAgencyRequirementTest()
        {
            var authRequest = GetAuthRequest("erlend", "991825827");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetPartyTypeRequirement(AccreditationPartyTypes.Requestor, PartyTypeConstraint.PublicAgency)
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);

        }

        [TestMethod]
        public async Task BR_OpenDataTest_Negative_MissingRole()
        {
            _mockAltinnServiceOwnerApiService.Setup(_ =>
                    _.VerifyAltinnRole(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            var authRequest = GetAuthRequest("erlend", "requestor");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        //Require role relationship between requestor (buyer) and owner (authenticated system vendor) that does not exist
                        GetAltinnRoleRequirement(AccreditationPartyTypes.Requestor, AccreditationPartyTypes.Owner,
                            "DAGL")
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task PartyTypeRequirement_SsnSubject()
        {
            //requestor 11 chars ==> ssn and invalid for requestor type
            var authRequest = GetAuthRequest("03065001488", "991825827");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetPartyTypeRequirement(AccreditationPartyTypes.Subject, PartyTypeConstraint.PrivatePerson)
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task PartyTypeRequirement_ForeignSubjectAndRequestor()
        {
            //requestor 11 chars ==> ssn and invalid for requestor type
            var authRequest = GetAuthRequest(null, null);
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetPartyTypeRequirement(AccreditationPartyTypes.Subject, PartyTypeConstraint.PrivatePerson),
                        GetPartyTypeRequirement(AccreditationPartyTypes.Requestor, PartyTypeConstraint.PublicAgency)
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 2);
        }

        [TestMethod]
        public async Task PartyTypeRequirement_Negative_WrongTypeRequestor()
        {
            //requestor 11 chars ==> ssn and invalid for requestor type
            var authRequest = GetAuthRequest("erlend", "06117701547");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetPartyTypeRequirements(AccreditationPartyTypes.Requestor,
                            PartyTypeConstraint.PublicAgency,
                            PartyTypeConstraint.PrivateEnterprise)
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task WhiteListRequirementTest_TwoOfEachParty()
        {
            var authRequest = GetAuthRequest("subjecta", "requestorb");
            authRequest.SubjectParty = new Party { NorwegianOrganizationNumber = "subjecta" };
            authRequest.RequestorParty = new Party { NorwegianOrganizationNumber = "requestorb" };
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetWhiteListRequirement(
                            new List<string>() { "ownera", "912345678" },
                            new List<string>() { "subjecta", "subjectb" },
                            new List<string>() { "requestora", "requestorb" })
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);

            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task WhiteListRequirementTest_SingleRequestor_Negative()
        {
            var authRequest = GetAuthRequest("subjecta", "requestora");
            authRequest.SubjectParty = new Party { NorwegianOrganizationNumber = "subjecta" };
            authRequest.RequestorParty = new Party { NorwegianOrganizationNumber = "requestora" };
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetWhiteListRequirement(
                            new List<string>() { "ownera", "912345678" },
                            new List<string>() { "subjecta", "subjectb" },
                            new List<string>() { "requestorb" })
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);

            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task WhiteListRequirementTest_AllFailed_Negative()
        {
            var authRequest = GetAuthRequest("notvalid", "notvalid");
            authRequest.SubjectParty = new Party { Scheme = "notvalid", Id = "notvalid" };
            authRequest.RequestorParty = new Party { Scheme = "notvalid", Id = "notvalid" };
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        GetWhiteListRequirement(
                            new List<string>() { "notvalid" },
                            new List<string>() { "subjecta", "subjectb" },
                            new List<string>() { "requestorb" })
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);

            Assert.IsTrue(errorList.Count == 3);
        }

        [TestMethod]
        public async Task SoftRequirementTest()
        {
            _mockAltinnServiceOwnerApiService.Setup(_ =>
                    _.VerifyAltinnRole(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            var authRequest = GetAuthRequest("erlend", "requestor");
            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new AltinnRoleRequirement()
                        {
                            CoveredBy = AccreditationPartyTypes.Requestor,
                            OfferedBy = AccreditationPartyTypes.Owner,
                            RoleCode = "DAGL",
                            FailureAction = FailureAction.Skip
                        }
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
            Assert.IsTrue(svc.GetSkippedEvidenceCodes().Count == 1);
        }

        [TestMethod]
        public async Task LegalBasisTest_Success_Single()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.LegalBasisList.Add(
                new LegalBasis()
                {
                    Type = LegalBasisType.Espd,
                    Content = "blabla"
                });

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new LegalBasisRequirement()
                        {
                            ValidLegalBasisTypes = LegalBasisType.Espd
                        }
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task LegalBasisTest_Success_Multiple_01()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.LegalBasisList = new List<LegalBasis>
            {
                new LegalBasis()
                {
                    Type = LegalBasisType.Espd,
                    Content = "blabla"
                }
            };

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new LegalBasisRequirement()
                        {
                            ValidLegalBasisTypes = LegalBasisType.Espd | LegalBasisType.Cpv
                        }
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task LegalBasisTest_Success_Multiple_02()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.LegalBasisList = new List<LegalBasis>
            {
                new LegalBasis()
                {
                    Type = LegalBasisType.Espd,
                    Content = "blabla"
                },
                new LegalBasis()
                {
                    Type = LegalBasisType.Cpv,
                    Content = "90911000-6"
                }
            };

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new LegalBasisRequirement()
                        {
                            ValidLegalBasisTypes = LegalBasisType.Cpv
                        }
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task LegalBasisTest_Success_Multiple_03()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.LegalBasisList = new List<LegalBasis>
            {
                new LegalBasis()
                {
                    Type = LegalBasisType.Espd,
                    Content = "blabla"
                },
                new LegalBasis()
                {
                    Type = LegalBasisType.Cpv,
                    Content = "blabla"
                }
            };

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new LegalBasisRequirement()
                        {
                            ValidLegalBasisTypes = LegalBasisType.Espd | LegalBasisType.Cpv
                        }
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task LegalBasisTest_Failed_CPV()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.LegalBasisList = new List<LegalBasis>
            {
                new LegalBasis()
                {
                    Type = LegalBasisType.Espd,
                    Content = "blabla"
                },
                new LegalBasis()
                {
                    Type = LegalBasisType.Cpv,
                    Content = "not-a-risky-code"
                }
            };

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new LegalBasisRequirement()
                        {
                            ValidLegalBasisTypes = LegalBasisType.Cpv
                        }
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task ConsentTest_Success()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.EvidenceRequests.First().RequestConsent = true;
            authRequest.ConsentReference = "foo";

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new ConsentRequirement()
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task ConsentTest_Failed_MissingRequestConsent()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.ConsentReference = "foo";

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new ConsentRequirement()
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task ConsentTest_Failed_MissingConsentReference()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");
            authRequest.EvidenceRequests.First().RequestConsent = true;

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new ConsentRequirement()
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task ConsentTest_Failed_MissingConsentMessageAndRequestConsent()
        {
            var authRequest = GetAuthRequest("bjorn", "requestor");

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new ConsentRequirement()
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 2);
        }

        [TestMethod]
        public async Task ConsentTest_Failed_NotNorwegianSubject()
        {
            var authRequest = GetAuthRequest(null, "requestor");
            authRequest.EvidenceRequests.First().RequestConsent = true;
            authRequest.ConsentReference = "foo";
            authRequest.SubjectParty =
                PartyParser.GetPartyFromIdentifier("iso6523-actorid-upis::9999:blabla-123", out _);

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new ConsentRequirement()
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task ConsentTest_Failed_NotNorwegianRequestor()
        {
            var authRequest = GetAuthRequest("bjorn", null);
            authRequest.EvidenceRequests.First().RequestConsent = true;
            authRequest.ConsentReference = "foo";
            authRequest.RequestorParty =
                PartyParser.GetPartyFromIdentifier("iso6523-actorid-upis::9999:blabla-123", out _);

            var reqs = new Dictionary<string, List<Requirement>>()
            {
                {
                    "ec1", new List<Requirement>
                    {
                        new ConsentRequirement()
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);
            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
        }

        [TestMethod]
        public async Task ReferenceTest_Success()
        {
            var authRequest = GetAuthRequest("bjorn", null);
            authRequest.EvidenceRequests.First().RequestConsent = true;
            authRequest.ConsentReference = "2018-123456";
            authRequest.RequestorParty =
                PartyParser.GetPartyFromIdentifier("iso6523-actorid-upis::9999:blabla-123", out _);

            var reqs = new Dictionary<string, List<Requirement>>
            {
                ["ec1"] = new List<Requirement>
                {
                    new ReferenceRequirement
                    {
                        ReferenceType = ReferenceType.ConsentReference,
                        AcceptedFormat = @"^[0-9]{4}-[0-9]{1,8}$"
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);

            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 0);
        }

        [TestMethod]
        public async Task ReferenceTest_Failed_InvalidFormat()
        {
            var authRequest = GetAuthRequest("bjorn", null);
            authRequest.ExternalReference = "my-own-reference-001";
            authRequest.RequestorParty =
                PartyParser.GetPartyFromIdentifier("iso6523-actorid-upis::9999:blabla-123", out _);

            var reqs = new Dictionary<string, List<Requirement>>
            {
                ["ec1"] = new List<Requirement>
                {
                    new ReferenceRequirement
                    {
                        ReferenceType = ReferenceType.ExternalReference,
                        AcceptedFormat = @"external-reference"
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);

            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
            Assert.IsTrue(errorList[0].Contains("The provided external reference is invalid; does not match the regular expression 'external-reference'"));
        }

        [TestMethod]
        public async Task ReferenceTest_Failed_InvalidRegex()
        {
            var authRequest = GetAuthRequest("bjorn", null);
            authRequest.ExternalReference = "my-own-reference-001";
            authRequest.RequestorParty =
                PartyParser.GetPartyFromIdentifier("iso6523-actorid-upis::9999:blabla-123", out _);

            var reqs = new Dictionary<string, List<Requirement>>
            {
                ["ec1"] = new List<Requirement>
                {
                    new ReferenceRequirement
                    {
                        ReferenceType = ReferenceType.ExternalReference,
                        AcceptedFormat = @"["
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);

            var errorList = await svc.ValidateRequirements(reqs, authRequest);
            Assert.IsTrue(errorList.Count == 1);
            Assert.IsTrue(errorList[0].Contains("The accepted format is invalid; '[' is not a valid regular expression."));
        }

        [TestMethod]
        public async Task ReferenceTest_Failed_ConsentReferenceIsNull()
        {
            var authRequest = GetAuthRequest("bjorn", null);
            authRequest.EvidenceRequests.First().RequestConsent = true;
            authRequest.ConsentReference = null;
            authRequest.RequestorParty =
                PartyParser.GetPartyFromIdentifier("iso6523-actorid-upis::9999:blabla-123", out _);

            var req = new Dictionary<string, List<Requirement>>
            {
                ["ec1"] = new List<Requirement>
                {
                    new ReferenceRequirement
                    {
                        ReferenceType = ReferenceType.ConsentReference,
                        AcceptedFormat = @"^[0-9]{4}-[0-9]{1,8}$"
                    }
                }
            };

            var svc = new RequirementValidationService(_mockAltinnServiceOwnerApiService.Object, _mockEntityRegistryService.Object, _mockRequestContextService.Object);

            var errorList = await svc.ValidateRequirements(req, authRequest);
            Assert.IsTrue(errorList.Count == 1);
            Assert.IsTrue(errorList[0].Contains("The request requires a valid consent reference but none is provided"));
        }

        private Requirement GetWhiteListRequirement(List<string> owners, List<string> subjects, List<string> requestors)
        {
            var result = new WhiteListRequirement();
            result.AllowedParties = new List<KeyValuePair<AccreditationPartyTypes, string>>();

            foreach (var a in owners)
            {
                result.AllowedParties.Add(new KeyValuePair<AccreditationPartyTypes, string>(AccreditationPartyTypes.Owner, a));
            }

            foreach (var a in subjects)
            {
                result.AllowedParties.Add(new KeyValuePair<AccreditationPartyTypes, string>(AccreditationPartyTypes.Subject, a));
            }

            foreach (var a in requestors)
            {
                result.AllowedParties.Add(new KeyValuePair<AccreditationPartyTypes, string>(AccreditationPartyTypes.Requestor, a));
            }


            return result;
        }

        private Requirement GetWhiteListFromConfigRequirement(string ownerKey, string subjectKey, string requestorKey)
        {
            var result = new WhiteListFromConfigRequirement()
            {
                SubjectConfigKey = subjectKey,
                OwnerConfigKey = ownerKey,
                RequestorConfigKey = requestorKey
            };


            return result;
        }

        private Requirement GetAltinnRoleRequirement(AccreditationPartyTypes offeredby, AccreditationPartyTypes coveredby, string roleCode)
        {

            return new AltinnRoleRequirement()
            {
                CoveredBy = coveredby,
                OfferedBy = offeredby,
                RoleCode = roleCode
            };
        }

        private Requirement GetPartyTypeRequirements(AccreditationPartyTypes reqType, PartyTypeConstraint partyType, PartyTypeConstraint partyType2)
        {
            return new Common.Models.PartyTypeRequirement()
            {
                AllowedPartyTypes = new AllowedPartyTypesList()
                    {
                        new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(reqType, partyType),
                        new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(reqType, partyType2)
                    }
            };
        }

        private Requirement GetPartyTypeRequirement(AccreditationPartyTypes reqType, PartyTypeConstraint partyType)
        {
            return new Common.Models.PartyTypeRequirement()
            {
                AllowedPartyTypes = new AllowedPartyTypesList()
                    {
                        new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(reqType, partyType)
                    }
            };
        }


        private Requirement GetAltinnRightsRequirement(AccreditationPartyTypes offeredby, AccreditationPartyTypes coveredby, string roleCode)
        {
            return new AltinnRightsRequirement()
            {
                RightsActions = new List<AltinnAction>() { AltinnAction.Read },
                CoveredBy = coveredby,
                OfferedBy = offeredby,
                ServiceCode = "1000",
                ServiceEdition = "2000"
            };
        }

        private Requirement GetMaskinportenScopeRequirement(string scope, string scope2)
        {
            return new MaskinportenScopeRequirement()
            {
                RequiredScopes = new List<string>()
                    {
                        scope, scope2
                    }
            };
        }

        private Requirement GetAccreditationPartyRequirement(AccreditationPartyRequirementType type1)
        {
            return new AccreditationPartyRequirement()
            {
                PartyRequirements = new List<AccreditationPartyRequirementType>()
                     { type1}
            };
        }

        private AuthorizationRequest GetAuthRequest(string subject, string requestor)
        {
            return new AuthorizationRequest()
            {
                ConsentReference = string.Empty,
                ExternalReference = string.Empty,
                Requestor = requestor,
                Subject = subject,
                ValidTo = DateTime.Now.AddDays(90),
                EvidenceRequests = new List<EvidenceRequest>()
                {
                    new EvidenceRequest()
                    {
                        EvidenceCodeName = "ec1"
                    }
                },
                LegalBasisList = new List<LegalBasis>()
            };
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



