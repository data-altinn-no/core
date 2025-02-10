namespace Dan.Core.Models;

[Serializable]
public class YearlyUsageStatistics
{
    public string? Time { get; set; }
    public string? ServiceContext { get; set; }
    public long? Count { get; set; }
}