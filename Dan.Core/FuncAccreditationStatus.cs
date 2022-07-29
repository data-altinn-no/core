using System.Net;
using Dan.Common.Helpers.Util;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core;

/// <summary>
/// The Azure function returning the status for the provided accreditation reference.
/// </summary>
public class FuncAccreditationStatus
{
    private readonly IEvidenceStatusService _evidenceStatusService;
    private readonly IRequestContextService _requestContextService;
    private readonly IAccreditationRepository _accreditationRepository;

    public FuncAccreditationStatus(
        IEvidenceStatusService evidenceStatusService, 
        IRequestContextService requestContextService,
        IAccreditationRepository accreditationRepository)
    {
        _evidenceStatusService = evidenceStatusService;
        _requestContextService = requestContextService;
        _accreditationRepository = accreditationRepository;
    }

    /// <summary>
    /// The entry point for the Azure function.
    /// </summary>
    /// <param name="req">
    /// The HTTP request object.
    /// </param>
    /// <param name="accreditationId">
    /// The accreditation id to check.
    /// </param>
    /// <returns>
    /// The <see cref="HttpResponseData"/>.
    /// </returns>
    [Function("EvidenceStatus")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "evidence/{accreditationId}")]
        HttpRequestData req,
        string accreditationId)
    {
        await _requestContextService.BuildRequestContext(req);
        var accreditation = await _accreditationRepository.GetAccreditationAsync(accreditationId, _requestContextService);
        if (accreditation == null)
        {
            throw new NonExistentAccreditationException();
        }

        var accreditationStatus = await _evidenceStatusService.GetEvidenceStatusListAsync(accreditation);
        return req.CreateExternalResponse(HttpStatusCode.OK, accreditationStatus);
    }
}