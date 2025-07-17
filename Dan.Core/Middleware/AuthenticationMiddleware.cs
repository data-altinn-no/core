using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dan.Common;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Dan.Core.Middleware;

public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    public const string AuthorizationHeader = "X-NADOBE-AUTHORIZATION";
    public const string AuthorizationHeaderLocal = "Authorization";
    public const string DefaultScope = "altinn:dataaltinnno";
    public const string NewScopeRoot = "dan:";

    private static readonly object CmLockMaskinporten = new();
    private static readonly object CmLockAltinnPlatform = new();
    private static volatile ConfigurationManager<OpenIdConnectConfiguration>? _cmMaskinporten;
    private static volatile ConfigurationManager<OpenIdConnectConfiguration>? _cmAltinnPlatform;

    /// <summary>
    /// Gets or sets maskinporten ConfigManager
    /// </summary>
    public static ConfigurationManager<OpenIdConnectConfiguration> CmMaskinporten
    {
        get
        {
            if (_cmMaskinporten != null) return _cmMaskinporten;
            lock (CmLockMaskinporten)
            {
                if (_cmMaskinporten == null)
                {
                    _cmMaskinporten = new ConfigurationManager<OpenIdConnectConfiguration>(
                        Settings.MaskinportenWellknownUrl,
                        new OpenIdConnectConfigurationRetriever(),
                        new HttpClient { Timeout = TimeSpan.FromMilliseconds(10000) });
                }
            }

            return _cmMaskinporten;
        }

        set
        {
            lock (CmLockMaskinporten)
            {
                _cmMaskinporten = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets Altinn3 ConfigManager
    /// </summary>
    public static ConfigurationManager<OpenIdConnectConfiguration> CmAltinnPlatform
    {
        get
        {
            if (_cmAltinnPlatform != null) return _cmAltinnPlatform;
            lock (CmLockAltinnPlatform)
            {
                if (_cmAltinnPlatform == null)
                {
                    _cmAltinnPlatform = new ConfigurationManager<OpenIdConnectConfiguration>(
                        Settings.AltinnWellknownUrl,
                        new OpenIdConnectConfigurationRetriever(),
                        new HttpClient { Timeout = TimeSpan.FromMilliseconds(10000) });
                }
            }

            return _cmAltinnPlatform;
        }

        set
        {
            lock (CmLockAltinnPlatform)
            {
                _cmAltinnPlatform = value;
            }
        }
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        string? orgNumber;
        List<string> scopes = new();

        var request = await context.GetHttpRequestDataAsync();
        if (request == null)
        {
            await next(context);
            return;
        }

        // Usually this header is set by APIM, but for local testing check Authorization header as well
        if (!request.Headers.TryGetValues(AuthorizationHeader, out var headerValues))
        {
            request.Headers.TryGetValues(AuthorizationHeaderLocal, out headerValues);
        }

        // check authorization header for bearer token
        if (headerValues != null)
        {
            var accessTokenJwt = headerValues.First();
            accessTokenJwt = Jwt.RemoveBearer(accessTokenJwt);
            var claimsPrincipal = await ValidateJwt(accessTokenJwt);
            if (ValidateScopes(claimsPrincipal))
            {
                orgNumber = claimsPrincipal.GetOrganizationNumberClaim();
                scopes = claimsPrincipal.GetScopes()!.ToList();
            }
            else
            {
                throw new AuthorizationFailedException($"Missing required scope(s)");
            }

            context.Items.Add(Constants.ACCESS_TOKEN, accessTokenJwt);
        }
        else
        {
            throw new MissingAuthenticationException("No authentication method supplied");
        }

        context.Items.Add(Constants.AUTHENTICATED_ORGNO, orgNumber);
        context.Items.Add(Constants.SCOPES, scopes);

        await next(context);
    }

    private async Task<ClaimsPrincipal> ValidateJwt(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(token);

        OpenIdConnectConfiguration discoveryDocument;
        if (jwt.Issuer == Settings.MaskinportenUrl)
        {
            discoveryDocument = await CmMaskinporten.GetConfigurationAsync();
        }
        else
        {
            discoveryDocument = await CmAltinnPlatform.GetConfigurationAsync();
        }

        ICollection<SecurityKey> signingKeys = discoveryDocument.SigningKeys;

        var validationParameters = new TokenValidationParameters
        {
            IssuerSigningKeys = signingKeys,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false,
            ValidateIssuer = false
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch (SecurityTokenException e)
        {
            throw new InvalidAccessTokenException(e.GetType().Name + ": " + e.Message);
        }
    }

    private bool ValidateScopes(ClaimsPrincipal claimsPrincipal)
    {
        var principalScopeList = claimsPrincipal.GetScopes();
        if (principalScopeList == null)
        {
            return false;
        }

        // Replaced with StartsWith to allow new scope root and removed foreach
        // Old: Note that this had .Contains does a substring match. This means that a requirement for
        // eg. altinn:somescope will be satisfied by altinn:somescope/foo or any scope containing the substring
        // "altinn:somescope". As ":" is not a valid subscope character in Maskinporten, this ought to be
        // safe as it cannot be abused by something like "difi:altinn:somescope"
        if (!principalScopeList.Any(x => x.StartsWith(DefaultScope) || x.StartsWith(NewScopeRoot)))
        {
            return false;
        }
        

        return true;
    }
}