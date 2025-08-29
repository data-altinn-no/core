using AsyncKeyedLock;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Dan.Common.Services;

/// <summary>
/// Service for handling getting auth token for plugins
/// </summary>
public interface IPluginCredentialService
{
    /// <summary>
    /// Get auth token needed to authorise against plugins. App settings need to include scopes,
    /// usually just api://{{guid}}/.default, but supports more with comma separation
    /// </summary>
    /// <returns>Plugin auth token</returns>
    Task<string?> GetToken(CancellationToken cancellationToken);
}

/// <summary>
/// Service for handling getting auth token for plugins
/// </summary>
public class PluginCredentialService(IConfiguration configuration) : IPluginCredentialService
{
    private readonly DefaultAzureCredential credentials = new();
    private readonly AsyncNonKeyedLocker semaphore = new(1);

    /// <summary>
    /// Get auth token needed to authorise against plugins. App settings need to include scopes,
    /// usually just api://{{guid}}/.default, but supports more with comma separation
    /// </summary>
    /// <returns>Plugin auth token</returns>
    public async Task<string?> GetToken(CancellationToken cancellationToken)
    {
        var scopes = configuration.GetSection("PluginScopes").Value?.Split(",");
    
        // No scopes? We just send. If something requires a scope, then add that to app settings
        if (scopes is not { Length: > 0 })
        {
            return null;
        }

        
        using (await semaphore.LockAsync(cancellationToken))
        {
            var tokenRequestContext = new TokenRequestContext(scopes);
            var tokenResult = await credentials.GetTokenAsync(tokenRequestContext, cancellationToken);
            return tokenResult.Token;
        }
    }
}