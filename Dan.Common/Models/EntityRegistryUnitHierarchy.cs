namespace Dan.Common.Models;

/// <summary>
/// Represents a hierarchical structure of entity registry units with their subunits
/// </summary>
public class EntityRegistryUnitHierarchy
{
    /// <summary>
    /// The organization number of the unit
    /// </summary>
    public string? OrgNumber { get; set; }
    
    /// <summary>
    /// The entity registry unit details
    /// </summary>
    public EntityRegistryUnit? Unit { get; set; }
    
    /// <summary>
    /// List of subunits in the hierarchy
    /// </summary>
    public List<EntityRegistryUnitHierarchy>? Subunits { get; set; }
}