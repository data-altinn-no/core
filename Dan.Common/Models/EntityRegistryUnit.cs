namespace Dan.Common.Models;
public class EntityRegistryUnit
{
    public string OrganizationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OrganizationForm { get; set; } = string.Empty;
    public string? ParentUnit { get; set; }
    public string SectorCode { get; set; } = string.Empty;
    public List<string> IndustrialCodes { get; set; } = new();
    public bool IsDeleted { get; set; }
}
