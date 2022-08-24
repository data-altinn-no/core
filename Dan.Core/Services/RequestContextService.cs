using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core.Services;

class RequestContextService : IRequestContextService
{
    private readonly IServiceContextService _serviceContextService;

    public string AuthenticatedOrgNumber { get; set; } = string.Empty;
    public string SubscriptionKey { get; set; } = string.Empty;
    public List<string>? Scopes { get; set; }
    public ServiceContext ServiceContext { get; set; } = new();
    public HttpRequestData? Request { get; set; }

    public const string ServicecontextHeader = "X-NADOBE-SERVICECONTEXT";
    public const string QueryParamTokenOnBehalfOf = "tokenonbehalfof";
    public const string QueryParamReuseToken = "reusetoken";
    public const string RequestHeaderForwardAccessToken = "X-Forward-Access-Token";

    public RequestContextService(IServiceContextService serviceContextService)
    {
        _serviceContextService = serviceContextService;
    }

    public async Task BuildRequestContext(HttpRequestData request)
    {
        AuthenticatedOrgNumber = request.GetAuthenticatedPartyOrgNumber();
        SubscriptionKey = request.GetSubscriptionKey() ?? "(unknown)";
        Scopes = request.GetMaskinportenScopes();
        Request = request;
        
        var serviceContext = GetServiceContextFromRequest(request);
        var serviceContexts = await _serviceContextService.GetRegisteredServiceContexts();
        var foundServiceContext = serviceContexts.Find(c => c.Id.ToLowerInvariant() == serviceContext);

        ServiceContext = foundServiceContext ?? throw new InternalServerErrorException($"Unknown service context identifier: {foundServiceContext}. Known: {string.Join(", ", serviceContexts.Select(x => x.Id))}");
    }

    private string GetServiceContextFromRequest(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues(ServicecontextHeader, out var header))
            throw new ServiceNotAvailableException("Missing Service Context definition in request.");

        return header.First().ToLowerInvariant();
    }

    public EvidenceHarvesterOptions GetEvidenceHarvesterOptionsFromRequest()
    {
        if (Request == null)
        {
            throw new ArgumentNullException(nameof(Request));
        }

        return new EvidenceHarvesterOptions
        {
            OverriddenAccessToken = Request.Headers.Get(RequestHeaderForwardAccessToken),
            ReuseClientAccessToken = Request.GetBoolQueryParam(QueryParamReuseToken),
            FetchSupplierAccessTokenOnBehalfOfOwner = Request.GetBoolQueryParam(QueryParamTokenOnBehalfOf)
        };
    }
}