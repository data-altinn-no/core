using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using Dan.Core.Helpers;

namespace Dan.Core.Config;

/// <summary>
/// Application settings
/// </summary>
public static class Settings
{
    private static CoreKeyVault? _keyVault;
    private static X509Certificate2? _altinnCertificate;
    private static string? _altinnApiKey;
    private static string? _altinnServiceOwnerApiKey;
    private static string? _cosmosDbConnection;
    private static string? _redisConnection;
    private static string? _agencySystemUserName;
    private static string? _agencySystemPassword;
    private static string? _functionKeyValue;
    private static string? _maskinportenClientId;
    private static List<string>? _consentValidationSecrets;

    /// <summary>
    /// If the program is running in development environment
    /// </summary>
    public static bool IsDevEnvironment => new[] { "Development", "LocalDevelopment" }.Any(GetSetting("ASPNETCORE_ENVIRONMENT").Contains);

    /// <summary>
    /// If the program is running in staging environment
    /// </summary>
    public static bool IsStagingEnvironment => GetSetting("ASPNETCORE_ENVIRONMENT").Equals("Staging");

    /// <summary>
    /// If the program is running in staging environment
    /// </summary>
    public static bool IsProductionEnvironment => !IsStagingEnvironment && !IsDevEnvironment;

    /// <summary>
    /// If the program is running unit test
    /// </summary>
    public static bool IsUnitTest => Convert.ToBoolean(GetSetting("IsUnitTest"));

    /// <summary>
    /// List of EvidenceSources
    /// </summary>
    public static List<string> EvidenceSources => GetSetting("EvidenceSources").Split(',').ToList();

    /// <summary>
    /// Gets the evidence source URL for a provider
    /// </summary>
    /// <param name="provider">The provider</param>
    /// <returns>The url</returns>
    public static string GetEvidenceSourceUrl(string provider) => GetSetting("EvidenceSourceURLPattern").Replace("%s", provider);

    /// <summary>
    /// Get the api url
    /// </summary>
    public static string ApiUrl => "https://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api";

    /// <summary>
    /// Connection string for CosmosDB
    /// </summary>
    public static string CosmosDbConnection => 
        _cosmosDbConnection ??= string.IsNullOrEmpty(GetSetting("CosmosDbConnection"))
            ? KeyVault.Get(KeyVaultCosmosDbConnection).Result
            : GetSetting("CosmosDbConnection"); 

    /// <summary>
    /// CosmosDB Accreditations
    /// </summary>
    public const string CosmosDbAccreditations = "Accreditations";

    /// <summary>
    /// Partition key used for Accreditations-collection
    /// </summary>
    public const string CosmosbDbAccreditationsPartitionKey = "/Owner";

    /// <summary>
    /// Cosmos DB database
    /// </summary>
    public static string CosmosDbDatabase => GetSetting("CosmosDbDatabase");

    /// <summary>
    /// Redis  Connection String
    /// </summary>
    public static string RedisCacheConnectionString =>
        _redisConnection ??= string.IsNullOrEmpty(GetSetting("RedisConnection"))
            ? KeyVault.Get(KeyVaultRedisCacheConnection).Result
            : GetSetting("RedisConnection");

    /// <summary>
    /// Azure Key vault
    /// </summary>
    public static CoreKeyVault KeyVault => _keyVault ??= new CoreKeyVault(KeyVaultName);

    /// <summary>
    /// Altinn EC Certificate
    /// </summary>
    public static X509Certificate2 AltinnCertificate =>
        IsUnitTest
            ? _altinnCertificate ??= new X509Certificate2(Convert.FromBase64String(GetSetting("SelfSignedCert")))
            : _altinnCertificate ??= KeyVault.GetCertificate(KeyVaultSslCertificate).Result;

    /// <summary>
    /// API-key for consent request / token
    /// </summary>
    public static string AltinnApiKey => 
        _altinnApiKey ??= string.IsNullOrEmpty(GetSetting("AltinnApiKey"))
            ? KeyVault.Get(KeyVaultAltinnApiKey).Result
            : GetSetting("AltinnApiKey");

    /// <summary>
    /// API-key for SRR
    /// </summary>
    public static string AltinnServiceOwnerApiKey => 
        _altinnServiceOwnerApiKey ??= string.IsNullOrEmpty(GetSetting("AltinnServiceOwnerApiKey"))
            ? KeyVault.Get(KeyVaultAltinnServiceOwnerApiKey).Result
            : GetSetting("AltinnServiceOwnerApiKey");

    /// <summary>
    /// The system user name to use in authentication with the Altinn agency services.
    /// </summary>
    public static string AgencySystemUserName =>
        _agencySystemUserName ??= string.IsNullOrEmpty(GetSetting("AgencySystemUserName"))
            ? KeyVault.Get(KeyVaultAgencySystemUserName).Result
            : GetSetting("AgencySystemUserName");

    /// <summary>
    /// The system password to use in authentication with the Altinn agency services.
    /// </summary>
    public static string AgencySystemPassword =>
        _agencySystemPassword ??= string.IsNullOrEmpty(GetSetting("AgencySystemPassword"))
            ? KeyVault.Get(KeyVaultAgencySystemPassword).Result
            : GetSetting("AgencySystemPassword");

    /// <summary>
    /// Function key value
    /// </summary>
    public static string FunctionKeyValue =>
        _functionKeyValue ??= string.IsNullOrEmpty(GetSetting("FunctionKeyValue"))
            ? KeyVault.Get(KeyVaultFunctionKeyValue).Result
            : GetSetting("FunctionKeyValue"); 

    /// <summary>
    /// Gets the ID of the Maskinporten client used to generate maskinporten tokens
    /// </summary>
    public static string MaskinportenClientId =>
        _maskinportenClientId ??= string.IsNullOrEmpty(GetSetting("MaskinportenClientId"))
            ? KeyVault.Get(KeyVaultMaskinportenClientId).Result
            : GetSetting("MaskinportenClientId");

    /// <summary>
    /// The secrets for verifying consent hashes 
    /// </summary>
    public static List<string> ConsentValidationSecrets =>
        _consentValidationSecrets ??= (string.IsNullOrEmpty(GetSetting("ConsentValidationSecrets"))
            ? KeyVault.Get(KeyVaultConsentValidationSecrets).Result
            : GetSetting("ConsentValidationSecrets")).Split(',').ToList();

    /// <summary>
    /// API-url for SSR
    /// </summary>
    public static string AltinnServiceOwnerApiUri => GetSetting("AltinnServiceOwnerApiURI");

    /// <summary>
    /// SSL Certificate Thumbprint
    /// </summary>
    public static string KeyVaultSslCertificate => GetSetting("KeyVaultSslCertificate");

    /// <summary>
    /// Nadobe certificate header
    /// </summary>
    public static string CertificateHeader => "X-NADOBE-CERT";

    /// <summary>
    /// Key Vault Name
    /// </summary>
    public static string KeyVaultName => GetSetting("KeyVaultName");

    /// <summary>
    /// Key Vault key name for the Altinn API key
    /// </summary>
    public static string KeyVaultAltinnApiKey => GetSetting("KeyVaultAltinnApiKey");

    /// <summary>
    /// Key Vault key name for the Altinn Service Owner API key
    /// </summary>
    public static string KeyVaultAltinnServiceOwnerApiKey => GetSetting("KeyVaultAltinnServiceOwnerApiKey");

    /// <summary>
    /// Key Vault key name for the CosmosDB connection string
    /// </summary>
    public static string KeyVaultCosmosDbConnection => GetSetting("KeyVaultCosmosDbConnection");

    /// <summary>
    /// Key Vault key name for the Redis connection string
    /// </summary>
    public static string KeyVaultRedisCacheConnection => GetSetting("KeyVaultRedisCacheConnection");

    /// <summary>
    /// Key Vault key name for the Agency System Username
    /// </summary>
    public static string KeyVaultAgencySystemUserName => GetSetting("KeyVaultAgencySystemUserName");

    /// <summary>
    /// Key Vault key name for the Agency System Password
    /// </summary>
    public static string KeyVaultAgencySystemPassword => GetSetting("KeyVaultAgencySystemPassword");

    /// <summary>
    /// Key Vault key name for the function key used to call data sources
    /// </summary>
    public static string KeyVaultFunctionKeyValue => GetSetting("KeyVaultFunctionKeyValue");

    /// <summary>
    /// Key Vault key name for the Maskinporten client id
    /// </summary>
    public static string KeyVaultMaskinportenClientId => GetSetting("KeyVaultMaskinportenClientId");

    /// <summary>
    /// Key Vault key name for the Maskinporten client id
    /// </summary>
    public static string KeyVaultConsentValidationSecrets => GetSetting("KeyVaultConsentValidationSecrets");

    /// <summary>
    /// Organization Validation Url
    /// </summary>
    public static string OrganizationValidationUrl => GetSetting("OrganizationValidationUrl");

    /// <summary>
    /// Organization Validation Url for sub units
    /// </summary>
    public static string OrganizationSubUnitValidationUrl => GetSetting("OrganizationSubUnitValidationUrl");

    /// <summary>
    /// Organization Validation Url, using syntetic CCR
    /// </summary>
    public static string SyntheticOrganizationValidationUrl => GetSetting("SyntheticOrganizationValidationUrl");

    /// <summary>
    /// Organization Validation Url for sub units, using syntetic CCR
    /// </summary>
    public static string SyntheticOrganizationSubUnitValidationUrl => GetSetting("SyntheticOrganizationSubUnitValidationUrl");

    /// <summary>
    /// Valid subject organizations in development / staging environment (aka test organizations in TT02)
    /// </summary>
    public static List<string> TestEnvironmentValidOrgs => GetSetting("TestEnvironmentValidOrgs").Split(',').ToList();

    /// <summary>
    /// Organization Validation Url
    /// </summary>
    public static int BreakerFailureCountThreshold => Convert.ToInt32(GetSetting("Breaker_FailureCountThreshold"));

    /// <summary>
    /// Organization Validation Url
    /// </summary>
    public static TimeSpan BreakerRetryWaitTime => TimeSpan.FromMilliseconds(Convert.ToInt32(GetSetting("Breaker_RetryWaitTime_ms")));


    /// <summary>
    /// The default amount of days the accreditation (and consent) should be valid
    /// </summary>
    public static int AccreditationDefaultValidDays => Convert.ToInt32(GetSetting("AccreditationDefaultValidDays"));

    /// <summary>
    /// The minimum amount of days the accreditation (and consent) should be valid
    /// </summary>
    public static int AccreditationMinimumValidDays => Convert.ToInt32(GetSetting("AccreditationMinimumValidDays"));

    /// <summary>
    /// The maximum amount of days the accreditation (and consent) should be valid
    /// </summary>
    public static int AccreditationMaximumValidDays => Convert.ToInt32(GetSetting("AccreditationMaximumValidDays"));

    /// <summary>
    /// The organization number for altinn
    /// </summary>
    public static string AltinnOrgNumber => GetSetting("AltinnOrgNumber");

    /// <summary>
    /// Https address to the altinn web services
    /// </summary>
    public static string AltinnServiceAddress => GetSetting("AltinnServiceAddress");

    /// <summary>
    /// Https address to the altinn portal
    /// </summary>
    public static string AltinnPortalAddress => GetSetting("AltinnPortalAddress");



    /// <summary>
    /// Correspondence settings (serviceCode,serviceEdition,username,password,systemUserCode)
    /// </summary>
    public static string CorrespondenceSettings => GetSetting("CorrespondenceSettings");

    /// <summary>
    /// The URI pattern for accessing an accreditation.
    /// </summary>
    /// <remarks>
    /// This property is used to set the <c>Location</c> header in the response from a successful <c>POST</c>
    /// to the authorization resource. This is temporary as it is probably better to have a policy in
    /// API Management that can rewrite the uri correctly. Do not use this property for other things.
    /// </remarks>
    public static string AccreditationCreatedLocationPattern => GetSetting("AccreditationCreatedLocationPattern");

    /// <summary>
    /// Gets the consent status url
    /// </summary>
    /// <param name="authCode">The authCode</param>
    /// <returns>The url</returns>
    public static string GetConsentStatusUrl(string authCode) =>string.Format(GetSetting("ConsentStatusURLPattern"), authCode);

    /// <summary>
    /// The condition string to use as condition when creating SRR rights.
    /// </summary>
    public static string SrrRightsCondition => GetSetting("SrrRightsCondition");

    /// <summary>
    /// Gets the consent logging URL
    /// </summary>
    /// <param name="authCode">The authCode</param>
    /// <returns>The url</returns>
    public static string GetConsentLoggingUrl(string authCode) => string.Format(GetSetting("ConsentLoggingURLPattern"), authCode);
    
    /// <summary>
    /// Gets the consent redirect URL
    /// </summary>
    /// <param name="accreditationId">The accreditation id</param>
    /// <param name="hmac">The hash-based message authentication code</param>
    /// <returns>The url</returns>
    public static string GetConsentRedirectUrl(string accreditationId, string hmac) 
        => string.Format(GetSetting("ConsentRedirectURLPattern"), accreditationId, System.Net.WebUtility.UrlEncode(hmac));

    /// <summary>
    /// Gets the url template for deep link in Doffin.
    /// </summary>
    /// <returns>The url template.</returns>
    public static string GetDoffinLinkTemplate() => GetSetting("DoffinLinkTemplate");

    /// <summary>
    /// Gets the url template for deep link in Ted.
    /// </summary>
    /// <returns>The url template.</returns>
    public static string GetTedLinkTemplate() => GetSetting("TedLinkTemplate");

    /// <summary>
    /// Gets the Base URL to the Maskinporten environment
    /// </summary>
    public static string MaskinportenUrl => GetSetting("MaskinportenUrl");

    /// <summary>
    /// Gets the Base URL to an auxiliary Maskinporten environment
    /// </summary>
    public static string MaskinportenAuxUrl => GetSetting("MaskinportenAuxUrl");

    /// <summary>
    /// Gets setting for whether or not to use altinn servers in test mode (for profiling and problem solving)
    /// </summary>
    public static bool UseAltinnTestServers => bool.Parse(GetSetting("UseAltinnTestServers"));

    public static string GetWhiteList(string key) => GetSetting(key);

    /// <summary>
    /// Gets the url to the wellknown endpoint for Maskinporten
    /// </summary>
    public static string MaskinportenWellknownUrl => GetSetting("MaskinportenWellknownUrl");

    /// <summary>
    /// Gets the url to the wellknown endpoint for the auxiliary Maskinporten env
    /// </summary>
    public static string MaskinportenAuxWellknownUrl => GetSetting("MaskinportenAuxWellknownUrl");

    public static string AltinnWellknownUrl => GetSetting("AltinnWellknownUrl");

    public const int MaxReferenceLength = 50;

    public static int DefaultHarvestTaskCancellation = 35;

    private static string GetSetting(string settingKey)
    {
        var value = ConfigurationHelper.ConfigurationRoot[settingKey];
        if (value == null)
        {
            throw new MissingSettingsException($"Missing settings key: {settingKey}");
        }

        return value;
    }

    /// <summary>
    /// Exception thrown if a required setting is missing
    /// </summary>
    public class MissingSettingsException : Exception
    {
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="message">The error message</param>
        public MissingSettingsException(string message) : base(message)
        {
        }
    }
}