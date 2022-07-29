using Dan.Common.Models;
using Dan.Core.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Dan.Core.Exceptions;
using Dan.Core.Middleware;

namespace Dan.Core;

/// <summary>
/// The Azure function returning all available status codes.
/// </summary>

public class FuncMetadataStatusCodes
{
    public FuncMetadataStatusCodes()
    {
    }

    /// <summary>
    /// The function entry point.
    /// </summary>
    /// <param name="req">
    /// The HTTP request.
    /// </param>
    /// <param name="context">The execution context</param>
    /// <param name="log">
    /// The logging object.
    /// </param>
    /// <returns>
    /// The <see cref="HttpResponseMessage"/>.
    /// </returns>
    [Function("MetadataStatusCodes")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/statuscodes")] HttpRequestData req)
    {
        throw new InvalidAuthorizationRequestException("Foo bar");

        var response = req.CreateExternalResponse(HttpStatusCode.OK, GetAll());
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        return response;
    }

    private static List<EvidenceStatusCode> GetAll()
    {
        return new List<EvidenceStatusCode>()
        {
            EvidenceStatusCode.Available,
            EvidenceStatusCode.PendingConsent,
            EvidenceStatusCode.Denied,
            EvidenceStatusCode.Expired,
            EvidenceStatusCode.Waiting
        };
    }
}
