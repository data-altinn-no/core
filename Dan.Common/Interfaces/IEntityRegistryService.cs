namespace Dan.Common.Interfaces;

/// <summary>
/// Service for handling entity registry
/// </summary>
[Obsolete("Use Dan.Common.Services.ICcrClientService instead.")]
public interface IEntityRegistryService
{
    /// <summary>
    /// This controls whether the service should call the proxy in Dan.Core or ER directory. Should be true for plugins and false for Core.
    /// </summary>
    public bool UseCoreProxy { get; set; }

    /// <summary>
    /// Controls whether lookups on synthetic (Tenor) organization numbers are allowed. Should be false in production.
    /// </summary>
    public bool AllowTestCcrLookup { get; set; }

    /// <summary>
    /// Gets the organization number from ER.
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <param name="attemptSubUnitLookupIfNotFound">Will attempt to lookup a sub unit if main unit is not found</param>
    /// <param name="nestToAndReturnMainUnit">If subunit, will nest up to uppermost parent and return that instead</param>
    /// <param name="subUnitOnly">Will skip checking for main unit, and only return a subunit if it's found</param>
    /// <returns>A simplified model from ER suitable for most verification purposes</returns>
    Task<SimpleEntityRegistryUnit?> Get(string organizationNumber, bool attemptSubUnitLookupIfNotFound = true, bool nestToAndReturnMainUnit = false, bool subUnitOnly = false);

    /// <summary>
    /// Gets the uppermost parent for the given organization number
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <returns>A simplified model from ER suitable for most verification purposes</returns>
    Task<SimpleEntityRegistryUnit?> GetMainUnit(string organizationNumber);

    /// <summary>
    /// Gets the organization number from ER.
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <param name="attemptSubUnitLookupIfNotFound">Will attempt to lookup a sub unit if main unit is not found</param>
    /// <param name="nestToAndReturnMainUnit">If subunit, will nest up to uppermost parent and return that instead</param>
    /// <param name="subUnitOnly">Will skip checking for main unit, and only return a subunit if it's found</param>
    /// <returns>A full model from ER containing all the fields from upstream</returns>
    Task<EntityRegistryUnit?> GetFull(string organizationNumber, bool attemptSubUnitLookupIfNotFound = true, bool nestToAndReturnMainUnit = false, bool subUnitOnly = false);

    /// <summary>
    /// Gets the uppermost parent for the given organization number
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <returns>A full model from ER containing all the fields from upstream</returns>
    Task<EntityRegistryUnit?> GetFullMainUnit(string organizationNumber);

    /// <summary>
    /// Returns true if the unit is a main unit
    /// </summary>
    /// <param name="unit">The unit</param>
    /// <returns>True if main unit</returns>
    bool IsMainUnit(SimpleEntityRegistryUnit unit);

    /// <summary>
    /// Returns true if the unit is a main unit
    /// </summary>
    /// <param name="unit">The unit</param>
    /// <returns>True if main unit</returns>
    bool IsMainUnit(EntityRegistryUnit unit);

    /// <summary>
    /// Returns true if the unit is a main unit
    /// </summary>
    /// <param name="organizationNumber">The organizationNumber</param>
    /// <returns>True if main unit</returns>
    Task<bool> IsMainUnit(string organizationNumber);

    /// <summary>
    /// <param name="unit">The unit</param>
    /// <returns>True if sub unit</returns>
    /// </summary>
    bool IsSubUnit(SimpleEntityRegistryUnit unit);

    /// <summary>
    /// <param name="unit">The unit</param>
    /// <returns>True if sub unit</returns>
    /// </summary>
    bool IsSubUnit(EntityRegistryUnit unit);

    /// <summary>
    /// <param name="organizationNumber">The organizationNumber</param>
    /// <returns>True if sub unit</returns>
    /// </summary>
    Task<bool> IsSubUnit(string organizationNumber);

    /// <summary>
    /// Returns true if the unit is determined to be a public agency
    /// </summary>
    /// <param name="unit">The unit</param>
    /// <returns>True if public agency</returns>
    bool IsPublicAgency(SimpleEntityRegistryUnit unit);

    /// <summary>
    /// Returns true if the unit is determined to be a public agency
    /// </summary>
    /// <param name="unit">The unit</param>
    /// <returns>True if public agency</returns>
    bool IsPublicAgency(EntityRegistryUnit unit);

    /// <summary>
    /// Returns true if the unit is determined to be a public agency
    /// </summary>
    /// <param name="organizationNumber">The organizationNumber</param>
    /// <returns>True if public agency</returns>
    Task<bool> IsPublicAgency(string organizationNumber);
}
