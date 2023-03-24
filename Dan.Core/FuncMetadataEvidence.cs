using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Dan.Core.Attributes;
using Dan.Core.Helpers;
using Dan.Core.Middleware;

namespace Dan.Core;

/// <summary>
/// Azure Function returning all available evidence codes
/// </summary>
public class FuncMetadataEvidence
{
    private readonly IAvailableEvidenceCodesService _availableEvidenceCodesService;
    private readonly ILogger<FuncMetadataEvidence> _logger;

    public FuncMetadataEvidence(IAvailableEvidenceCodesService availableEvidenceCodesService, ILoggerFactory loggerFactory)
    {
        _availableEvidenceCodesService = availableEvidenceCodesService;
        _logger = loggerFactory.CreateLogger<FuncMetadataEvidence>();
    }

    /// <summary>
    /// Entry point for the Azure function
    /// </summary>
    /// <param name="req">
    /// The HTTP request object
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    [Function("MetadataEvidence"), NoAuthentication]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/evidencecodes")]
        HttpRequestData req)
    {
        using (_logger.Timer("get-evidencecodes-meta"))
        {
            var content = await _availableEvidenceCodesService.GetAvailableEvidenceCodes(req.GetQueryParam("forceRefresh") == Settings.FunctionKeyValue);

            var response = req.CreateExternalResponse(HttpStatusCode.OK, content);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            return response;
        }
    }
#if !DEBUG
    /// <summary>
    /// Runs every five minutes to trigger a forced refresh of all evidence codes. This prevents disruption of calls to the HTTP endpoint,
    /// which will not block while updating.
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <returns></returns>
    [Function("EvidenceCodesListRefresher")]
    public async Task Refresh([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
    {
        await _availableEvidenceCodesService.GetAvailableEvidenceCodes(true);
    }
#endif
}
