namespace Dan.Core.Models;

[Serializable]
public class MonthlyUsageStatistics
{
    public string? ServiceContext { get; set; }
    public long? Count { get; set; }
}