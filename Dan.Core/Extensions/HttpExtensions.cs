using System.Collections;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Helpers;
using Dan.Core.Middleware;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core.Extensions;

/// <summary>
/// Object extensions
/// </summary>
public static class HttpExtensions
{
    public static readonly HttpRequestOptionsKey<List<HttpStatusCode>> AllowedStatusCodes = new("AllowedStatusCodes");

    /// <summary>
    /// Get a single header from a request. If multiple are found, only return the first item
    /// </summary>
    /// <param name="headers">Http headers</param>
    /// <param name="key">Header key</param>
    /// <returns>Header value</returns>
    public static string? Get(this HttpHeaders headers, string key)
    {
        return headers.Where(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Value).FirstOrDefault();
    }

    /// <summary>
    /// Add response content as JSON
    /// </summary>
    /// <typeparam name="T">Object type to serialize to JSON</typeparam>
    /// <param name="response">The http response</param>
    /// <param name="data">The object to serialize</param>
    /// <returns>The http message</returns>
    public static HttpResponseMessage JsonContent<T>(this HttpResponseMessage response, T data)
    {
        response.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        return response;
    }

    /// <summary>
    /// Get the value of a query parameter or <c>null</c> if not found.
    /// </summary>
    /// <param name="req">A <see cref="HttpRequestData"/> from the specified query parameter value should be found.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The value of a query parameter or <c>null</c> if not found.</returns>
    public static string? GetQueryParam(this HttpRequestData req, string name)
    {
        return req.GetQueryParams().Get(name);
    }

    /// <summary>
    /// Gets all query parameteres.
    /// </summary>
    /// <param name="req">A <see cref="HttpRequestData"/> from the specified query parameter value should be found.</param>
    /// <returns>The value of a query parameter or <c>null</c> if not found.</returns>
    public static NameValueCollection GetQueryParams(this HttpRequestData req)
    {
        return System.Web.HttpUtility.ParseQueryString(req.Url.Query);
    }

    /// <summary>
    /// Returns true if the given query parameter has been supplied in the request
    /// </summary>
    /// <param name="req">A <see cref="HttpRequestData"/> from the specified query parameter value should be found.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>Returns <c>true</c> if set, otherwise <c>false</c></returns>
    public static bool HasQueryParam(this HttpRequestData req, string name)
    {
        return req.GetQueryParam(name) != null;
    }

    /// <summary>
    /// Get the boolean value of a query parameter.
    /// </summary>
    /// <param name="req">A <see cref="HttpRequestData"/> from the specified query parameter value should be found.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>Returns <c>false</c> if not found, "false" (insenstive) or "0"; otherwise <c>true</c></returns>
    public static bool GetBoolQueryParam(this HttpRequestData req, string name)
    {
        var val = req.GetQueryParam(name);
        if (val == null) return false;

        switch (val.ToLowerInvariant())
        {
            case "0":
            case "false":
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Get the certificate org number from a request
    /// </summary>
    /// <param name="request">The request message</param>
    /// <returns>The organization number in a certificate</returns>
    public static string GetAuthenticatedPartyOrgNumber(this HttpRequestData request)
    {
        if (!request.FunctionContext.Items.TryGetValue(Constants.AUTHENTICATED_ORGNO, out var orgNumber))
        {
            throw new MissingAuthenticationException();
        }

        return orgNumber.ToString()!;
    }

    /// <summary>
    /// Gets the unmasked subject identifier if supplied in POST. Does not attempt to do any parsing, returns null if unable to deserialize .
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static async Task<string?> GetSubjectIdentifierFromPost(this HttpRequestData request)
    {
        try
        {
            var subject = await request.ReadFromJsonAsync<Subject>();
            return subject?.SubjectId!;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public static List<string> GetMaskinportenScopes(this HttpRequestData request)
    {
        if (!request.FunctionContext.Items.TryGetValue(Constants.SCOPES, out var scopes))
        {
            return new List<string>();
        }

        return (List<string>)scopes;
    }

    public static string? GetAuthorizationToken(this HttpRequestData request)
    {
        if (!request.FunctionContext.Items.TryGetValue(Constants.ACCESS_TOKEN, out var accessToken))
        {
            return null;
        }

        return (string)accessToken;
    }

    public static string? GetSubscriptionKey(this HttpRequestData request)
    {
        return !request.Headers.TryGetValues(Constants.SUBSCRIPTION_KEY_HEADER, out var headers) 
            ? null 
            : headers.First();
    }

    /// <summary>
    /// Create a html response
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="statusCode">The status code</param>
    /// <param name="view">The view</param>
    /// <param name="viewData">The view data</param>
    /// <returns>A html response message</returns>
    public static HttpResponseData CreateHtmlResponse(this HttpRequestData request, HttpStatusCode statusCode, string view, object viewData)
    {
        var response = request.CreateResponse(statusCode);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Dan.Core.Views.{view}";

        var textView = string.Empty;

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                using StreamReader reader = new StreamReader(stream);
                textView = reader.ReadToEnd();
            }
        }

        var replacements = viewData.GetType().GetProperties()
            .ToDictionary(x => x.Name, x => x.GetValue(viewData)?.ToString() ?? string.Empty);

        foreach (var pair in replacements.OrderByDescending(x => x.Key.Length))
        {
            textView = textView.Replace($"@{pair.Key}", pair.Value);
        }

        response.Headers.Add("Content-Type", "text/html");
        response.WriteStringAsync(textView);

        return response;
    }

    /// <summary>
    /// Create a http response message using custom contract resolver
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="httpStatusCode">The HTTP status code that will be returned</param>
    /// <param name="content">The content of the response.</param>
    /// <returns>A json response message. All attributes marked as hidden in the content model will be removed</returns>
    public static HttpResponseData CreateExternalResponse<T>(this HttpRequestData request, HttpStatusCode httpStatusCode, T content)
    {
        var response = request.CreateResponse(httpStatusCode);
        var json = JsonConvert.SerializeObject(content, new JsonSerializerSettings { ContractResolver = new HiddenPropertyContractResolver() });
        JmesPathTransfomer.Apply(request.GetQueryParam(JmesPathTransfomer.QueryParameter), ref json);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteStringAsync(json);
        return response;
    }


    /// <summary>
    /// Add allowed error status codes for an individual request
    /// </summary>
    /// <param name="request">The http request message</param>
    /// <param name="codes">The allowed error status codes</param>
    public static void SetAllowedErrorCodes(this HttpRequestMessage request, params HttpStatusCode[] codes)
    {
        request.Options.Set(AllowedStatusCodes, codes.ToList());
    }

    /// <summary>
    /// Get allowed error status codes for an individual request
    /// </summary>
    /// <param name="request">The http request message</param>
    /// <returns>A list of allowed http status codes</returns>
    public static List<HttpStatusCode> GetAllowedErrorCodes(this HttpRequestMessage request)
    {
        return request.Options.TryGetValue(HttpExtensions.AllowedStatusCodes, out var allowedStatusCodes) 
            ? allowedStatusCodes 
            : new List<HttpStatusCode>();
    }

    /// <summary>
    /// Add request content as JSON and set method to Post
    /// </summary>
    /// <typeparam name="T">Object type to serialize to JSON</typeparam>
    /// <param name="request">The Http request</param>
    /// <param name="data">The object to serialize</param>
    /// <returns>The http message</returns>
    public static HttpRequestMessage JsonContent<T>(this HttpRequestMessage request, T data)
    {
        request.Method = HttpMethod.Post;
        request.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        return request;
    }

    public static async Task SetUnenvelopedEvidenceValuesAsync(this HttpResponseData response, List<EvidenceValue> evidenceValues, string? jmesExpression = null)
    {
        if (!evidenceValues.Any())
        {
            return;
        }

        string jsonResult;
        if (evidenceValues.Count > 1)
        {
            var asHashTable = ConvertToHashtable(evidenceValues);
            jsonResult = JsonConvert.SerializeObject(asHashTable, new JsonSerializerSettings { ContractResolver = new HiddenPropertyContractResolver() });
            JmesPathTransfomer.Apply(jmesExpression, ref jsonResult);
            await response.WriteStringAsync(jsonResult);
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("X-Signature-JWT", Jwt.GetDigestJwt(jsonResult));
            return;
        }

        var evidence = evidenceValues.First();

        switch (evidence.ValueType)
        {
            case EvidenceValueType.JsonSchema:
                jsonResult = (string?)evidence.Value ?? string.Empty;
                JmesPathTransfomer.Apply(jmesExpression, ref jsonResult);
                await response.WriteStringAsync(jsonResult);
                response.Headers.Add("Content-Type", "application/json");
                response.Headers.Add("X-Signature-JWT", Jwt.GetDigestJwt(jsonResult));
                break;
            case EvidenceValueType.Attachment:
                await response.WriteBytesAsync(evidence.Value == null ? Array.Empty<byte>() : Convert.FromBase64String((string)evidence.Value));
                response.Headers.Add("Content-Type", "application/octet-stream");
                // TODO! How to calculate digest?
                break;
            default:
                jsonResult = JsonConvert.SerializeObject(evidence.Value, new JsonSerializerSettings { ContractResolver = new HiddenPropertyContractResolver() });
                JmesPathTransfomer.Apply(jmesExpression, ref jsonResult);
                await response.WriteStringAsync(jsonResult);
                response.Headers.Add("Content-Type", "application/json");
                response.Headers.Add("X-Signature-JWT", Jwt.GetDigestJwt(jsonResult));
                break;
        }
    }

    public static async Task SetEvidenceAsync(this HttpResponseData response, Evidence evidence)
    {
        var jsonResult = JsonConvert.SerializeObject(evidence, new JsonSerializerSettings { ContractResolver = new HiddenPropertyContractResolver() });
        await response.WriteStringAsync(jsonResult);
        response.Headers.Add("Content-Type", "application/json");
        response.Headers.Add("X-Signature-JWT", Jwt.GetDigestJwt(jsonResult));
    }

    private static Hashtable ConvertToHashtable(List<EvidenceValue> evidenceValues)
    {
        var hashTable = new Hashtable();
        foreach (var evidenceValue in evidenceValues)
            hashTable.Add(evidenceValue.EvidenceValueName, evidenceValue.Value);

        return hashTable;
    }
}