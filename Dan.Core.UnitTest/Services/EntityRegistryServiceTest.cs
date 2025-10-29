using System.Diagnostics.CodeAnalysis;
using Dan.Common.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dan.Core.UnitTest.Services;

[TestClass]
[ExcludeFromCodeCoverage]
public class EntityRegistryServiceTest
{
    private readonly Mock<IEntityRegistryApiClientService> entityRegistryApiClientServiceMock = new();
    private readonly Mock<ILogger<EntityRegistryService>> logger = new();
    private readonly EntityRegistryService entityRegistryService;

    public EntityRegistryServiceTest()
    {
        entityRegistryService = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        entityRegistryApiClientServiceMock.Setup(_ => _.GetUpstreamEntityRegistryUnitAsync(It.IsAny<Uri>()))
            .Returns((Uri url) =>
            {
                var orgForm = new Organisasjonsform { Kode = "AS" };
                if (url.AbsolutePath.Contains("/enheter/91"))
                {
                    return Task.FromResult(new EntityRegistryUnit { Organisasjonsnummer = "91", Organisasjonsform = orgForm })!;
                }

                if (url.AbsolutePath.Contains("/underenheter/92"))
                {
                    return Task.FromResult(new EntityRegistryUnit { Organisasjonsnummer = "92", OverordnetEnhet = "91", Organisasjonsform = orgForm  })!;
                }

                // Only subunits are at the leaf node, any nested parents are MainUnits
                // Example: https://data.brreg.no/enhetsregisteret/api/underenheter/879587662
                if (url.AbsolutePath.Contains("/enheter/93"))
                {
                    return Task.FromResult(new EntityRegistryUnit { Organisasjonsnummer = "93", OverordnetEnhet = "91", Organisasjonsform = orgForm  })!;
                }

                if (url.AbsolutePath.Contains("/underenheter/94"))
                {
                    return Task.FromResult(new EntityRegistryUnit { Organisasjonsnummer = "94", OverordnetEnhet = "93", Organisasjonsform = orgForm  })!;
                }
                
                if (url.AbsolutePath.Contains("/underenheter/31"))
                {
                    return Task.FromResult(new EntityRegistryUnit { Organisasjonsnummer = "31", Organisasjonsform = orgForm  })!;
                }

                return Task.FromResult<EntityRegistryUnit?>(null);
            });
    }
    
    [TestMethod]
    public void TestGetMapping()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.Get("91").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }

    [TestMethod]
    public void TestGetAttemptSubUnitLookupIfNotFoundReturnsSubUnit()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.Get("92").Result;
        Assert.AreEqual("92", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetAttemptSubUnitLookupIfNotFoundSetToFalseReturnsNull()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.Get("92", attemptSubUnitLookupIfNotFound: false).Result;
        Assert.IsNull(r);
    }
    
    [TestMethod]
    public void TestGetMainUnit()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.GetMainUnit("91").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetMainUnitAttemptsSubunitLookup()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.GetMainUnit("92").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetFullMainAttemptsSubunitLookup()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.GetFullMainUnit("92").Result;
        Assert.AreEqual("91", r?.Organisasjonsnummer);
    }
    
    [TestMethod]
    public void TestGetSubUnitOnlyReturnsSubunit()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.Get("92", subUnitOnly: true).Result;
        Assert.AreEqual("92", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetSubUnitOnlyReturnsNullIfMainUnit()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.Get("91", subUnitOnly: true).Result;
        Assert.IsNull(r);
    }

    [TestMethod]
    public void TestGetNestToTopmostMainUnitReturnsMainunit()
    {
        var s = new EntityRegistryService(entityRegistryApiClientServiceMock.Object, logger.Object);
        var r = s.GetMainUnit("94").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }
    
    [DataTestMethod]
    [DataRow("971032146", "AS", "0000", "0000", true)]  // Public sector organization from PublicSectorOrganizations
    [DataRow("123456781", "KF", "0000", "0000", true)]  // Public sector unit type
    [DataRow("123456782", "AS", "3900", "0000", true)]  // Public sector sector code
    [DataRow("123456783", "AS", "0000", "8411", true)]  // Public sector industrial code
    [DataRow("123456784", "AS", "0000", "0000", false)] // Non-public sector organization
    public async Task TestIsPublicAgency(
        string organizationNumber,
        string organizationForm,
        string sectorCode,
        string industrialCode1,
        bool expectedResult)
    {
        // Arrange
        var entityRegistryUnit = new EntityRegistryUnit
        {
            Organisasjonsnummer = organizationNumber,
            Organisasjonsform = new Organisasjonsform { Kode = organizationForm },
            InstitusjonellSektorkode = new SektorKodeDto{Kode = sectorCode},
            Naeringskode1 = new NaeringsKodeDto{ Kode = industrialCode1 }
        };
        
        entityRegistryApiClientServiceMock
            .Setup(m => m.GetUpstreamEntityRegistryUnitAsync(It.IsAny<Uri>()))
            .ReturnsAsync(entityRegistryUnit);

        // Act
        var isPublicAgency = await entityRegistryService.IsPublicAgency(organizationNumber);

        // Assert
        Assert.AreEqual(expectedResult, isPublicAgency);
    }
}