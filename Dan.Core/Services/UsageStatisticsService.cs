using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Dan.Core.Config;
using Dan.Core.Models;

namespace Dan.Core.Services;

public interface IUsageStatisticsService
{
    Task<List<MonthlyUsageStatistics>> GetMonthlyUsage();
    Task<List<IGrouping<string?, YearlyUsageStatistics>>> GetLastYearsUsage();
    Task<List<IGrouping<string?, YearlyUsageStatistics>>> GetAllUsage();
}

public class UsageStatisticsService : IUsageStatisticsService
{
    private const string CountKey = "Antall";
    private const string ServiceContextKey = "ServiceContext";
    private const string SortingfieldKey = "Sortingfield";
    
    private readonly LogsQueryClient _logsQueryClient = new(new DefaultAzureCredential());

    public async Task<List<MonthlyUsageStatistics>> GetMonthlyUsage()
    {
        const int days = 30;
        var queryTime = GetQueryTime(QueryTimeType.Monthly, days);
        var groupingValue = GetQueryGrouping(QueryTimeType.Monthly);
        var results = await _logsQueryClient.QueryResourceAsync(
            new ResourceIdentifier(Settings.ApplicationInsightsResourceId),
            GetQuery(queryTime, groupingValue),
            new QueryTimeRange(TimeSpan.FromDays(days))); 
        var rows = results.Value.Table.Rows;
        var usage = rows
            .Select(r => 
                new MonthlyUsageStatistics
                {
                    ServiceContext = r[ServiceContextKey].ToString(),
                    Count = r[CountKey] as long?
                })
            .ToList();
        return usage;
    }
    
    public async Task<List<IGrouping<string?,YearlyUsageStatistics>>> GetLastYearsUsage()
    {
        var lastYear = DateTime.UtcNow.Year - 1;
        var lastJanuaryFirst = new DateTime(lastYear, 1, 1);
        var timeSinceLastYearsStart = lastJanuaryFirst - DateTime.UtcNow;
        var queryTime = GetQueryTime(QueryTimeType.Yearly, lastYear);
        var groupingValue = GetQueryGrouping(QueryTimeType.Yearly);
        
        var results = await _logsQueryClient.QueryResourceAsync(
            new ResourceIdentifier(Settings.ApplicationInsightsResourceId),
            GetQuery(queryTime, groupingValue),
            new QueryTimeRange(timeSinceLastYearsStart)); 
        var rows = results.Value.Table.Rows;
        var usage = rows
            .Select(r => 
                new YearlyUsageStatistics
                {
                    Time = r[SortingfieldKey].ToString(),
                    ServiceContext = r[ServiceContextKey].ToString(),
                    Count = r[CountKey] as long?
                })
            .ToList();
        var grouped = usage.GroupBy(u => u.Time).ToList();
        return grouped;
    }

    public async Task<List<IGrouping<string?, YearlyUsageStatistics>>> GetAllUsage()
    {
        var queryTime = GetQueryTime(QueryTimeType.All, default);
        var groupingValue = GetQueryGrouping(QueryTimeType.All);
        
        var results = await _logsQueryClient.QueryResourceAsync(
            new ResourceIdentifier(Settings.ApplicationInsightsResourceId),
            GetQuery(queryTime, groupingValue),
            QueryTimeRange.All); 
        var rows = results.Value.Table.Rows;
        var usage = rows
            .Select(r => 
                new YearlyUsageStatistics
                {
                    Time = r[SortingfieldKey].ToString(),
                    ServiceContext = r[ServiceContextKey].ToString(),
                    Count = r[CountKey] as long?
                })
            .ToList();
        var grouped = usage.GroupBy(u => u.Time).ToList();
        return grouped;
    }

    private static string GetQuery(string timeValue, string groupingValue)
    {
        // First bit is just to make the sorting field out result more readable
        return $$"""
               let f=(a: int, b:int) {
                   case(
                       a == 1,
                       strcat("Januar ", b),
                       a == 2,
                       strcat("Februar ", b),
                       a == 3,
                       strcat("Mars ", b),
                       a == 4,
                       strcat("April ", b),
                       a == 5,
                       strcat("Mai ", b),
                       a == 6,
                       strcat("Juni ", b),
                       a == 7,
                       strcat("Juli ",  b),
                       a == 8,
                       strcat("August ",  b),
                       a == 9,
                       strcat("September ",  b),
                       a == 10,
                       strcat("Oktober " ,  b),
                       a == 11,
                       strcat("November ",  b),
                       a == 12,
                       strcat("Desember ",  b),
                       "Error"
                   )
               };
               traces
               {{timeValue}}
               | where cloud_RoleName == "{{Settings.ApplicationInsightsCloudRoleName}}"
               | where tostring(customDimensions["action"]) == "DatasetRetrieved"
               | project timestamp = timestamp, action = tostring(customDimensions["action"]), dataset = tostring(customDimensions["evidenceCodeName"]), {{ServiceContextKey}} = tostring(customDimensions["serviceContext"]), Konsument = tostring(customDimensions["requestor"])
               | summarize {{CountKey}}=count() by {{groupingValue}}
               | order by {{ServiceContextKey}}
               """;
    }

    private static string GetQueryTime(QueryTimeType timeType, int value)
    {
        return timeType switch
        {
            QueryTimeType.Monthly => $"| where timestamp > ago({value}d)",
            QueryTimeType.Yearly => $"| where getyear(timestamp) == {value}",
            QueryTimeType.All => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(timeType), timeType, null)
        };
    }

    private static string GetQueryGrouping(QueryTimeType timeType)
    {
        return timeType switch
        {
            QueryTimeType.Monthly => $"{ServiceContextKey}",
            QueryTimeType.Yearly => $"{SortingfieldKey}=f(getmonth(timestamp), getyear(timestamp)), {ServiceContextKey}",
            QueryTimeType.All => $"{SortingfieldKey}=f(getmonth(timestamp), getyear(timestamp)), {ServiceContextKey}",
            _ => throw new ArgumentOutOfRangeException(nameof(timeType), timeType, null)
        };
    }

    private enum QueryTimeType
    {
        Monthly,
        Yearly,
        All
    }
}