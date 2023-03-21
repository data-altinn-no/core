namespace Dan.Common.UnitTest.Services;

[TestClass]
[ExcludeFromCodeCoverage]
public class EntityRegistryServiceTest
{
    private readonly Mock<IEntityRegistryApiClientService> _entityRegistryApiClientServiceMock = new();
    private readonly IEntityRegistryService _entityRegistryService;

    public EntityRegistryServiceTest()
    {
        _entityRegistryService = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _entityRegistryApiClientServiceMock.Setup(_ => _.GetUpstreamEntityRegistryUnitAsync(It.IsAny<Uri>()))
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
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.Get("91").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }

    [TestMethod]
    public void TestGetAttemptSubUnitLookupIfNotFoundReturnsSubUnit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.Get("92").Result;
        Assert.AreEqual("92", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetAttemptSubUnitLookupIfNotFoundSetToFalseReturnsNull()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.Get("92", attemptSubUnitLookupIfNotFound: false).Result;
        Assert.IsNull(r);
    }
    
    [TestMethod]
    public void TestGetMainUnit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.GetMainUnit("91").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetMainUnitAttemptsSubunitLookup()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.GetMainUnit("92").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetFullMainAttemptsSubunitLookup()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.GetFullMainUnit("92").Result;
        Assert.AreEqual("91", r?.Organisasjonsnummer);
    }
    
    [TestMethod]
    public void TestGetSubUnitOnlyReturnsSubunit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.Get("92", subUnitOnly: true).Result;
        Assert.AreEqual("92", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestGetSubUnitOnlyReturnsNullIfMainUnit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.Get("91", subUnitOnly: true).Result;
        Assert.IsNull(r);
    }

    [TestMethod]
    public void TestGetNestToTopmostMainUnitReturnsMainunit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.GetMainUnit("94").Result;
        Assert.AreEqual("91", r?.OrganizationNumber);
    }
    
    [TestMethod]
    public void TestSyntheticLookupsNotAllowedByDefault()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.GetMainUnit("31").Result;
        Assert.IsNull(r);
    }
    
    [TestMethod]
    public void TestIsMainUnit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.IsMainUnit("91").Result;
        Assert.IsTrue(r);
    }
    
    [TestMethod]
    public void TestIsMainUnitSimpleObj()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.IsMainUnit(new SimpleEntityRegistryUnit());
        Assert.IsTrue(r);
    }
    
    [TestMethod]
    public void TestIsMainUnitFullObj()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.IsMainUnit(new EntityRegistryUnit { Organisasjonsnummer = "91", Organisasjonsform = new Organisasjonsform { Kode = "AS" } });
        Assert.IsTrue(r);
    }
    
    [TestMethod]
    public void TestIsSubUnit()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.IsSubUnit("92").Result;
        Assert.IsTrue(r);
    }
    
    [TestMethod]
    public void TestIsSubUnitSimpleObj()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.IsSubUnit(new SimpleEntityRegistryUnit { ParentUnit = "x" });
        Assert.IsTrue(r);
    }
    
    [TestMethod]
    public void TestIsSubUnitFullObj()
    {
        var s = new EntityRegistryService(_entityRegistryApiClientServiceMock.Object) { UseCoreProxy = false };
        var r = s.IsSubUnit(new EntityRegistryUnit { Organisasjonsnummer = "91", Organisasjonsform = new Organisasjonsform { Kode = "AS" }, OverordnetEnhet = "x" });
        Assert.IsTrue(r);
    }
    
    [DataTestMethod]
    [DataRow("971032146", "AS", "0000", "0000", true)]  // Public sector organization from PublicSectorOrganizations
    [DataRow("123456789", "KF", "0000", "0000", true)]  // Public sector unit type
    [DataRow("123456789", "AS", "3900", "0000", true)]  // Public sector sector code
    [DataRow("123456789", "AS", "0000", "8411", true)]  // Public sector industrial code
    [DataRow("123456789", "AS", "0000", "0000", false)] // Non-public sector organization
    public void TestIsPublicAgency(
        string organizationNumber,
        string organizationForm,
        string sectorCode,
        string industrialCode1,
        bool expectedResult)
    {
        // Arrange
        var entityRegistryUnit = new SimpleEntityRegistryUnit
        {
            OrganizationNumber = organizationNumber,
            OrganizationForm = organizationForm,
            SectorCode = sectorCode,
            IndustrialCodes =  new List<string> { industrialCode1 }
        };

        // Act
        var isPublicAgency = _entityRegistryService.IsPublicAgency(entityRegistryUnit);

        // Assert
        Assert.AreEqual(expectedResult, isPublicAgency);
    }
}