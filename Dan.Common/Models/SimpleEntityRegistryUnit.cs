namespace Dan.Common.Models;

#pragma warning disable 1591
public class SimpleEntityRegistryUnit
{
    public string OrganizationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OrganizationForm { get; set; } = string.Empty;
    public string? ParentUnit { get; set; }
    public string? SectorCode { get; set; }
    public List<string>? IndustrialCodes { get; set; }
    public bool IsDeleted { get; set; }
}
#pragma warning restore 1591