using System.IdentityModel.Tokens.Jwt;
using Dan.Core.Config;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Jose;

namespace Dan.Core.Helpers;

/// <summary>
/// Helper class that handles signing of evidence responses
/// </summary>
public static class Jwt
{
    /// <summary>
    /// The field name for the claim
    /// </summary>
    public const string Claim = "SHA256PayloadDigest";

    /// <summary>
    /// Creates a JWT with a payload of the SHA256 hash of the content as payload
    /// </summary>
    /// <param name="payload">The JSON payload</param>
    /// <returns>The JWT</returns>
    public static string GetDigestJwt(string payload)
    {
        var claims = new Dictionary<string, object>()
        {
            { Claim, GetSha256DigestAsHex(payload) },
        };

        return JWT.Encode(claims, Settings.AltinnCertificate.GetRSAPrivateKey(), JwsAlgorithm.RS256);
    }

    public static string RemoveBearer(string mpToken)
    {
        var splitList = mpToken.Split(" ");
        return splitList[splitList.Length - 1];
    }

    /// <summary>
    /// Verifies a signature
    /// </summary>
    /// <param name="token">The token</param>
    /// <returns>If the signature is valid for the payload</returns>
    public static bool VerifyTokenSignature(string token)
    {
        try
        {
            JWT.Decode(token, Settings.AltinnCertificate.GetRSAPublicKey());
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetSha256DigestAsHex(string payload)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(payload));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    public static string? GetOrgNumberFromMaskinportenToken(JwtSecurityToken jwt)
    {
        var consumer = jwt.Claims.FirstOrDefault(j => j.Type == "consumer");
        if (consumer == null)
        {
            return null;
        }

        try
        {
            dynamic result = JObject.Parse(consumer.Value);
            return result.ID.ToString().Split(":")[1];
        }
        catch (Exception)
        {
            return null;
        }
    }
}
