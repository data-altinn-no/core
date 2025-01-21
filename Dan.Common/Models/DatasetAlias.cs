namespace Dan.Common.Models;

/// <summary>
/// Model for alias for a dataset
/// </summary>
public class DatasetAlias
{
    /// <summary>
    /// Which service context this alias applies to
    /// </summary>
    public string ServiceContext { get; set; } = string.Empty;

    /// <summary>
    /// Alias name
    /// </summary>
    public string DatasetAliasName { get; set; } = string.Empty;
}