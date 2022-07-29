using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Dan.Core.Models;

namespace Dan.Core.Extensions;

/// <summary>
/// Extensions methods for claims principals
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string CLAIM_SCOPE = "scope";
    private const string CLAIM_CONSUMER = "consumer";
    private const string CLAIM_CONSUMER_AUTHORITY = "iso6523-actorid-upis";
    private const string CLAIM_CONSUMER_ID_ISO6523 = "0192";

    /// <summary>
    /// Convenience method for getting a claim value by name
    /// </summary>
    /// <param name="claimsPrincipal">The claims principal</param>
    /// <param name="claimType">The claim requested</param>
    /// <returns>The claim or null</returns>
    public static string GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        Claim claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == claimType);
        return claim?.Value;
    }

    /// <summary>
    /// Convenience method for checking if principal contains scope
    /// </summary>
    /// <param name="claimsPrincipal">The claims principal</param>
    /// <param name="scope">scope to check</param>
    /// <returns>a bool</returns>
    public static bool HasScope(this ClaimsPrincipal claimsPrincipal, string scope)
    {
        string[] scopes = claimsPrincipal?.GetClaimValue(CLAIM_SCOPE)?.Split(' ');
        return !string.IsNullOrEmpty(scopes?.FirstOrDefault(s => s.Equals(scope)));
    }

    /// <summary>
    /// Convenience method for getting a list of scopes from a principal
    /// </summary>
    /// <param name="claimsPrincipal"></param>
    /// <returns></returns>
    public static string[] GetScopes(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.GetClaimValue(CLAIM_SCOPE)?.Split(' ');
    }

    public static string GetOrganizationNumberClaim(this ClaimsPrincipal claimsPrincipal)
    {
        string rawClaimValue = claimsPrincipal.GetClaimValue(CLAIM_CONSUMER);
        ConsumerClaim consumerClaim;
        try
        {
            consumerClaim = JsonConvert.DeserializeObject<ConsumerClaim>(rawClaimValue);
        }
        catch (JsonReaderException)
        {
            throw new ArgumentException("Invalid consumer claim: invalid JSON");
        }

        if (consumerClaim.Authority != CLAIM_CONSUMER_AUTHORITY)
        {
            throw new ArgumentException("Invalid consumer claim: unexpected authority");
        }

        string[] identityParts = consumerClaim.ID.Split(':');
        if (identityParts[0] != CLAIM_CONSUMER_ID_ISO6523)
        {
            throw new ArgumentException("Invalid consumer claim: unexpected ISO6523 identifier");
        }

        if (!Regex.IsMatch(identityParts[1], @"^\d{9}$"))
        {
            throw new ArgumentException("Invalid consumer scope: expected norwegian organization number");
        }

        return identityParts[1];
    }
}