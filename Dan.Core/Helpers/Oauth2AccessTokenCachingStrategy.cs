using Newtonsoft.Json;
using Polly;
using Polly.Caching;

namespace Dan.Core.Helpers;

public class Oauth2AccessTokenCachingStrategy : ITtlStrategy<string>
{
    private const uint SafetyMarginInSeconds = 20;

    /// <summary>
    /// Returns the TTL for a access token received from the OAuth2 authorization server, based on the lifetime for the token itself. Has a safety margin to avoid
    /// the token expiring while in transit to the ES (which may need a cold start). Tokens with lifetime below the safety margin will not be cached.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public Ttl GetTtl(Context context, string result)
    {
        try
        {
            var token = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
            return int.TryParse(token?["expires_in"], out var expiresIn)
                ? new Ttl(TimeSpan.FromSeconds(Math.Max(0, expiresIn - SafetyMarginInSeconds)))
                : new Ttl(TimeSpan.Zero);
        }
        catch
        {
            return new Ttl(TimeSpan.Zero);
        }
    }
}