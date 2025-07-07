using Dan.Core.Attributes;
using Dan.Core.Extensions;
using Dan.Core.Services;
using Dan.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Parquet.Serialization;
using System.Net;

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

    [Function("DigdirStatistics"), NoAuthentication]
    public async Task<HttpResponseData> DigdirStatistics(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/usage/digdirinternal")]
        HttpRequestData req)
    {
        var content = await usageStatisticsService.GetUsageDataForParquet();

        MemoryStream stream = new MemoryStream();
        await ParquetSerializer.SerializeAsync(content.Records, stream);
        stream.Position = 0;

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("content-type", "application/octet-stream");
        response.Body = stream;
        return response;
    }
}