using System.IdentityModel.Tokens.Jwt;
using Dan.Core.Config;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dan.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;

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
            new Context(GetCacheKey(scopes, consumerOrgNo)));
    }

    private string GetCacheKey(string scopes, string? consumerOrgNo)
    {
        using var hash = MD5.Create();
        var scopeDigest = string.Join("", hash.ComputeHash(Encoding.ASCII.GetBytes(scopes)).Select(x => x.ToString("x2")));
        return "mpt_" + Settings.MaskinportenClientId + "_" + (consumerOrgNo ?? string.Empty) + "_" + scopeDigest;
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