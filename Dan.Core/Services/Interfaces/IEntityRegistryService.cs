using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

public interface IEntityRegistryService
{
    /// <summary>
    /// Gets either main or sub unit from BR
    /// </summary>
    /// <param name="orgNumber">The organization number</param>
    /// <returns>The organization entry from ER, null if not found</returns>
    public Task<BREntityRegisterEntry> GetOrganizationEntry(string orgNumber);

    /// <summary>
    /// Uses various heuristics to determine if a organization is a public agency
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <returns>True if public agency; otherwise false</returns>
    public Task<bool> IsOrganizationPublicAgency(string organizationNumber);
}
