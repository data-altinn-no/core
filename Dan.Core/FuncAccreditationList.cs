using System.Net;
using Dan.Core.Extensions;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core;

/// <summary>
/// The Azure function rendering the list of accreditations for the given client certificate
/// </summary>
public class FuncAccreditationList
{
    private readonly IRequestContextService _requestContextService;
    private readonly IAccreditationRepository _accreditationRepository;

    /// <summary>
    /// Creates an instance of <see cref="FuncAccreditationList"/>
    /// </summary>
    /// <param name="requestContextService"></param>
    /// <param name="accreditationRepository"></param>
    public FuncAccreditationList(IRequestContextService requestContextService, IAccreditationRepository accreditationRepository)
    {
        _requestContextService = requestContextService;
        _accreditationRepository = accreditationRepository;
    }

    /// <summary>
    /// Returns a list of accreditations owned by the organization number in the supplied enterprise certificate
    /// </summary>
    /// <param name="req">
    /// The HTTP request.
    /// </param>
    /// <returns>
    /// The list of non-expired accreditations owned by the current authenticated organization.
    /// </returns>
    [Function("Accreditation")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accreditations")]
        HttpRequestData req)
    {
        await _requestContextService.BuildRequestContext(req);

        var accreditationsQuery = new AccreditationsQuery
        {
            Requestor = req.GetQueryParam("requestor"),
            OnlyAvailableForHarvest = req.GetBoolQueryParam("onlyavailable")
        };

        if (DateTime.TryParse(req.GetQueryParam("changedafter"), out DateTime changedAfter))
        {
            accreditationsQuery.ChangedAfter = changedAfter;
        }

        var accreditations =
            await _accreditationRepository.QueryAccreditationsAsync(accreditationsQuery, _requestContextService);

        return req.CreateExternalResponse(HttpStatusCode.OK, accreditations);
    }
}