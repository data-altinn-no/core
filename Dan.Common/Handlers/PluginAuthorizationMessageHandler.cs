using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Dan.Common.Handlers;

// Source: https://stackoverflow.com/questions/71187362/azure-function-to-azure-function-request-using-defaultazurecredential-and-httpcl
/// <summary>
/// Adds bearer token with azure credentials for scope
/// </summary>
public class PluginAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly DefaultAzureCredential _credentials;

    /// <summary>
    /// Base constructor for azure credential handler
    /// </summary>
    public PluginAuthorizationMessageHandler(IConfiguration configuration)
    {
        _configuration = configuration;
        _credentials = new DefaultAzureCredential();
    }

    /// <summary>
    /// Adds bearer token to http request
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Usually just need one scope, api://{{guid}}/.default, but supports more for special cases
        var scopes = _configuration.GetSection("PluginScopes").Value?.Split(",");
    
        // No scopes? We just send. If something requires a scope, then add that to app settings
        if (scopes is not { Length: > 0 })
        {
            return await base.SendAsync(request, cancellationToken);
        }
    
        var tokenRequestContext = new TokenRequestContext(scopes);
        var tokenResult = await _credentials.GetTokenAsync(tokenRequestContext, cancellationToken);
        var authorizationHeader = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
        request.Headers.Authorization = authorizationHeader;
        return await base.SendAsync(request, cancellationToken);
    }
}