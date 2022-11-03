namespace Dan.Common.Interfaces;
public interface IEntityRegistryService
{
    public bool UseCoreProxy { get; set; }
    public bool AllowTestCcrLookup { get; set; }

    Task<EntityRegistryUnit?> Get(string organizationNumber, bool attemptSubUnitLookupIfNotFound = true, bool nestToAndReturnMainUnit = false, bool checkIfSubUnitFirst = false);
    Task<EntityRegistryUnit?> GetMainUnit(string organizationNumber);
    bool IsMainUnit(EntityRegistryUnit unit);
    Task<bool> IsMainUnit(string organizationNumber);
    bool IsSubUnit(EntityRegistryUnit unit);
    Task<bool> IsSubUnit(string organizationNumber);
    bool IsPublicAgency(EntityRegistryUnit unit);
    Task<bool> IsPublicAgency(string organizationNumber);
}
