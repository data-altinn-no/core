namespace Dan.Common.Models;

/// <summary>
/// Localization values for text settings per service context defaulting to NoNb if EN or NN are not set in template
/// </summary>
public class LocalizedString
{
    public string? En { get; set; }
    public string? NoNb { get; set; }
    public string? NoNn { get; set; }

}