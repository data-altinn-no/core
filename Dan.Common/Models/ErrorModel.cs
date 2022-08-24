namespace Dan.Common.Models;

/// <summary>
/// Describing an error state
/// </summary>
[DataContract]
public class ErrorModel
{
    /// <summary>
    /// Error code
    /// </summary>
    [Required]
    [DataMember(Name = "code")]
    public int? Code { get; set; }

    /// <summary>
    /// Error description
    /// </summary>
    [DataMember(Name = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Evidence Source specific error code
    /// </summary>
    [DataMember(Name = "detailCode", EmitDefaultValue = false)]
    public string? DetailCode { get; set; }

    /// <summary>
    /// Evidence Source specific error description
    /// </summary>
    [DataMember(Name = "detailDescription", EmitDefaultValue = false)]
    public string? DetailDescription { get; set; }

    /// <summary>
    /// Stack trace (only available in dev)
    /// </summary>
    [DataMember(Name = "stacktrace", EmitDefaultValue = false)]
    public string? Stacktrace { get; set; }

    /// <summary>
    /// Inner exception message (only available in dev)
    /// </summary>
    [DataMember(Name = "innerExceptionMessage", EmitDefaultValue = false)]
    public string? InnerExceptionMessage { get; set; }

    /// <summary>
    /// Inner exception stack trace (only available in dev)
    /// </summary>
    [DataMember(Name = "innerExceptionStackTrace", EmitDefaultValue = false)]
    public string? InnerExceptionStackTrace { get; set; }
}