using Dan.Common.Interfaces;

namespace Dan.Common.Models;

/// <summary>
/// Class describing Service Context
/// </summary>
[DataContract]
public class ServiceContext
{
    /// <summary>
    /// Name of the service context
    /// </summary>
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the service context
    /// </summary>
    [DataMember(Name = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Organisation managing/orchestrating the service context towards consumers and providers
    /// </summary>
    [DataMember(Name = "owner")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Description of the service context
    /// </summary>
    [DataMember(Name = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// List of languages that are valid for the service context
    /// </summary>
    [DataMember(Name = "validLanguages")]
    public List<string> ValidLanguages { get; set; } = new();

    /// <summary>
    /// List of requirements needed to access datasets in the service context
    /// </summary>
    [DataMember(Name = "authorizationRequirements")]
    public List<Requirement> AuthorizationRequirements { get; set; } = new();

    /// <summary>
    /// Text templates with localised values for the service context
    /// </summary>
    [DataMember(Name = "serviceContextTextTemplate")]
    public IServiceContextTextTemplate<LocalizedString>? ServiceContextTextTemplate { get; set; }
}