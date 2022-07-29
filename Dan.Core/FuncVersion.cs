using System.Net;
using System.Reflection;
using Dan.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core
{
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
                version = assembly.GetName().Version?.ToString(),
                built = GetLinkerTimestampUtc(assembly).ToString("u"),
                commit = GetGitHash(assembly)
            };

            var response = req.CreateResponse();
            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(output);

            return response;
        }

        private static DateTime GetLinkerTimestampUtc(Assembly assembly)
        {
            var location = assembly.Location;
            return GetLinkerTimestampUtc(location);
        }

        private static DateTime GetLinkerTimestampUtc(string filePath)
        {
            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8;
            var bytes = new byte[2048];

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var unused = file.Read(bytes, 0, bytes.Length);
            }

            var headerPos = BitConverter.ToInt32(bytes, peHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(bytes, headerPos + linkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return dt.AddSeconds(secondsSince1970);
        }

        private static string? GetGitHash(Assembly assembly)
        {
            var attrs = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
            return attrs.FirstOrDefault(a => a.Key == "GitHash")?.Value;
        }
    }
}
