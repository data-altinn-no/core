namespace Dan.Common.Models;

[DataContract]
public class Party
{
    [DataMember(Name = "scheme")]
    public string Scheme { get; set; }

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "norwegianOrganizationNumber")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string NorwegianOrganizationNumber { get; set; }

    [DataMember(Name = "norwegianSocialSecurityNumber")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string NorwegianSocialSecurityNumber { get; set; }

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
    public override string ToString() => GetAsString();
}