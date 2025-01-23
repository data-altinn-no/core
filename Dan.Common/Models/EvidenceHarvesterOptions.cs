namespace Dan.Common.Models;

/// <summary>
/// Settings for handing access tokens for evidence harvesting
/// </summary>
public class EvidenceHarvesterOptions
{
    /// <summary>
    /// If set, this will attempt to fetch a supplier token from Maskinporten on behalf of the authenticated party.
    /// This requires the party to have delegated access to a delegation scheme(s) in Altinn to Digdir for the scopes the
    /// evidence code requires. If the evidence code does not require any scopes, this value is ignored.
    /// </summary>
    /// <value>
    /// If true, DAN will attempt to aquire a supplier token on behalf of the authenticated party. If false, DAN will
    /// attempt to fetch a normal Maskinporten consumer token as Digdir.
    /// </value>
    public bool FetchSupplierAccessTokenOnBehalfOfOwner { get; set; } = false;

    /// <summary>
    /// If set, the supplied string will be used as a bearer token against the evidence source for the given evidence code.
    /// Setting this will cause FetchSupplierAccessTokenOnBehalfOfOwner to be ignored.
    /// </summary>
    /// <value>
    /// The overridden access token as a string. 
    /// </value>
    public string? OverriddenAccessToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the access token used to request DAN should be reused against the evidence source.
    /// This allows the consumer to use a single token with multiple scopes. Note that DAN do not perform any audience claim validation.
    /// Setting this will cause FetchSupplierAccessTokenOnBehalfOfOwner and OverriddenAccessToken to be ignored.
    /// </summary>
    /// <value>
    ///   <c>true</c> if DAN is to reuse the access token provided by the client to DAN; otherwise, <c>false</c>.
    /// </value>
    public bool ReuseClientAccessToken { get; set; } = false;
}