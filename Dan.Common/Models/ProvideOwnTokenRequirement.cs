namespace Dan.Common.Models
{
    /// <summary>
    /// Requirement for requestor to provide a bearer token or delegates access to Digitaliseringsdirektoratet
    /// </summary>
    [DataContract]
    public class ProvideOwnTokenRequirement : Requirement { }
}