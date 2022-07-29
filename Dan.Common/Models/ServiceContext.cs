using Dan.Common.Interfaces;

namespace Dan.Common.Models;

/// <summary>
/// Class describing Service Context
/// </summary>
[DataContract]
public class ServiceContext
{
    [DataMember(Name = "Name")]
    public string Name { get; set; }

    [DataMember(Name = "Id")]
    public string Id { get; set; }

    [DataMember(Name = "validLanguages")]
    public List<string> ValidLanguages { get; set; }

    [DataMember(Name = "authorizationRequirements")]
    public List<Requirement> AuthorizationRequirements { get; set; }

    [DataMember(Name = "serviceContextTextTemplate")]
    public IServiceContextTextTemplate<LocalizedString> ServiceContextTextTemplate { get; set; }
}