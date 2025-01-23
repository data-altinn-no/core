namespace Dan.Common.Models;

/// <summary>
/// Localization values for text settings per service context defaulting to NoNb if EN or NN are not set in template
/// </summary>
public class LocalizedString
{
    /// <summary>
    /// English
    /// </summary>
    public string? En { get; set; }
    
    /// <summary>
    /// Bokmål
    /// </summary>
    public string? NoNb { get; set; }
    
    /// <summary>
    /// Nynorsk
    /// </summary>
    public string? NoNn { get; set; }

}