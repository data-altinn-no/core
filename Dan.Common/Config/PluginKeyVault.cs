using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Dan.Common.Config;

/// <summary>
/// Keyvault config class
/// </summary>
public class PluginKeyVault
{
    private SecretClient SecretClient { get; }

    /// <summary>
    /// Keyvault config class
    /// </summary>
    /// <param name="vaultName">Name of the Key Vault</param>
    public PluginKeyVault(string? vaultName)
    {
        if (string.IsNullOrEmpty(vaultName))
        {
            throw new ArgumentNullException(nameof(vaultName), $"{nameof(vaultName)} cannot be null or empty.");
        }
        SecretClient = new SecretClient(new Uri($"https://{vaultName}.vault.azure.net/"), new DefaultAzureCredential());
    }

    /// <summary>
    /// Get a secret from the key vault
    /// </summary>
    /// <param name="key">Secret name</param>
    /// <returns>The secret value</returns>
    public async Task<string?> Get(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), $"{nameof(key)} cannot be null or empty.");
        }
        
        var secret = await SecretClient.GetSecretAsync(key);
        return secret?.Value?.Value;
    }

    /// <summary>
    /// Get a X509 certificate from the key vault
    /// </summary>
    /// <param name="key">Certificate name</param>
    /// <returns>The certificate</returns>
    public async Task<X509Certificate2?> GetCertificate(string key)
    {
        var base64Certificate = await Get(key);
        if (base64Certificate == null)
        {
            return default;
        }
        var certBytes = Convert.FromBase64String(base64Certificate);

        var cert = new X509Certificate2(certBytes, string.Empty, X509KeyStorageFlags.MachineKeySet);

        return await Task.FromResult(cert);
    }
}