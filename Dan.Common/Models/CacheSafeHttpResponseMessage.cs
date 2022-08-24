namespace Dan.Common.Models;

/// <summary>
/// Cache Safe Http Response Message for use with JSON serialization
/// </summary>
public class CacheSafeHttpResponseMessage
{
    /// <summary>
    /// The runtime of the request
    /// </summary>
    public TimeSpan Runtime { get; private set; }

    /// <summary>
    /// If the request was from cache
    /// </summary>
    public bool FromCache => !LiveRequest;

    /// <summary>
    /// If the request was sent
    /// </summary>
    public bool LiveRequest { get; private set; }

    /// <summary>
    /// Http Content
    /// </summary>
    public CacheSafeHttpContent Content { get; set; }

    /// <summary>
    /// Request Headers
    /// </summary>
    public CacheSafeHttpHeaders Headers { get; set; }

    /// <summary>
    /// If the HTTP Status Code is in the 200-range
    /// </summary>
    public bool IsSuccessStatusCode { get; set; }

    /// <summary>
    /// Reason Phrase
    /// </summary>
    public string? ReasonPhrase { get; set; }

    /// <summary>
    /// HTTP Status Code
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Create a new CacheSafeHttpResponseMessage from a HttpResponseMessage
    /// </summary>
    /// <param name="response">The response message</param>
    /// <returns>A Task for getting CacheSafeHttpResponseMessage</returns>
    public static async Task<CacheSafeHttpResponseMessage> CreateInstance(HttpResponseMessage response)
    {
        return new CacheSafeHttpResponseMessage
        {
            Headers = new CacheSafeHttpHeaders(response.Headers),
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            Content = await CacheSafeHttpContent.CreateInstance(response.Content)
        };
    }

    /// <summary>
    /// Convert to String
    /// </summary>
    /// <returns>Object as string</returns>
    public override string ToString()
    {
        return $"{StatusCode}, {Content.ToString()}";
    }

    /// <summary>
    /// Cache Safe Http Response Message for use with JSON serialization
    /// </summary>
    public CacheSafeHttpResponseMessage()
    {
        Content = new CacheSafeHttpContent();
        Headers = new CacheSafeHttpHeaders();
    }

    /// <summary>
    /// Set the runtime of the request
    /// </summary>
    /// <param name="elapsed">Duration of the request</param>
    public void SetRunTime(TimeSpan elapsed)
    {
        Runtime = elapsed;
        LiveRequest = true;
    }
}

/// <summary>
/// Cache Safe Http Content for use with JSON serialization
/// </summary>
public class CacheSafeHttpContent
{
    /// <summary>
    /// Http Content Headers
    /// </summary>
    public CacheSafeHttpHeaders Headers { get; set; }

    /// <summary>
    /// Http Content
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Create a new CacheSafeHttpContent from a HttpContent
    /// </summary>
    /// <param name="content">The http content</param>
    /// <returns>Task for getting CacheSafeHttpContent</returns>
    public static async Task<CacheSafeHttpContent> CreateInstance(HttpContent content)
    {
        var httpContent = new CacheSafeHttpContent
        {
            Headers = new CacheSafeHttpHeaders(content.Headers),
            Content = await content.ReadAsStringAsync()
        };

        return httpContent;
    }

    /// <summary>
    /// Cache Safe Http Content for use with JSON serialization
    /// </summary>
    public CacheSafeHttpContent()
    {
        Headers = new CacheSafeHttpHeaders();
        Content = string.Empty;
    }

    /// <summary>
    /// Read Http Content as a string
    /// </summary>
    /// <returns>String as Task</returns>
    public Task<string> ReadAsStringAsync()
    {
        return Task.FromResult(Content);
    }

    /// <summary>
    /// Read Http Content as a type
    /// </summary>
    /// <returns>The object as Task</returns>
    /// <typeparam name="T">Type to return as</typeparam>
    public Task<T?> ReadAsAsync<T>()
    {
        var obj = JsonConvert.DeserializeObject<T>(Content, new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
        return Task.FromResult(obj);
    }

    /// <summary>
    /// Convert to String
    /// </summary>
    /// <returns>Object as string</returns>
    public override string ToString()
    {
        return Content;
    }
}

/// <summary>
/// Cache safe http headers
/// </summary>
public class CacheSafeHttpHeaders : List<KeyValuePair<string, List<string>>>
{
    /// <summary>
    /// Cache safe http headers
    /// </summary>
    public CacheSafeHttpHeaders()
    {
    }

    /// <summary>
    /// Cache safe http headers
    /// </summary>
    /// <param name="headers">Http Headers</param>
    public CacheSafeHttpHeaders(HttpHeaders headers)
    {
        foreach (var pair in headers)
        {
            Add(new KeyValuePair<string, List<string>>(pair.Key, pair.Value.ToList()));
        }
    }

    /// <summary>
    /// Get values
    /// </summary>
    /// <param name="key">Name of values</param>
    /// <returns>List of values for the given key</returns>
    public List<string> GetValues(string key)
    {
        var found = this.FirstOrDefault(x => x.Key == key);
        return found.Value;
    }
}