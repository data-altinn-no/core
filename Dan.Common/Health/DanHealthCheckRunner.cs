using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dan.Common.Health;

/// <summary>
/// Evaluates the health checks that belong to a given endpoint kind and renders the result.
/// HTTP-trigger functions delegate to this so each function body is a one-liner.
/// </summary>
public sealed class DanHealthCheckRunner
{
    private readonly HealthCheckService _healthCheckService;
    private readonly HttpRequestDataHealthReportWriter _writer;

    /// <summary>
    /// Creates a runner.
    /// </summary>
    public DanHealthCheckRunner(HealthCheckService healthCheckService, HttpRequestDataHealthReportWriter writer)
    {
        _healthCheckService = healthCheckService;
        _writer = writer;
    }

    /// <summary>
    /// Runs the checks selected by <paramref name="kind"/> and writes the report to a response for <paramref name="request"/>.
    /// </summary>
    public async Task<HttpResponseData> RunAsync(HttpRequestData request, HealthEndpointKind kind, CancellationToken cancellationToken = default)
    {
        var report = await _healthCheckService.CheckHealthAsync(HealthEndpointPredicates.For(kind), cancellationToken);
        return await _writer.WriteAsync(request, report, cancellationToken);
    }
}
