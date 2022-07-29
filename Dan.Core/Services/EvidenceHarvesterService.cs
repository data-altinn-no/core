using Dan.Common.Enums;
using Dan.Common.Helpers.Extensions;
using Dan.Common.Helpers.Util;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace Dan.Core.Services;

public class EvidenceHarvesterService : IEvidenceHarvesterService
{
    private readonly ILogger<EvidenceHarvesterService> _log;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConsentService _consentService;
    private readonly IEvidenceStatusService _evidenceStatusService;
    private readonly ITokenRequesterService _tokenRequesterService;
    private readonly IRequestContextService _requestContextService;

    public const string QueryParamTokenOnBehalfOf = "tokenonbehalfof";
    public const string QueryParamTokenOnBehalfOfOwner = "owner";
    public const string QueryParamTokenOnBehalfOfRequestor = "requestor";
    public const string QueryParamTokenOnBehalfOfSubject = "subject";
    public const string QueryParamReuseToken = "reusetoken";
    public const string RequestHeaderForwardAccessToken = "X-Forward-Access-Token";


    public EvidenceHarvesterService(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConsentService consentService, IEvidenceStatusService evidenceStatusService, ITokenRequesterService tokenRequesterService, IRequestContextService requestContextService)
    {
        _log = loggerFactory.CreateLogger<EvidenceHarvesterService>();
        _httpClientFactory = httpClientFactory;
        _consentService = consentService;
        _evidenceStatusService = evidenceStatusService;
        _tokenRequesterService = tokenRequesterService;
        _requestContextService = requestContextService;
    }

    public async Task<Evidence> Harvest(string evidenceCodeName, Accreditation accreditation, EvidenceHarvesterOptions? evidenceHarvesterOptions = default)
    {
        var evidenceCode = accreditation.GetValidEvidenceCode(evidenceCodeName);

        _log.LogInformation("Start get evidence status | aid={accreditationId}, evidenceCode={evidenceCodeName}", accreditation.AccreditationId, evidenceCode.EvidenceCodeName);
        var evidenceStatus = await _evidenceStatusService.GetEvidenceStatusAsync(accreditation, evidenceCode, false);

        ThrowIfNotAvailableForHarvest(evidenceStatus);

        List<EvidenceValue> harvestedEvidence;
        using (var _ = _log.Timer($"{evidenceCode.EvidenceCodeName}-harvest"))
        {
            _log.LogInformation("Start harvesting evidence values | aid={accreditationId}, status={evidenceStatus}, evidenceCode={evidenceCodeName}",
             accreditation.AccreditationId, evidenceStatus.Status.Description, evidenceCode.EvidenceCodeName
                );
            harvestedEvidence = await HarvestEvidenceValues(evidenceCode, accreditation, evidenceHarvesterOptions);
            _log.LogInformation("Completed harvesting evidence values | aid={accreditationId}", accreditation.AccreditationId);
        }

        var evidence = new Evidence()
        {
            EvidenceStatus = evidenceStatus,
            EvidenceValues = harvestedEvidence
        };
        return evidence;
    }

    public async Task<Evidence> HarvestOpenData(EvidenceCode evidenceCode, string identifier = "")
    {
        _log.LogDebug("Running HaaS (Harvest as a Service) for open data with dataset {evidenceCodeName} and identifier {identifier}", evidenceCode.EvidenceCodeName, identifier == "" ? "(empty)" : identifier);
        List<EvidenceValue> harvestedEvidence;
        var url = evidenceCode.GetEvidenceSourceUrl();

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        var evidenceHarvesterRequest = new EvidenceHarvesterRequest()
        {
            OrganizationNumber = identifier,
            SubjectParty = PartyParser.GetPartyFromIdentifier(identifier, out string _),
            EvidenceCodeName = evidenceCode.EvidenceCodeName,
            Parameters = evidenceCode.Parameters
        };

        using (var _ = _log.Timer($"{evidenceCode.EvidenceCodeName}-harvest"))
        {
            _log.LogInformation("Start harvesting evidence values for open data evidenceCode={evidenceCodeName} and identifier {identifier}", evidenceCode.EvidenceCodeName, identifier == "" ? "(empty)" : identifier);
            request.JsonContent(evidenceHarvesterRequest);
            request.SetPolicyExecutionContext(new Context(request.Key(CacheArea.Absolute)));
            var client = _httpClientFactory.CreateClient("SafeHttpClient");
            harvestedEvidence = (await EvidenceSourceHelper.DoRequest<List<EvidenceValue>>(
                request,
                () => client.SendAsync(request, cts.Token)))!;

            _log.LogInformation("Completed harvesting evidence values for open data evidenceCode={evidenceCodeName} and identifier {identifier}", evidenceCode.EvidenceCodeName, identifier == "" ? "(empty)" : identifier);
        }

        var evidence = new Evidence()
        {
            EvidenceStatus = new EvidenceStatus() { EvidenceCodeName = evidenceCode.EvidenceCodeName, Status = EvidenceStatusCode.Available, ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow },
            EvidenceValues = harvestedEvidence
        };

        return evidence;
    }

    public EvidenceHarvesterOptions GetEvidenceHarvesterOptionsFromRequest()
    {
        var options = new EvidenceHarvesterOptions
        {
            OverriddenAccessToken = _requestContextService.Request.Headers.Get(RequestHeaderForwardAccessToken),
            ReuseClientAccessToken = _requestContextService.Request.GetBoolQueryParam(QueryParamReuseToken)
        };

        var onbehalfof = _requestContextService.Request.GetQueryParam(QueryParamTokenOnBehalfOf);
        if (onbehalfof == null)
        {
            return options;
        }

        switch (onbehalfof.ToLowerInvariant())
        {
            case QueryParamTokenOnBehalfOfOwner:
                options.FetchSupplierAccessTokenOnBehalfOf = AccreditationPartyTypes.Owner;
                break;
            case QueryParamTokenOnBehalfOfRequestor:
                options.FetchSupplierAccessTokenOnBehalfOf = AccreditationPartyTypes.Requestor;
                break;
            case QueryParamTokenOnBehalfOfSubject:
                options.FetchSupplierAccessTokenOnBehalfOf = AccreditationPartyTypes.Subject;
                break;
        }

        return options;
    }

    private async Task<List<EvidenceValue>> HarvestEvidenceValues(EvidenceCode evidenceCode, Accreditation accreditation, EvidenceHarvesterOptions? evidenceHarvesterOptions = default)
    {
        var url = evidenceCode.GetEvidenceSourceUrl();

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var evidenceHarvesterRequest = new EvidenceHarvesterRequest()
        {
            OrganizationNumber = accreditation.Subject,
            SubjectParty = accreditation.SubjectParty,
            Requestor = accreditation.Requestor,
            RequestorParty = accreditation.RequestorParty,
            ServiceContext = accreditation.ServiceContext,
            EvidenceCodeName = evidenceCode.EvidenceCodeName,
            Parameters = evidenceCode.Parameters,
            AccreditationId = accreditation.AccreditationId,
        };

        if (!string.IsNullOrEmpty(evidenceCode.RequiredScopes))
        {
            evidenceHarvesterRequest.MPToken = await GetAccessToken(evidenceCode, accreditation, evidenceHarvesterOptions ?? new EvidenceHarvesterOptions());
        }

        if (_consentService.EvidenceCodeRequiresConsent(evidenceCode))
        {
            evidenceHarvesterRequest.JWT = await GetConsentToken(evidenceCode, accreditation);
        }

        if (evidenceCode.IsAsynchronous)
        {
            evidenceHarvesterRequest.AsyncEvidenceCodeAction = AsyncEvidenceCodeAction.Harvest;
        }

        request.JsonContent(evidenceHarvesterRequest);

        request.SetPolicyExecutionContext(new Context(request.Key(CacheArea.Absolute)));
        var client = _httpClientFactory.CreateClient("SafeHttpClient");
        return (await EvidenceSourceHelper.DoRequest<List<EvidenceValue>>(
            request,
            () => client.SendAsync(request, cts.Token)))!;
    }

    private async Task<string> GetAccessToken(EvidenceCode evidenceCode, Accreditation accreditation, EvidenceHarvesterOptions evidenceHarvesterOptions)
    {
        if (evidenceHarvesterOptions.ReuseClientAccessToken && _requestContextService.Request.GetAuthorizationToken() != null)
        {
            return _requestContextService.Request.GetAuthorizationToken();
        }

        if (evidenceHarvesterOptions.OverriddenAccessToken != null)
        {
            return evidenceHarvesterOptions.OverriddenAccessToken;
        }

        var metricName = evidenceHarvesterOptions.FetchSupplierAccessTokenOnBehalfOf.HasValue
            ? "mp-token-fetch-supplier"
            : "mp-token-fetch";

        using (var _ = _log.Timer(metricName))
        {
            _log.LogInformation(
                "Getting mp-token | aid={accreditationId}, evidenceCode={evidenceCodeName}, requiredScopes={requiredScopes}, onbehalfof={onbehalfof}",
                accreditation.AccreditationId,
                evidenceCode.EvidenceCodeName,
                evidenceCode.RequiredScopes,
                evidenceHarvesterOptions.FetchSupplierAccessTokenOnBehalfOf.HasValue
                    ? Enum.GetName(typeof(AccreditationPartyTypes),
                        evidenceHarvesterOptions.FetchSupplierAccessTokenOnBehalfOf)
                    : "self");

            var token = await _tokenRequesterService.GetMaskinportenToken(evidenceCode.RequiredScopes, GetConsumerOrg(accreditation, evidenceHarvesterOptions));

            _log.LogInformation("Completed getting mp-token | aid={accreditationId}, token={token}",
                accreditation.AccreditationId, token);

            if (string.IsNullOrEmpty(token))
            {
                _log.LogWarning(
                    $"Failed getting maskinporten token for requiredScopes={evidenceCode.RequiredScopes}");
                throw new ServiceNotAvailableException(
                    "Temporarily unable to retrieve authentication token from third party service");
            }

            var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(token);
            return response["access_token"];
        }
    }

    private async Task<string> GetConsentToken(EvidenceCode evidenceCode, Accreditation accreditation)
    {
        using (var _ = _log.Timer("jwt-fetch"))
        {
            _log.LogInformation(
                "Getting JWT | aid={accreditationId}, evidenceCode={evidenceCodeName}, authCode={authorizationCode}",
                accreditation.AccreditationId, evidenceCode.EvidenceCodeName, accreditation.AuthorizationCode);

            string jwt = await _consentService.GetJwt(accreditation);

            _log.LogInformation(
                "Completed JWT | aid={accreditationId}, evidenceCode={evidenceCodeName}, authCode={authorizationCode}, jwt={jwt}",
                accreditation.AccreditationId, evidenceCode.EvidenceCodeName, accreditation.AuthorizationCode,
                jwt);

            return jwt;
        }
    }

    private static string? GetConsumerOrg(Accreditation accreditation,
        EvidenceHarvesterOptions evidenceHarvesterOptions)
    {
        return evidenceHarvesterOptions.FetchSupplierAccessTokenOnBehalfOf switch
        {
            AccreditationPartyTypes.Owner => accreditation.Owner,
            AccreditationPartyTypes.Requestor => accreditation.RequestorParty.NorwegianOrganizationNumber,
            AccreditationPartyTypes.Subject => accreditation.SubjectParty.NorwegianOrganizationNumber,
            null => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static void ThrowIfNotAvailableForHarvest(EvidenceStatus evidenceStatus)
    {
        if (evidenceStatus.Status.Code == EvidenceStatusCode.PendingConsent.Code)
        {
            throw new RequiresConsentException("The evidence code requested is pending a reply to the consent request");
        }

        if (evidenceStatus.Status.Code == EvidenceStatusCode.Denied.Code)
        {
            throw new RequiresConsentException("The consent to harvest the data for the requested evidence code has been denied or revoked");
        }

        if (evidenceStatus.Status.Code == EvidenceStatusCode.Expired.Code)
        {
            throw new RequiresConsentException("The consent to harvest the data for the requested evidence code has expired");
        }

        if (evidenceStatus.Status.Code == EvidenceStatusCode.Unavailable.Code)
        {
            throw new ServiceNotAvailableException(
                "The requested evidence code is not currently available. The evidence source plugin might be down.");
        }

        if (evidenceStatus.Status.Code != EvidenceStatusCode.Waiting.Code) return;

        if (evidenceStatus.Status.RetryAt.HasValue)
        {
            throw new AsyncEvidenceStillWaitingException("The data for the requested evidence is not yet available. The evidence source suggests a retry should be made no earlier than: " +
                                                         evidenceStatus.Status.RetryAt.Value.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
        }

        throw new AsyncEvidenceStillWaitingException();
    }

}
