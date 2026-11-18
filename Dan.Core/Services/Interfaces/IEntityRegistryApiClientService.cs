using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// API Service for fetching entity registries
/// </summary>

public interface IEntityRegistryApiClientService
{
    /// <summary>
    /// Get entity registry unit
    /// </summary>
    public Task<EntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri);
}