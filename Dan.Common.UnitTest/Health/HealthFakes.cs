using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dan.Common.UnitTest.Health;

internal static class HealthFakes
{
    /// <summary>
    /// Fakes an HttpRequestData whose CreateResponse() yields a response with a real header collection and body stream,
    /// following the pattern used in Dan.Core.UnitTest.
    /// </summary>
    public static HttpRequestData Request(string? accept = null)
    {
        var functionContext = A.Fake<FunctionContext>();

        var request = A.Fake<HttpRequestData>(o => o.WithArgumentsForConstructor([functionContext]));
        A.CallTo(() => request.Url).Returns(new Uri("https://localhost/api/health"));
        var headers = new HttpHeadersCollection();
        if (accept is not null)
        {
            headers.Add("Accept", accept);
        }
        A.CallTo(() => request.Headers).Returns(headers);

        var response = A.Fake<HttpResponseData>(o => o.WithArgumentsForConstructor([functionContext]));
        response.Headers = new HttpHeadersCollection();
        response.Body = new MemoryStream();
        A.CallTo(() => request.CreateResponse()).Returns(response);

        return request;
    }

    public static string BodyAsString(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return reader.ReadToEnd();
    }

    public static HealthReport Report(params (string Name, HealthStatus Status, string? Description, Exception? Exception, string[] Tags)[] entries)
    {
        var dict = entries.ToDictionary(
            e => e.Name,
            e => new HealthReportEntry(
                e.Status,
                e.Description,
                TimeSpan.FromMilliseconds(7),
                e.Exception,
                new Dictionary<string, object> { ["pool"] = "primary" },
                e.Tags));
        return new HealthReport(dict, TimeSpan.FromMilliseconds(41));
    }
}
