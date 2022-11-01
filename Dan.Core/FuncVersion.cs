using System.Net;
using Dan.Core.Attributes;
using Dan.Core.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core;

/// <summary>
/// Class for function showing version information
/// </summary>
public class FuncVersion
{
    /// <summary>
    /// Endpoint showing version information
    /// </summary>
    /// <param name="req">The request</param>
    /// <returns>The response</returns>
    [Function("version"), NoAuthentication]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(VersionHelper.GetVersionInfo());

        return response;
    }
}