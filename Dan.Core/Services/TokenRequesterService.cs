using System.IdentityModel.Tokens.Jwt;
using Dan.Core.Config;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dan.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Dan.Common.Models;
using Newtonsoft.Json;

namespace Dan.Core.Services;

/// <summary>
/// Class for requesting JWT
/// </summary>
public class TokenRequesterService : ITokenRequesterService
{
    private readonly HttpClient _client;
    private readonly IPolicyRegistry<string> _policyRegistry;
    private readonly ILogger<TokenRequesterService> _logger;
    private const string CachingPolicy = "MaskinportenTokenPolicy";

    /// <summary>
    /// 
    /// </summary>
    public TokenRequesterService(IHttpClientFactory factory, IPolicyRegistry<string> policyRegistry, ILogger<TokenRequesterService> logger)
    {
        _client = factory.CreateClient("SafeHttpClient");
        _client.BaseAddress = new Uri(Settings.MaskinportenUrl);
        _policyRegistry = policyRegistry;
        _logger = logger;
    }

    public async Task<string> GetMaskinportenToken(string scopes, string? consumerOrgNo = null)
    {
        var cachePolicy = _policyRegistry.Get<AsyncPolicy<string>>(CachingPolicy);
        return await cachePolicy.ExecuteAsync(async _ => await GetMaskinportenTokenInternal(scopes, consumerOrgNo),
            new Context(GetCacheKey(scopes, consumerOrgNo, null, null)));
    }

    public async Task<string> GetAltinnExchangedToken(string scopes, string? consumerOrgNo = null)
    {
        var cachePolicy = _policyRegistry.Get<AsyncPolicy<string>>(CachingPolicy);
        return await cachePolicy.ExecuteAsync(async _ => await GetAltinnExchangedTokenInternal(scopes, consumerOrgNo),
            new Context("altexch_" + GetCacheKey(scopes, consumerOrgNo, null, null)));
    }

    private async Task<string> GetAltinnExchangedTokenInternal(string scopes, string? consumerOrgNo)
    {
        // The cached Maskinporten token (reused as-is) is the input to the exchange.
        var maskinportenJson = await GetMaskinportenToken(scopes, consumerOrgNo);
        var maskinportenToken = JsonConvert.DeserializeObject<Dictionary<string, string>>(maskinportenJson);
        if (maskinportenToken == null
            || !maskinportenToken.TryGetValue("access_token", out var maskinportenAccessToken)
            || string.IsNullOrEmpty(maskinportenAccessToken))
        {
            throw new ServiceNotAvailableException("Failed getting Maskinporten token to exchange for an Altinn token");
        }

        // Exchange the Maskinporten token for an Altinn token (GET with the Maskinporten token as bearer).
        var request = new HttpRequestMessage(HttpMethod.Get, Settings.AltinnTokenExchangeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", maskinportenAccessToken);

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed exchanging Maskinporten token for an Altinn token - response status={statusCode}",
                response.StatusCode);
            throw new ServiceNotAvailableException("Failed exchanging Maskinporten token for an Altinn token");
        }

        // The exchange endpoint returns the Altinn token as a JSON-quoted string.
        var altinnToken = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
        if (string.IsNullOrEmpty(altinnToken))
        {
            throw new ServiceNotAvailableException("Altinn token exchange returned an empty token");
        }

        // Wrap in the same shape as the Maskinporten token response and reuse its lifetime, so the
        // existing Oauth2AccessTokenCachingStrategy can cache the exchanged token alongside it.
        maskinportenToken.TryGetValue("expires_in", out var expiresIn);
        return JsonConvert.SerializeObject(new Dictionary<string, string>
        {
            ["access_token"] = altinnToken,
            ["expires_in"] = string.IsNullOrEmpty(expiresIn) ? "0" : expiresIn
        });
    }

    public async Task<string> GetMaskinportenConsentToken(string consentId, string offeredBy, EvidenceCode evidenceCode)
    {
        var consentScope = evidenceCode.AuthorizationRequirements
            .OfType<ConsentRequirement>()
            .Select(x => x.Scope)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

        //if consentScope is empty, it means this is an altinn 2 consent requirement and we can just set the required scope from the actual api, should not happen
        consentScope = consentScope ?? evidenceCode.RequiredScopes;


        if (string.IsNullOrEmpty(consentScope))
        {
            throw new InvalidOperationException($"No consent scope found for evidence code {evidenceCode.EvidenceCodeName}");
        }

        var cachePolicy = _policyRegistry.Get<AsyncPolicy<string>>(CachingPolicy);
        return await cachePolicy.ExecuteAsync(async _ => await GetMaskinportenConsentTokenInternal(consentId, offeredBy, consentScope),
            new Context(GetCacheKey(consentId, null, offeredBy, evidenceCode.EvidenceCodeName)));
    }

    private async Task<string> GetMaskinportenConsentTokenInternal(string consentId, string offeredBy, string scope)
    {
        X509Certificate2 cert = Settings.AltinnCertificate;

        var securityKey = new X509SecurityKey(cert);
        var certs = new List<string> { Convert.ToBase64String(cert.GetRawCertData()) };
        var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
                {"x5c", certs}
            };

        header.Remove("typ");
        header.Remove("kid");

        var clientId = Settings.MaskinportenClientId;
        var audience = Settings.MaskinportenUrl;

        var payload = new JwtPayload
            {
                { "aud", audience},
                { "scope", scope },
                { "iss",  clientId},
                { "exp", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + 60 },
                { "iat", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() },
                { "jti", Guid.NewGuid().ToString() },
            };

        //Add consent-specific information to the assertion
        List<JwtPayload> authzDetail = new List<JwtPayload>()
        {
            new JwtPayload()
                {
            //only support power of attourney from organizations for the time being
                    { "from", $"urn:altinn:organization:identifier-no:{offeredBy}" },                  
                    { "id" , $"{consentId}"},
                    { "type" , "urn:altinn:consent"}
                }
        };      

        payload.Add("authorization_details", authzDetail);

        var securityToken = new JwtSecurityToken(header, payload);
        var handler = new JwtSecurityTokenHandler();
        var assertion = handler.WriteToken(securityToken);

        var formContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
            new("assertion", assertion),
        });

        var response = await _client.PostAsync("token", formContent);

        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content.ReadAsStringAsync();
            return token;
        } else
        {
            _logger.LogError("Failed getting consent token from maskinporten - response status={statusCode} body={body}", response.StatusCode, await response.Content.ReadAsStringAsync());
            throw new ServiceNotAvailableException("Failed getting consent token from Maskinporten");
        }
    }

    private string GetCacheKey(string scopes, string? consumerOrgNo, string? offeredby, string? evidenceCodeName)
    {
        using var hash = MD5.Create();
        var scopeDigest = string.Join("", hash.ComputeHash(Encoding.ASCII.GetBytes(scopes)).Select(x => x.ToString("x2")));
        return "mpt_" + Settings.MaskinportenClientId + "_" + (consumerOrgNo ?? string.Empty) + "_" + (offeredby ?? string.Empty) + "_" + (evidenceCodeName ?? string.Empty) +  scopeDigest;
    }

    private async Task<string> GetMaskinportenTokenInternal(string scopes, string? consumerOrgNo)
    {
        X509Certificate2 cert = Settings.AltinnCertificate;

        var securityKey = new X509SecurityKey(cert);
        var certs = new List<string> { Convert.ToBase64String(cert.GetRawCertData()) };
        var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
                {"x5c", certs}
            };

        header.Remove("typ");
        header.Remove("kid");

        var clientId = Settings.MaskinportenClientId;
        var audience = Settings.MaskinportenUrl;

        var payload = new JwtPayload
            {
                { "aud", audience},
                { "resource", null},
                { "scope", scopes },
                { "iss",  clientId},
                { "exp", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + 60 },
                { "iat", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() },
                { "jti", Guid.NewGuid().ToString() },
            };

        if (consumerOrgNo != null)
        {
            payload.AddClaim(new Claim("consumer_org", consumerOrgNo));
        }

        var securityToken = new JwtSecurityToken(header, payload);
        var handler = new JwtSecurityTokenHandler();
        var assertion = handler.WriteToken(securityToken);

        var formContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
            new("assertion", assertion),
        });

        var response = await _client.PostAsync("token", formContent);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        
        _logger.LogError("Failed getting token from maskinporten - response status={statusCode} body={body}",
            response.StatusCode,
            await response.Content.ReadAsStringAsync());

        throw new ServiceNotAvailableException("Failed getting token from Maskinporten");
    }
}