namespace Dan.Common.Models;

/// <summary>
/// Subject or requestor party for requests
/// </summary>
[DataContract]
public class Party
{
    /// <summary>
    /// Party scheme. Example: iso6523-actorid-upis
    /// </summary>
    [DataMember(Name = "scheme")]
    public string? Scheme { get; set; }

    /// <summary>
    /// Party identifier
    /// </summary>
    [DataMember(Name = "id")]
    public string? Id { get; set; }

    /// <summary>
    /// Norwegian organisation number
    /// </summary>
    [DataMember(Name = "norwegianOrganizationNumber")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? NorwegianOrganizationNumber { get; set; }

    /// <summary>
    /// Norwegian social security number
    /// </summary>
    [DataMember(Name = "norwegianSocialSecurityNumber")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? NorwegianSocialSecurityNumber { get; set; }

    /// <summary>
    /// Returns norwegian organisation number if set, else norwegian social security if set,
    /// if not returns scheme plus id.
    /// </summary>
    /// <param name="maskString">Masks part of social security number if true</param>
    public string GetAsString(bool maskString = true)
    {
        if (NorwegianOrganizationNumber != null)
        {
            return NorwegianOrganizationNumber;
        }

        if (NorwegianSocialSecurityNumber != null)
        {
            return maskString ? NorwegianSocialSecurityNumber[..6] + "*****" : NorwegianSocialSecurityNumber;
        }

        return Scheme + "::" + Id;
    }
    
    /// <summary>
    /// Returns norwegian organisation number if set, else norwegian social security if set,
    /// if not returns scheme plus id.
    /// </summary>
    public override string ToString() => GetAsString();
}