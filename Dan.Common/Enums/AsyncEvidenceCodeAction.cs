namespace Dan.Common.Enums;

/// <summary>
/// Different actions for asynchronous evidence codes
/// </summary>
public enum AsyncEvidenceCodeAction
{
    /// <summary>
    /// For initializing an async evidence code request
    /// </summary>
    Initialize = 0,

    /// <summary>
    /// For checking the status of an async evidence code request
    /// </summary>
    CheckStatus = 1,

    /// <summary>
    /// For harvesting a ready async evidence code.
    /// </summary>
    Harvest = 2,

    /// <summary>
    /// For cancelling an async evidence code request
    /// </summary>
    Cancel = 3
}