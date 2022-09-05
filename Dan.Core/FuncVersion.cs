using System.Globalization;
using System.Net;
using System.Reflection;
using Dan.Core.Attributes;
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
        var assembly = Assembly.GetExecutingAssembly();
        var output = new
        {
            name = assembly.GetName().Name,
            built = GetBuildDate(assembly).ToString("u"),
            commit = ThisAssembly.Git.Commit
        };

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        await response.WriteAsJsonAsync(output);

        return response;
    }


    private static DateTime GetBuildDate(Assembly assembly)
    {
        const string buildVersionMetadataPrefix = "+build";

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(buildVersionMetadataPrefix, StringComparison.Ordinal);
            if (index > 0)
            {
                value = value[(index + buildVersionMetadataPrefix.Length)..];
                if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }
        }

        return default;
    }
}