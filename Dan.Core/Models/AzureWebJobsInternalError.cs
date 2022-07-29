using Newtonsoft.Json;

namespace Dan.Core.Models;

/// <summary>
/// The azure web jobs internal error.
/// </summary>
public class AzureWebJobsInternalError
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    [JsonProperty("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the request id.
    /// </summary>
    [JsonProperty("requestId")]
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    [JsonProperty("statusCode")]
    public long StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonProperty("errorCode")]
    public long ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the error details.
    /// </summary>
    [JsonProperty("errorDetails")]
    public string ErrorDetails { get; set; }
}
