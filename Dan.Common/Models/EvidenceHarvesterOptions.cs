namespace Dan.Common.Models;

public class EvidenceHarvesterOptions
{
    /// <summary>
    /// If set, this will attempt to fetch a supplier token from Maskinporten on behalf of the supplied party.
    /// This requires the party to have delegated access to a delegation scheme(s) in Altinn to Digdir for the scopes the
    /// evidence code requires. If the evidence code does not require any scopes, this value is ignored.
    /// </summary>
    /// <value>
    /// The part (owner, requestor, subject) that DAN will attempt to fetch a token on behalf of. If null, DAN will
    /// attempt to fetch a normal Maskinporten consumer token as Digdir.
    /// </value>
    public AccreditationPartyTypes? FetchSupplierAccessTokenOnBehalfOf { get; set; }

    /// <summary>
    /// If set, the supplied string will be used as a bearer token against the evidence source for the given evidence code.
    /// Setting this will cause FetchSupplierAccessTokenOnBehalfOf to be ignored.
    /// </summary>
    /// <value>
    /// The overridden access token as a string. 
    /// </value>
    public string OverriddenAccessToken { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether the access token used to request DAN should be reused against the evidence source.
    /// This allows the consumer to use a single token with multiple scopes. Note that DAN do not perform any audience claim validation.
    /// Setting this will cause FetchSupplierAccessTokenOnBehalfOf and OverriddenAccessToken to be ignored.
    /// </summary>
    /// <value>
    ///   <c>true</c> if DAN is to reuse the access token provided by the client to DAN; otherwise, <c>false</c>.
    /// </value>
    public bool ReuseClientAccessToken { get; set; } = false;
}