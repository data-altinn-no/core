using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Filters;
using Newtonsoft.Json;
using System.Collections;
using System.Net;
using System.Text;
using Dan.Common.Helpers.Extensions;
using Dan.Core.Middleware;
using Dan.Core.Models;

namespace Dan.Core.Helpers;

/// <summary>
/// Helper methods for communicating with evidence sources and handling errors from it
/// </summary>
public static class EvidenceSourceHelper
{
    private const int ERROR_EMPTY_NONSUCESSFUL_RESPONSE = 5001;
    private const int ERROR_UNHANDLED_INTERNAL_ERROR = 5002;
    private const int ERROR_UNABLE_TO_DESERIALIZE = 5003;
    private const int ERROR_INVALID_ERROR_MODEL = 5004;

    /// <summary>
    /// Cache time span for checking status on asynchronous evidence codes
    /// </summary>
    private static TimeSpan AsyncEvidenceStatusCacheTimeSpan { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Method for wrapping calls to the HTTP client which handles errors from the evidence source. 
    /// The evidence source can choose to handle errors by throwing one of the three Evidence Source exceptions, 
    /// which has an Error Model in the body of the response. In addition, unhandled errors in the evidence source,
    /// like null dereference exceptions, that results in a 500 Internal Server Error and a Azure Web Jobs error model
    /// being returned, is also caught and re-thrown. Note that this circumvents the circuit breaker except for those
    /// errors being caused by the evidence source being unreachable (due to downtime or slow response).
    /// </summary>
    /// <typeparam name="T">Expected model type the response should have</typeparam>
    /// <param name="req">The request message to be used</param>
    /// <param name="func">The delegate containing the actual call to the HTTP client</param>
    /// <returns>The expected model, or will throw one of the evidence source exceptions</returns>
    public static async Task<T?> DoRequest<T>(HttpRequestMessage req, Func<Task<HttpResponseMessage>> func)
    {
        req.SetAllowedErrorCodes(
                       HttpStatusCode.BadRequest,
                       HttpStatusCode.BadGateway,
                       HttpStatusCode.ServiceUnavailable,
                       HttpStatusCode.InternalServerError); // This is for unhandled errors in the ES causing a internal error model to be returned

        var response = await func.Invoke();
        if (response.Content == null)
        {
            // Allow for empty successful responses, for instance 202 Accepted when initializing asyncronous evidence codes
            if (response.IsSuccessStatusCode)
            {
                return default;
            }

            throw new EvidenceSourcePermanentServerException(ERROR_EMPTY_NONSUCESSFUL_RESPONSE, "Empty non-sucessful response from evidence source", new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}"));
        }

        var json = await response.Content.ReadAsStringAsync();

        var deserializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error
        };

        try
        {
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var internalError = JsonConvert.DeserializeObject<AzureWebJobsInternalError>(json, deserializerSettings);
                EvidenceSourcePermanentServerException ex;
                if (internalError != null)
                {
                    ex = new EvidenceSourcePermanentServerException(
                        ERROR_UNHANDLED_INTERNAL_ERROR,
                        "An unhandled internal error occurred in the evidence source",
                        new ProxiedException(internalError.Message))
                    { InnerStackTrace = internalError.ErrorDetails };
                }
                else
                {
                    ex = new EvidenceSourcePermanentServerException(
                        ERROR_UNHANDLED_INTERNAL_ERROR,
                        "An unhandled internal error occurred in the evidence source");
                }

                throw ex;
            }

            return JsonConvert.DeserializeObject<T>(json, deserializerSettings);
        }
        catch (JsonException)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<ErrorModel>(json, deserializerSettings);
                throw GetExceptionInstanceFromErrorModel(req, error);
            }
            catch (JsonException exjson)
            {
                throw new EvidenceSourcePermanentServerException(ERROR_UNABLE_TO_DESERIALIZE, "Unable to deserialize response from evidence source", exjson);
            }
        }
    }

    /// <summary>
    /// Helper method for initializing asynchronous evidence codes to the evidence source
    /// </summary>
    /// <param name="accreditation">The associated accreditation</param>
    /// <param name="evidenceCode">The evidence code to initialize</param>
    /// <param name="client">The HTTP client</param>
    /// <returns>Nothing, but will throw if the call fails or the evidence source reports and error</returns>
    public static async Task InitAsynchronousEvidenceCodeRequest(Accreditation accreditation, EvidenceCode evidenceCode, HttpClient client)
    {
        var url = evidenceCode.GetEvidenceSourceUrl();

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add("Accept", "application/json");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var evidenceHarvesterRequest = new EvidenceHarvesterRequest()
        {
            OrganizationNumber = accreditation.Subject,
            SubjectParty = accreditation.SubjectParty,
            Requestor = accreditation.Requestor,
            RequestorParty = accreditation.RequestorParty,
            ServiceContext = accreditation.ServiceContext,
            EvidenceCodeName = evidenceCode.EvidenceCodeName,
            AccreditationId = accreditation.AccreditationId,
            AsyncEvidenceCodeAction = AsyncEvidenceCodeAction.Initialize,
            Parameters = evidenceCode.Parameters
        };

        request.JsonContent(evidenceHarvesterRequest);

        await DoRequest<string>(request, () => client.SendAsync(request, cts.Token));
    }

    private static EvidenceSourceException GetExceptionInstanceFromErrorModel(HttpRequestMessage req, ErrorModel error)
    {
        EvidenceSourceException ex;
        if (!error.Code.HasValue)
        {
            ex = new EvidenceSourcePermanentServerException(ERROR_INVALID_ERROR_MODEL, $"Invalid error model returned from evidence source, message was: {error.Description ?? "(not set)"}");
        }
        else
        {
            switch (error.Code)
            {
                case (int)ErrorCode.EvidenceSourceTransientException:
                    ex = new EvidenceSourceTransientException(
                        Convert.ToInt32(error.DetailCode),
                        null,
                        GetEvidenceSourceInnerException(error));
                    break;
                case (int)ErrorCode.EvidenceSourcePermanentClientException:
                    ex = new EvidenceSourcePermanentClientException(
                        Convert.ToInt32(error.DetailCode),
                        null,
                        GetEvidenceSourceInnerException(error));
                    break;
                case (int)ErrorCode.EvidenceSourcePermanentServerException:
                    ex = new EvidenceSourcePermanentServerException(
                        Convert.ToInt32(error.DetailCode),
                        null,
                        GetEvidenceSourceInnerException(error));
                    break;
                default:
                    ex = new EvidenceSourcePermanentServerException(
                        ERROR_INVALID_ERROR_MODEL,
                        $"Invalid error model returned from evidence source. Code and message was: ({error.Code}) {error.Description ?? "(not set)"}");
                    break;
            }
        }

        ex.DetailErrorSource = GetEvidenceSourceIdentifier(req);
        ex.DetailErrorDescription = GetEvidenceSourceErrorMessage(error);
        ex.InnerStackTrace = error.Stacktrace;
        return ex;
    }

    private static string GetEvidenceSourceErrorMessage(ErrorModel error)
    {
        // Avoid duplicating default text. If a custom text is supplied it will be after the first ": "
        if (error.Description.Contains(": "))
        {
            return error.Description.Substring(error.Description.IndexOf(": ", StringComparison.Ordinal) + 2);
        }

        return string.Empty;
    }

    /// <summary>
    /// Takes a HTTP request object for a request to an evidence source, and determines the evidence source identifier base on application settings
    /// </summary>
    /// <param name="req">A request object for a evidence source</param>
    /// <returns>The identifier for the evidence source in upper case</returns>
    private static string GetEvidenceSourceIdentifier(HttpRequestMessage req)
    {
        var needle = "-t-e-m-p-";
        var srchost = req.RequestUri.Host;
        var pattern = new Uri(Settings.GetEvidenceSourceUrl(needle)).Host;
        var startpos = pattern.IndexOf(needle, StringComparison.Ordinal);
        if (startpos == -1)
        {
            return "ES";
        }

        var endpos = startpos + needle.Length;
        var before = pattern.Substring(0, startpos);
        var after = pattern.Substring(endpos);
        var id = srchost;
        if (!string.IsNullOrEmpty(before))
        {
            id = srchost.Replace(before, string.Empty);
        }

        if (!string.IsNullOrEmpty(after))
        {
            id = id.Replace(after, string.Empty);
        }

        return id.ToUpper();
    }

    private static Exception GetEvidenceSourceInnerException(ErrorModel error)
    {
        return string.IsNullOrEmpty(error.InnerExceptionMessage) ? null : new Exception(error.InnerExceptionMessage);
    }
}
