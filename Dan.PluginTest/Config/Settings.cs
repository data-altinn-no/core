using Dan.Common.Config;

namespace Dan.PluginTest.Config;

public class Settings
{
    public string? PluginCode { get; set; }
    public string? KeyVaultName { get; set; }
    public string? CertName { get; set; }
    
    
    private string? _cert;
    public string? Certificate
    {
        get => _cert ?? new PluginKeyVault(KeyVaultName).GetCertificateAsBase64(CertName).Result;
        set => _cert = value;
    }
}