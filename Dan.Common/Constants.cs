namespace Dan.Common;

/// <summary>
/// Shared constants in Dan
/// </summary>
public static class Constants
{
    /// <summary>
    /// The function returning evidence code metadata 
    /// </summary>
    public const string EvidenceSourceMetadataFunctionName = "evidencecodes";

    public const string SafeHttpClient = "SafeHttpClient";

    public const string SafeHttpClientPolicy = "SafeHttpClientPolicy";

    public const string LANGUAGE_CODE_NORWEGIAN_NB = "no-nb";

    public const string LANGUAGE_CODE_NORWEGIAN_NN = "no-nn";

    public const string LANGUAGE_CODE_ENGLISH = "en";

    public const string AUTHENTICATED_ORGNO = "AuthenticatedOrgNo";

    public const string SCOPES = "scopes";

    public const string ACCESS_TOKEN = "access_token";

    public const string SUBSCRIPTION_KEY_HEADER = "Ocp-Apim-Subscription-Key";
}

/// <summary>
/// Shared service context constants in Dan
/// </summary>
public static class ServiceContextNames
{
    /// <summary>
    /// Public procurement data sets for KGVs
    /// </summary>
    public const string eBevis = "eBevis";

    /// <summary>
    /// Enterprise solidity data sets for county municipalites used to grant or deny taxi licenses
    /// </summary>
    public const string drosjelove = "Drosjeloyve";

    /// <summary>
    /// Data sets from Norwegian Audit agencies shared between themselves and third parties
    /// </summary>
    public const string tilsyn = "Tilsyn";
}

/// <summary>
/// Macros used in texts to insert actual values runtime
/// </summary>
public static class TextMacros
{
    public const string Requestor = "#Requestor#";

    public const string RequestorName = "#RequestorName#";

    public const string Subject = "#Subject#";

    public const string SubjectName = "#SubjectName#";

    public const string ServiceContextName = "#ServiceContextName#";

    public const string ConsentType = "#ConsentType#";

    public const string Button = "#Button#";

    public const string ConsentReference = "#ConsentReference#";

    public const string ExternalReference = "#ExternalReference#";

    //Custom macro for using ted/doffin templates in ebevis context
    public const string EbevisReference = "#EbevisReference#";

    public const string ConsentOrExternalReference = "#ConsentOrExternalReference#";

    public const string ConsentAndExternalReference = "#ConsentAndExternalReference#";

}
