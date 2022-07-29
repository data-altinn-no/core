using Dan.Common.Helpers.Util;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Dan.Core.Attributes;
using Dan.Core.Middleware;

namespace Dan.Core;

public class FuncMetadataEvidenceServiceContext
{
    private readonly IAvailableEvidenceCodesService _evidenceCodesService;
    private readonly ILogger<FuncMetadataEvidenceServiceContext> _logger;

    public FuncMetadataEvidenceServiceContext(IAvailableEvidenceCodesService evidenceCodesService, ILoggerFactory loggerFactory)
    {
        _evidenceCodesService = evidenceCodesService;
        _logger = loggerFactory.CreateLogger<FuncMetadataEvidenceServiceContext>();
    }

    /// <summary>
    /// Entry point for the Azure function
    /// </summary>
    /// <param name="req">
    /// The HTTP request object
    /// </param>
    /// <param name="serviceContext">
    /// The service domain
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    [Function("MetadataEvidenceServiceContext"), NoAuthentication]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/evidencecodes/{servicecontext}")]
        HttpRequestData req,
        string serviceContext)
    {
        using (_logger.Timer("get-evidencecodes-meta-servicecontext"))
        {
            HttpResponseData response;

            var evidenceCodes = await _evidenceCodesService.GetAvailableEvidenceCodes();
            var result = new List<EvidenceCode>();

            foreach (var ec in evidenceCodes.Where(ec => ec.GetBelongsToServiceContexts()
                         .Contains(serviceContext, StringComparer.OrdinalIgnoreCase)))
            {
                //  Add some metadata properties to make serialized output more parseable
                ec.AuthorizationRequirements?.ForEach(x => x.RequirementType = x.GetType().Name);
                result.Add(ec);
            }

            if (evidenceCodes.Count == 0)
            {
                response = req.CreateResponse(HttpStatusCode.NotFound);
                return response;
            }

            response = req.CreateExternalResponse(HttpStatusCode.OK, result);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            return response;
        }

    }
}