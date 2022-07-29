using System.Net;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core;

/// <summary>
/// Function to delete an accreditation
/// </summary>
public class FuncAccreditationDelete
{
    private readonly IRequestContextService _requestContextService;
    private readonly IAccreditationRepository _accreditationRepository;

    public FuncAccreditationDelete(IRequestContextService requestContextService, IAccreditationRepository accreditationRepository)
    {
        _requestContextService = requestContextService;
        _accreditationRepository = accreditationRepository;
    }
    /// <summary>
    /// HTTP trigger handler for deleting accreditations
    /// </summary>
    /// <param name="req">The HTTP request</param>
    /// <param name="accreditationId">The requested accreditation to delete</param>
    /// <returns>204 on success, 404 on error</returns>
    [Function("FuncAccreditationDelete")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "accreditations/{accreditationId}")]
        HttpRequestData req,
        string accreditationId)
    {
        await _requestContextService.BuildRequestContext(req);

        var accreditation = await _accreditationRepository.GetAccreditationAsync(accreditationId, _requestContextService);
        if (accreditation == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        await _accreditationRepository.DeleteAccreditationAsync(accreditationId);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}