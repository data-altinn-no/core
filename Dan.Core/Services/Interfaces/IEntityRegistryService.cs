using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Service for handling entity registry
/// </summary>
public interface IEntityRegistryService
{
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
    /// Gets the organization number from ER.
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <param name="attemptSubUnitLookupIfNotFound">Will attempt to lookup a sub unit if main unit is not found</param>
    /// <param name="nestToAndReturnMainUnit">If subunit, will nest up to uppermost parent and return that instead</param>
    /// <param name="subUnitOnly">Will skip checking for main unit, and only return a subunit if it's found</param>
    /// <returns>A full model from ER containing all the fields from upstream</returns>
    Task<EntityRegistryUnit?> GetFull(string organizationNumber, bool attemptSubUnitLookupIfNotFound = true, bool nestToAndReturnMainUnit = false, bool subUnitOnly = false);

    /// <summary>
    /// Returns true if the unit is determined to be a public agency
    /// </summary>
    /// <param name="organizationNumber">The organizationNumber</param>
    /// <returns>True if public agency</returns>
    Task<bool> IsPublicAgency(string organizationNumber);
}