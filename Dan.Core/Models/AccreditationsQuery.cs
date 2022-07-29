namespace Dan.Core.Models;

public class AccreditationsQuery
{
    public string? Owner { get; set; }
    public string? ServiceContext { get; set; }
    public string? Requestor { get; set; }
    public DateTime? ChangedAfter { get; set; }
    public bool OnlyAvailableForHarvest { get; set; } = false;
}