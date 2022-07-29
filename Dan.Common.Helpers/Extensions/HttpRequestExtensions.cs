namespace Dan.Common.Helpers.Extensions;

/// <summary>
/// HTTP request extensions
/// </summary>
public static class HttpRequestExtensions
{
    private const string TimeoutPropertyKey = "RequestTimeout";

    /// <summary>
    /// Timeout for this request. Will cause the Safe Http Client to throw a Timeout Exception if passed
    /// </summary>
    /// <param name="request">
    /// The request.
    /// </param>
    /// <param name="timeout">
    /// The timeout.
    /// </param>
    [Obsolete("SetTimeout is deprecated, please use a cancellation token instead")]
    public static void SetTimeout(this HttpRequestMessage request, TimeSpan timeout)
    {
#pragma warning disable CS0618
        request.Properties[TimeoutPropertyKey] = timeout;
#pragma warning restore CS0618
    }

    /// <summary>
    /// Timeout for this request. Will cause the Safe Http Client to throw a Timeout Exception if passed
    /// </summary>
    /// <param name="request">
    /// The request.
    /// </param>
    /// <returns>
    /// The <see cref="TimeSpan"/> representing the timeout if set, else null.
    /// </returns>
    [Obsolete("GetTimeout is deprecated, please use a cancellation token instead")]
    public static TimeSpan? GetTimeout(this HttpRequestMessage request)
    {
#pragma warning disable CS0618
        if (request.Properties.TryGetValue(TimeoutPropertyKey, out var value) && value is TimeSpan timeout)
#pragma warning restore CS0618
        {
            return timeout;
        }

        return null;
    }
}
