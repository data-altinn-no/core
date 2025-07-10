using Dan.Core.Attributes;
using Dan.Core.Extensions;
using Dan.Core.Services;
using Dan.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Parquet.Serialization;
using System.Net;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Dan.Core;

public class FuncUsageStatistics
{
    private readonly ILogger _logger;
    private readonly IUsageStatisticsService _usageStatisticsService;

    public FuncUsageStatistics(IUsageStatisticsService usageStatisticsService, ILoggerFactory factory) 
    {
        _logger = factory.CreateLogger<FuncUsageStatistics>();
        _usageStatisticsService = usageStatisticsService;
    }

    [Function("MonthlyUsageStatistics"), NoAuthentication]
    public async Task<HttpResponseData> MonthlyUsageStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/month")]
       HttpRequestData req)
    {
        var content = await _usageStatisticsService.GetMonthlyUsage();
        var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
        response.Headers.Add("Access-Control-Allow-Origin", "*");

        _logger.LogInformation("Monthly usage statistics retrieved successfully.");
        return response;
    }

    [Function("LastYearUsageStatistics"), NoAuthentication]
    public async Task<HttpResponseData> LastYearUsageStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/lastYear")]
       HttpRequestData req)
    {
        var content = await _usageStatisticsService.GetLastYearsUsage();
        var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
        response.Headers.Add("Access-Control-Allow-Origin", "*");

        _logger.LogInformation("Last year's usage statistics retrieved successfully.");
        return response;
    }

    [Function("AllUsageStatistics"), NoAuthentication]
    public async Task<HttpResponseData> AllUsageStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/all")]
       HttpRequestData req)
    {
        var content = await _usageStatisticsService.GetAllUsage();
        var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
        response.Headers.Add("Access-Control-Allow-Origin", "*");

        _logger.LogInformation("All usage statistics retrieved successfully.");
        return response;
    }

    [Function("DigdirStatistics"), NoAuthentication]
    public async Task<HttpResponseData> DigdirStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/digdir")]
       HttpRequestData req)
    {
        var content = await _usageStatisticsService.GetLast24HoursPerServiceContext();

        MemoryStream stream = new MemoryStream();
        await ParquetSerializer.SerializeAsync(content.Records, stream);
        stream.Position = 0;

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("content-type", "application/octet-stream");
        response.Body = stream;

        _logger.LogInformation("Digdir usage statistics retrieved successfully.");
        return response;
    }
}
