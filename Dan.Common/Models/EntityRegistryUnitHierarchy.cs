namespace Dan.Common.Models;

public class EntityRegistryUnitHierarchy
{
    public string OrgNumber { get; set; }
    
    public EntityRegistryUnit Unit { get; set; }
    
    public List<EntityRegistryUnitHierarchy> Subunits { get; set; }
}