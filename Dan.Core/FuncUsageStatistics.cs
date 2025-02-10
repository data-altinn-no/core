using System.Net;
using Dan.Core.Attributes;
using Dan.Core.Extensions;
using Dan.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core;

public class FuncUsageStatistics(IUsageStatisticsService usageStatisticsService)
{
    [Function("MonthlyUsageStatistics"), NoAuthentication]
    public async Task<HttpResponseData> MonthlyUsageStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/month")]
        HttpRequestData req)
    {
        var content = await usageStatisticsService.GetMonthlyUsage();
        var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        return response;
    }
    
    [Function("LastYearUsageStatistics"), NoAuthentication]
    public async Task<HttpResponseData> LastYearUsageStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/lastYear")]
        HttpRequestData req)
    {
        var content = await usageStatisticsService.GetLastYearsUsage();
        var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        return response;
    }
    
    [Function("AllUsageStatistics"), NoAuthentication]
    public async Task<HttpResponseData> AllUsageStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/all")]
        HttpRequestData req)
    {
        var content = await usageStatisticsService.GetAllUsage();
        var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        return response;
    }
}