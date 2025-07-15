using System.Collections.Concurrent;
using System.Diagnostics;
using Dan.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dan.Common.Services;
/// <summary>
/// Default implementation of IEntityRegistryService
/// </summary>
[Obsolete("Use Dan.Common.Services.CcrClientService instead.")]
public class EntityRegistryService(
    IEntityRegistryApiClientService entityRegistryApiClientService) : IEntityRegistryService
{
    /// <summary>
    /// Flag to set if using PpeProxyMainUnitLookupEndpoint or MainUnitLookupEndpoint
    /// </summary>
    public bool UseCoreProxy { get; set; } = true;
    
    /// <summary>
    /// Flag to set if allowed to look up synthetic users
    /// </summary>
    public bool AllowTestCcrLookup { get; set; } = false;

    /// <summary>
    /// CCR proxy main unit dataset name
    /// </summary>
    public const string CcrProxyMainUnitDatasetName = "_ccrproxymain";
    
    /// <summary>
    /// CCR proxy sub unit dataset name
    /// </summary>
    public const string CcrProxySubUnitDatasetName  = "_ccrproxysub";

    private const string MainUnitLookupEndpoint         = "https://data.brreg.no/enhetsregisteret/api/enheter/{0}";
    private const string SubUnitLookupEndpoint          = "https://data.brreg.no/enhetsregisteret/api/underenheter/{0}";
    private const string PpeMainUnitLookupEndpoint      = "https://data.ppe.brreg.no/enhetsregisteret/api/enheter/{0}";
    private const string PpeSubUnitLookupEndpoint       = "https://data.ppe.brreg.no/enhetsregisteret/api/underenheter/{0}";
    
    private const string ProxyMainUnitLookupEndpoint    = "https://api.data.altinn.no/v1/opendata/" + CcrProxyMainUnitDatasetName + "/{0}";
    private const string ProxySubUnitLookupEndpoint     = "https://api.data.altinn.no/v1/opendata/" + CcrProxySubUnitDatasetName + "/{0}";
    private const string PpeProxyMainUnitLookupEndpoint = "https://test-api.data.altinn.no/v1/opendata/" + CcrProxyMainUnitDatasetName + "/{0}";
    private const string PpeProxySubUnitLookupEndpoint  = "https://test-api.data.altinn.no/v1/opendata/" + CcrProxySubUnitDatasetName + "/{0}";

    private static readonly string[] PublicSectorUnitTypes   = ["ADOS", "FKF", "FYLK", "KF", "KOMM", "ORGL", "STAT", "SF", "SÆR"];
    private static readonly string[] PublicSectorSectorCodes = ["1110", "1120", "1510", "1520", "3900", "6100", "6500"];

    // A list of various organization numbers that the code heuristics fail to recognize as public sector
    private static readonly string[] PublicSectorOrganizations = ["971032146" /*KS-KOMMUNESEKTORENS ORGANISASJON*/];

    private static readonly ConcurrentDictionary<string, (DateTime expiresAt, EntityRegistryUnit? unit)> EntityRegistryUnitsCache = new();
    private static readonly ConcurrentDictionary<string, (DateTime expiresAt, List<EntityRegistryUnit> list)> SubunitListCache = new();

    private readonly TimeSpan cacheEntryTtl = TimeSpan.FromSeconds(600);

    private enum UnitType
    {
        MainUnit,
        SubUnit
    };

    /// <summary>
    /// Gets simple entity registry unit
    /// </summary>
    public async Task<SimpleEntityRegistryUnit?> Get(string organizationNumber, bool attemptSubUnitLookupIfNotFound = true, bool nestToAndReturnMainUnit = false, bool subUnitOnly = false) 
        => MapToEntityRegistryUnit(await GetFull(organizationNumber, attemptSubUnitLookupIfNotFound, nestToAndReturnMainUnit, subUnitOnly));
    

    /// <summary>
    /// Gets simple entity registry main unit
    /// </summary>
    public async Task<SimpleEntityRegistryUnit?> GetMainUnit(string organizationNumber) 
        => await Get(organizationNumber, attemptSubUnitLookupIfNotFound: false, nestToAndReturnMainUnit: true);

    /// <summary>
    /// Get full entity registry unit
    /// </summary>
    public async Task<EntityRegistryUnit?> GetFull(string organizationNumber, bool attemptSubUnitLookupIfNotFound = true,
        bool nestToAndReturnMainUnit = false, bool subUnitOnly = false)
    {

        if (IsSyntheticOrganizationNumber(organizationNumber) && !AllowTestCcrLookup)
        {
            return null;
        }

        EntityRegistryUnit? unit;
        // We only want a subunit, so try that and return regardless
        if (subUnitOnly)
        {
            return await InternalGet(organizationNumber, UnitType.SubUnit);
        }

        // At this point we return a mainunit if we find one
        unit = await InternalGet(organizationNumber, UnitType.MainUnit);
        if (unit != null)
        {
            return unit;
        }
        
        // Didn't find a main unit, check if we should check if it's a subunit
        // or nest to topmost parent
        if (attemptSubUnitLookupIfNotFound || nestToAndReturnMainUnit) {
            unit = await InternalGet(organizationNumber, UnitType.SubUnit);
        }
        
        // If we didn't find any subunit at this point or we're not supposed
        // to nest, return at this point
        if (unit == null || !nestToAndReturnMainUnit)
        {
            return unit;
        }
        
        // We did find a subunit, and we're instructed to nest to the top mainunit
        var parentUnit = unit;
        do
        {
            // Only subunits are at the leaf node, any nested parents are MainUnits
            // Example: https://data.brreg.no/enhetsregisteret/api/underenheter/879587662
            parentUnit = await InternalGet(parentUnit.OverordnetEnhet!, UnitType.MainUnit);
        }
        while (parentUnit?.OverordnetEnhet != null);
        unit = parentUnit;

        return unit;
    }

    /// <summary>
    /// Get full entity registry main unit
    /// </summary>
    public async Task<EntityRegistryUnit?> GetFullMainUnit(string organizationNumber) => await GetFull(organizationNumber, attemptSubUnitLookupIfNotFound: false, nestToAndReturnMainUnit: true);

    /// <summary>
    /// Checks if an entity registry unit is a main unit
    /// </summary>
    public bool IsMainUnit(SimpleEntityRegistryUnit unit)
    {
        return !IsSubUnit(unit);
    }

    /// <summary>
    /// Checks if an entity registry unit is a main unit
    /// </summary>
    public bool IsMainUnit(EntityRegistryUnit unit) => IsMainUnit(MapToEntityRegistryUnit(unit)!);

    /// <summary>
    /// Checks if an entity registry unit is a main unit
    /// </summary>
    public async Task<bool> IsMainUnit(string organizationNumber)
    {
        var unit = await Get(organizationNumber, attemptSubUnitLookupIfNotFound: false);
        return unit != null && IsMainUnit(unit);
    }

    /// <summary>
    /// Checks if an entity registry unit is a sub unit
    /// </summary>
    public bool IsSubUnit(SimpleEntityRegistryUnit unit)
    {
        return !string.IsNullOrEmpty(unit.ParentUnit);
    }

    /// <summary>
    /// Checks if an entity registry unit is a sub unit
    /// </summary>
    public bool IsSubUnit(EntityRegistryUnit unit) => IsSubUnit(MapToEntityRegistryUnit(unit)!);

    /// <summary>
    /// Checks if an entity registry unit is a sub unit
    /// </summary>
    public async Task<bool> IsSubUnit(string organizationNumber)
    {
        var unit = await Get(organizationNumber, subUnitOnly: true);
        return unit != null && IsSubUnit(unit);
    }

    /// <summary>
    /// Checks if an entity registry unit is a public agency
    /// </summary>
    public bool IsPublicAgency(SimpleEntityRegistryUnit unit)
    {
        return PublicSectorUnitTypes.Contains(unit.OrganizationForm)
               || unit.IndustrialCodes != null && unit.IndustrialCodes.Any(x => x.StartsWith("84"))
               || PublicSectorSectorCodes.Contains(unit.SectorCode)
               || PublicSectorOrganizations.Contains(unit.OrganizationNumber);
    }

    /// <summary>
    /// Checks if an entity registry unit is a public agency
    /// </summary>
    public bool IsPublicAgency(EntityRegistryUnit unit) => IsPublicAgency(MapToEntityRegistryUnit(unit)!);

    /// <summary>
    /// Checks if an entity registry unit is a public agency
    /// </summary>
    public async Task<bool> IsPublicAgency(string organizationNumber)
    {
        var unit = await Get(organizationNumber);
        return unit != null && IsPublicAgency(unit);
    }

    private bool IsSyntheticOrganizationNumber(string organizationNumber)
    {
        return organizationNumber.StartsWith("2") || organizationNumber.StartsWith("3");
    }

    private async Task<EntityRegistryUnit?> InternalGet(string organizationNumber, UnitType unitType)
    {
        var cacheKey = Enum.GetName(typeof(UnitType), unitType) + "_" + organizationNumber;
        if (EntityRegistryUnitsCache.TryGetValue(cacheKey, out var cacheEntry) && cacheEntry.expiresAt > DateTime.UtcNow)
        {
            return cacheEntry.unit;
        }

        var urlToFetch = unitType switch
        {
            UnitType.MainUnit => GetLookupUrlForMainUnits(organizationNumber),
            UnitType.SubUnit => GetLookupUrlForSubUnits(organizationNumber),
            _ => throw new InvalidOperationException()
        };

        var entry = (DateTime.UtcNow.Add(cacheEntryTtl), await GetFromClientService(urlToFetch));
        EntityRegistryUnitsCache.AddOrUpdate(cacheKey, entry, (_, _) => entry);

        return entry.Item2;
    }

    private async Task<EntityRegistryUnit?> GetFromClientService(Uri url)
    {
        return await entityRegistryApiClientService.GetUpstreamEntityRegistryUnitAsync(url);
    }

    private SimpleEntityRegistryUnit? MapToEntityRegistryUnit(EntityRegistryUnit? upstreamEntityRegistryUnit)
    {
        if (upstreamEntityRegistryUnit == null) return null;

        var unit = new SimpleEntityRegistryUnit
        {
            OrganizationNumber = upstreamEntityRegistryUnit.Organisasjonsnummer,
            Name = upstreamEntityRegistryUnit.Navn,
            OrganizationForm = upstreamEntityRegistryUnit.Organisasjonsform.Kode,
            ParentUnit = upstreamEntityRegistryUnit.OverordnetEnhet,
            SectorCode = upstreamEntityRegistryUnit.InstitusjonellSektorkode?.Kode,
            IsDeleted = upstreamEntityRegistryUnit.Slettedato is not null
        };

        if (upstreamEntityRegistryUnit.Naeringskode1 != null) unit.IndustrialCodes = new List<string> { upstreamEntityRegistryUnit.Naeringskode1.Kode };
        if (upstreamEntityRegistryUnit.Naeringskode2 != null) unit.IndustrialCodes!.Add(upstreamEntityRegistryUnit.Naeringskode2.Kode);
        if (upstreamEntityRegistryUnit.Naeringskode3 != null) unit.IndustrialCodes!.Add(upstreamEntityRegistryUnit.Naeringskode3.Kode);

        return unit;
    }

    private Uri GetLookupUrlForMainUnits(string organizationNumber)
    {
        string urlPattern;
        if (IsSyntheticOrganizationNumber(organizationNumber))
        {
            urlPattern = UseCoreProxy ? PpeProxyMainUnitLookupEndpoint : PpeMainUnitLookupEndpoint;
        }
        else
        {
            urlPattern = UseCoreProxy ? ProxyMainUnitLookupEndpoint : MainUnitLookupEndpoint;
        }

        return new Uri(string.Format(urlPattern, organizationNumber));
    }

    private Uri GetLookupUrlForSubUnits(string organizationNumber)
    {
        string urlPattern;
        if (IsSyntheticOrganizationNumber(organizationNumber))
        {
            urlPattern = UseCoreProxy ? PpeProxySubUnitLookupEndpoint : PpeSubUnitLookupEndpoint;
        }
        else
        {
            urlPattern = UseCoreProxy ? ProxySubUnitLookupEndpoint : SubUnitLookupEndpoint;
        }

        return new Uri(string.Format(urlPattern, organizationNumber));
    }
    
    private Uri GetLookupUrlForSubunitsOfAUnit(string organizationNumber)
    {
        string urlPattern;
        if (IsSyntheticOrganizationNumber(organizationNumber))
        {
            urlPattern = UseCoreProxy ? PpeProxySubUnitLookupEndpoint : PpeSubUnitLookupEndpoint;
        }
        else
        {
            urlPattern = UseCoreProxy ? ProxySubUnitLookupEndpoint : SubUnitLookupEndpoint;
        }
        
        var query = $"?overordnetEnhet={organizationNumber}";

        return new Uri(string.Format(urlPattern, query));
    }
}
