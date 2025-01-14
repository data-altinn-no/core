using Dan.Common.Interfaces;

namespace Dan.Common.Models;

/// <summary>
/// Class describing Service Context
/// </summary>
[DataContract]
public class ServiceContext
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "id")]
    public string Id { get; set; } = string.Empty;
    
    [DataMember(Name = "description")]
    public string? Description { get; set; }

    [DataMember(Name = "validLanguages")]
    public List<string> ValidLanguages { get; set; } = new();

    [DataMember(Name = "authorizationRequirements")]
    public List<Requirement> AuthorizationRequirements { get; set; } = new();

    [DataMember(Name = "serviceContextTextTemplate")]
    public IServiceContextTextTemplate<LocalizedString>? ServiceContextTextTemplate { get; set; }
}