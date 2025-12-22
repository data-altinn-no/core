using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Dan.Common.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly IHttpClientFactory _mockHttpClientFactory = A.Fake<IHttpClientFactory>();
        private readonly IServiceContextService _mockServiceContextService = A.Fake<IServiceContextService>();
        private readonly IFunctionContextAccessor _mockFunctionContextAccessor = A.Fake<IFunctionContextAccessor>();

        private IPolicyRegistry<string> _policyRegistry;

        private List<EvidenceCode> _availableEvidenceCodes = new List<EvidenceCode>
        {
            new EvidenceCode
            {
                EvidenceCodeName = "ec1",
                BelongsToServiceContexts = new List<string> { "sc1" },
                AuthorizationRequirements = new List<Requirement>
                {
                    new TestAuthorizationRequirement { Name = "ec1_req" }
                }
            },
            new EvidenceCode
            {
                EvidenceCodeName = "ec2",
                BelongsToServiceContexts = new List<string> { "sc1", "sc2" },
                AuthorizationRequirements = new List<Requirement>
                {
                    new TestAuthorizationRequirement { Name = "ec2_req" }
                }
            },
            new EvidenceCode
            {
                EvidenceCodeName = "ec3",
                BelongsToServiceContexts = new List<string> { "sc1", "sc2" },
                DatasetAliases = new List<DatasetAlias>
                {
                    new() { ServiceContext = "sc1", DatasetAliasName = "a1" },
                    new() { ServiceContext = "sc2", DatasetAliasName = "a2" }
                },
                AuthorizationRequirements = new List<Requirement>
                {
                    new TestAuthorizationRequirement { Name = "ec3_req" }
                }
            }
        };

        [TestInitialize]
        public void Initialize()
        {
            // Fake HttpClientFactory
            A.CallTo(() => _mockHttpClientFactory.CreateClient(A<string>._))
                .Returns(TestHelpers.GetHttpClientMock(
                    JsonConvert.SerializeObject(
                        _availableEvidenceCodes,
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }
                    )
                ));

            // Setup memory cache policy
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            _policyRegistry = new PolicyRegistry
            {
                {
                    "EvidenceCodesCachePolicy",
                    Policy.CacheAsync<List<EvidenceCode>>(memoryCacheProvider, TimeSpan.FromMinutes(5))
                }
            };

            // Fake ServiceContextService
            A.CallTo(() => _mockServiceContextService.GetRegisteredServiceContexts())
                .Returns(new List<ServiceContext>
                {
                    new ServiceContext
                    {
                        Name = "sc1",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new TestAuthorizationRequirement { Name = "sc1_req1" },
                            new TestAuthorizationRequirement { Name = "sc1_req2" }
                        }
                    },
                    new ServiceContext
                    {
                        Name = "sc2",
                        AuthorizationRequirements = new List<Requirement>
                        {
                            new TestAuthorizationRequirement { Name = "sc2_req1" },
                            new TestAuthorizationRequirement { Name = "sc2_req2" }
                        }
                    }
                });

            // Inside Initialize()
            var services = new ServiceCollection();
            var fakeRequestContextService = A.Fake<IRequestContextService>();
            services.AddSingleton(fakeRequestContextService);

            var serviceProvider = services.BuildServiceProvider();

            // Fake the FunctionContextAccessor to return a FunctionContext with this ServiceProvider
            var fakeFunctionContext = A.Fake<Microsoft.Azure.Functions.Worker.FunctionContext>();
            A.CallTo(() => fakeFunctionContext.InstanceServices).Returns(serviceProvider);

            A.CallTo(() => _mockFunctionContextAccessor.FunctionContext)
                .Returns(fakeFunctionContext);
        }

        [TestMethod]
        public async Task CheckServiceContextRequirementsIncluded()
        {
            // Arrange
            var mockCache = new MockCache();
            var acs = new AvailableEvidenceCodesService(
                _loggerFactory,
                _mockHttpClientFactory,
                _policyRegistry,
                mockCache,
                _mockServiceContextService,
                _mockFunctionContextAccessor
            );

            // Act
            var result = await acs.GetAvailableEvidenceCodes();

            // Assert
            Assert.HasCount(4, result);

            // ec1
            Assert.HasCount(3, result[0].AuthorizationRequirements);
            Assert.IsTrue(result[0].AuthorizationRequirements.All(x => x is TestAuthorizationRequirement));
            Assert.IsTrue(result[0].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec1_req"));
            Assert.IsTrue(result[0].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req1"));
            Assert.IsTrue(result[0].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req2"));

            // ec2
            Assert.HasCount(5, result[1].AuthorizationRequirements);
            Assert.IsTrue(result[1].AuthorizationRequirements.All(x => x is TestAuthorizationRequirement));
            Assert.IsTrue(result[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec2_req"));
            Assert.IsTrue(result[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req1"));
            Assert.IsTrue(result[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req2"));
            Assert.IsTrue(result[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req1"));
            Assert.IsTrue(result[1].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req2"));

            // Aliases
            Assert.HasCount(3, result[2].AuthorizationRequirements);
            Assert.IsTrue(result[2].AuthorizationRequirements.All(x => x is TestAuthorizationRequirement));
            Assert.IsTrue(result[2].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec3_req"));
            Assert.IsTrue(result[2].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req1"));
            Assert.IsTrue(result[2].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc1_req2"));

            Assert.HasCount(3, result[3].AuthorizationRequirements);
            Assert.IsTrue(result[3].AuthorizationRequirements.All(x => x is TestAuthorizationRequirement));
            Assert.IsTrue(result[3].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "ec3_req"));
            Assert.IsTrue(result[3].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req1"));
            Assert.IsTrue(result[3].AuthorizationRequirements.Any(x => ((TestAuthorizationRequirement)x).Name == "sc2_req2"));
        }

        [TestMethod]
        public async Task GetAliases()
        {
            var mockCache = new MockCache();
            var acs = new AvailableEvidenceCodesService(
                _loggerFactory,
                _mockHttpClientFactory,
                _policyRegistry,
                mockCache,
                _mockServiceContextService,
                _mockFunctionContextAccessor
            );

            var expected = new Dictionary<string, string>
            {
                { "a1", "ec3" },
                { "a2", "ec3" }
            };

            // Act
            await acs.GetAvailableEvidenceCodes();
            var actual = await acs.GetAliases();

            // Assert
            Assert.HasCount(expected.Count, actual);
            foreach (var kvp in expected)
            {
                Assert.IsTrue(actual.ContainsKey(kvp.Key));
                Assert.AreEqual(kvp.Value, actual[kvp.Key]);
            }
        }
    }

    [DataContract]
    internal class TestAuthorizationRequirement : Requirement
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }
    }
}
