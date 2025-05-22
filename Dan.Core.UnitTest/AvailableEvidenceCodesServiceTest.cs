using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Dan.Common.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Polly;
using Polly.Caching.Memory;
using Polly.Registry;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]

    public class AvailableEvidenceCodesServiceTest
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        private readonly Mock<AsyncPolicy<List<EvidenceCode>>> _mockAsyncPolicy = new Mock<AsyncPolicy<List<EvidenceCode>>>();
        private readonly Mock<IServiceContextService> _mockServiceContextService = new Mock<IServiceContextService>();
        private readonly Mock<IFunctionContextAccessor> _mockFunctionContextAccessor = new Mock<IFunctionContextAccessor>();

        private IPolicyRegistry<string> _policyRegistry;

        private List<EvidenceCode> _availableEvidenceCodes = new List<EvidenceCode>
        {
            new EvidenceCode
            {
                EvidenceCodeName = "ec1",
                BelongsToServiceContexts = new List<string> { "sc1" },
                AuthorizationRequirements = new List<Requirement>
                {
                    new TestAuthorizationRequirement
                    {
                        Name = "ec1_req"
                    }
                }
            },
            new EvidenceCode
            {
                EvidenceCodeName = "ec2",
                BelongsToServiceContexts = new List<string> { "sc1", "sc2" },
                AuthorizationRequirements = new List<Requirement>
                {
                    new TestAuthorizationRequirement
                    {
                        Name = "ec2_req"
                    }
                }
            },
            new EvidenceCode
            {
                EvidenceCodeName = "ec3",
                BelongsToServiceContexts = new List<string> { "sc1", "sc2" },
                DatasetAliases = new List<DatasetAlias>
                {
                    new() {ServiceContext = "sc1", DatasetAliasName = "a1"},
                    new() {ServiceContext = "sc2", DatasetAliasName = "a2"}
                },
                AuthorizationRequirements = new List<Requirement>
                {
                    new TestAuthorizationRequirement
                    {
                        Name = "ec3_req"
                    }
                }
            }
        };

        [TestInitialize]
        public void Initialize()
        {
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(TestHelpers.GetHttpClientMock(JsonConvert.SerializeObject(_availableEvidenceCodes, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto })));

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            _policyRegistry = new PolicyRegistry()
            {
                {
                    "EvidenceCodesCachePolicy", Policy.CacheAsync<List<EvidenceCode>>(memoryCacheProvider, TimeSpan.FromMinutes(5))
                }
            };

            _mockServiceContextService.Setup(_ => _.GetRegisteredServiceContexts()).ReturnsAsync(
                new List<ServiceContext>
                {
                    new ServiceContext
                    {
                        Name = "sc1",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new TestAuthorizationRequirement
                            {
                                Name = "sc1_req1"
                            },
                            new TestAuthorizationRequirement
                            {
                                Name = "sc1_req2"
                            }
                        }
                    },
                    new ServiceContext
                    {
                        Name = "sc2",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new TestAuthorizationRequirement
                            {
                                Name = "sc2_req1"
                            },
                            new TestAuthorizationRequirement
                            {
                                Name = "sc2_req2"
                            }
                        }
                    }
                });
        }

        [TestMethod]
        public async Task CheckServiceContextRequirementsIncluded()
        {

            // Arrange
            var acs = new AvailableEvidenceCodesService(
                _loggerFactory,
                _mockHttpClientFactory.Object,
                _policyRegistry,
                _mockServiceContextService.Object,
                _mockFunctionContextAccessor.Object);

            // Act
            var a = await acs.GetAvailableEvidenceCodes();

            // Assert
            Assert.AreEqual(4, a.Count);
            
            // ec1
            Assert.AreEqual(3, a[0].AuthorizationRequirements.Count);
            Assert.IsTrue(a[0].AuthorizationRequirements.All(x => x.GetType() == typeof(TestAuthorizationRequirement)));
            Assert.IsTrue(a[0].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec1_req"));
            Assert.IsTrue(a[0].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req1"));
            Assert.IsTrue(a[0].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req2"));

            // ec2
            Assert.AreEqual(5, a[1].AuthorizationRequirements.Count);
            Assert.IsTrue(a[1].AuthorizationRequirements.All(x => x.GetType() == typeof(TestAuthorizationRequirement)));
            Assert.IsTrue(a[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec2_req"));
            Assert.IsTrue(a[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req1"));
            Assert.IsTrue(a[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req2"));
            Assert.IsTrue(a[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req1"));
            Assert.IsTrue(a[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req2"));
            
            // Aliases get replaced at end of list
            // a1
            Assert.AreEqual(3, a[2].AuthorizationRequirements.Count);
            Assert.IsTrue(a[2].AuthorizationRequirements.All(x => x.GetType() == typeof(TestAuthorizationRequirement)));
            Assert.IsTrue(a[2].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec3_req"));
            Assert.IsTrue(a[2].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req1"));
            Assert.IsTrue(a[2].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req2"));
            
            // a2
            Assert.AreEqual(3, a[3].AuthorizationRequirements.Count);
            Assert.IsTrue(a[3].AuthorizationRequirements.All(x => x.GetType() == typeof(TestAuthorizationRequirement)));
            Assert.IsTrue(a[3].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec3_req"));
            Assert.IsTrue(a[3].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req1"));
            Assert.IsTrue(a[3].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req2"));
        }

        [TestMethod]
        public async Task GetAliases()
        {
            var acs = new AvailableEvidenceCodesService(
                _loggerFactory,
                _mockHttpClientFactory.Object,
                _policyRegistry,
                _mockServiceContextService.Object,
                _mockFunctionContextAccessor.Object);

            var expected = new Dictionary<string, string>
            {
                {"a1", "ec3"},
                {"a2", "ec3"}
            };

            // Act
            await acs.GetAvailableEvidenceCodes();
            var actual = acs.GetAliases();
            
            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
    }

    [DataContract]
    internal class TestAuthorizationRequirement : Requirement
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }
    }

}
