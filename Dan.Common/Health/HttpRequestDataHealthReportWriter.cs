using System.Net;
using Altinn.AspNet.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;

namespace Dan.Common.Health;

/// <summary>
/// Renders a <see cref="HealthReport"/> into an <see cref="HttpResponseData"/> using the formatters from
/// Altinn.AspNet.HealthChecks, so Functions apps produce the same payloads
/// (<c>application/vnd.altinn.health.v1+json</c> / <c>text/plain</c>, same detail levels) as ASP.NET Core apps
/// that call <c>MapAltinnHealthChecks()</c>.
/// </summary>
/// <remarks>
/// The library writer only knows ASP.NET Core's <see cref="HttpContext"/>. We run it against an in-memory
/// <see cref="DefaultHttpContext"/> and copy the negotiated content type and body over to the Functions response.
/// </remarks>
public sealed class HttpRequestDataHealthReportWriter
{
    private readonly HealthReportResponseWriter _writer;

    /// <summary>
    /// The detail level this writer renders with.
    /// </summary>
    public HealthReportDetailLevel DetailLevel { get; }

    /// <summary>
    /// Creates a writer that renders with the given detail level using the library's JSON and plain-text formatters.
    /// </summary>
    public HttpRequestDataHealthReportWriter(HealthReportDetailLevel detailLevel)
        : this(detailLevel, [new HealthReportJsonFormatter(), new HealthReportTextFormatter()])
    {
    }

    /// <summary>
    /// Creates a writer with custom formatters. The first formatter is the fallback when the request has no acceptable <c>Accept</c>.
    /// </summary>
    public HttpRequestDataHealthReportWriter(HealthReportDetailLevel detailLevel, IEnumerable<HealthReportFormatter> formatters)
    {
        DetailLevel = detailLevel;
        _writer = new HealthReportResponseWriter(detailLevel, formatters);
    }

    /// <summary>
    /// Writes the report as a response to <paramref name="request"/>. Status is 200 for Healthy/Degraded and 503 for Unhealthy,
    /// matching the ASP.NET Core health check middleware defaults used by the library endpoints.
    /// </summary>
    public async Task<HttpResponseData> WriteAsync(HttpRequestData request, HealthReport report, CancellationToken cancellationToken = default)
    {
        var httpContext = new DefaultHttpContext { RequestAborted = cancellationToken };
        if (request.Headers.TryGetValues("Accept", out var accept))
        {
            httpContext.Request.Headers.Accept = new StringValues(accept.ToArray());
        }

        var bodyBuffer = new MemoryStream();
        httpContext.Response.Body = bodyBuffer;

        await _writer.WriteAsync(httpContext, report);
        await httpContext.Response.BodyWriter.CompleteAsync();

        var response = request.CreateResponse(ToStatusCode(report.Status));
        if (!string.IsNullOrEmpty(httpContext.Response.ContentType))
        {
            response.Headers.Add("Content-Type", httpContext.Response.ContentType);
        }

        response.Headers.Add("Cache-Control", "no-store, no-cache");
        response.Headers.Add("Pragma", "no-cache");

        bodyBuffer.Position = 0;
        await bodyBuffer.CopyToAsync(response.Body, cancellationToken);

        return response;
    }

    /// <summary>
    /// Maps an aggregate health status to the HTTP status code probes expect.
    /// </summary>
    public static HttpStatusCode ToStatusCode(HealthStatus status) =>
        status == HealthStatus.Unhealthy ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
}
