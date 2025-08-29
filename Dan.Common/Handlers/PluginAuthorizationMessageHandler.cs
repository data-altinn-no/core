using AsyncKeyedLock;
using Azure.Core;
using Azure.Identity;
using Dan.Common.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dan.Common.Handlers;

// Source: https://stackoverflow.com/questions/71187362/azure-function-to-azure-function-request-using-defaultazurecredential-and-httpcl
/// <summary>
/// Adds bearer token with azure credentials for scope
/// </summary>
public class PluginAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IPluginCredentialService pluginCredentialService;
    

    /// <summary>
    /// Base constructor for azure credential handler
    /// </summary>
    public PluginAuthorizationMessageHandler(IPluginCredentialService pluginCredentialService)
    {
        this.pluginCredentialService = pluginCredentialService;
    }

    /// <summary>
    /// Adds bearer token to http request
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await pluginCredentialService.GetToken(cancellationToken);
        
        if (token is null)
        {
            return await base.SendAsync(request, cancellationToken);
        }
        
        var authorizationHeader = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Authorization = authorizationHeader;
        
        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}