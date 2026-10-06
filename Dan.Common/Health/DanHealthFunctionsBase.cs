using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Common.Health;

/// <summary>
/// Convenience base class for exposing the Altinn health endpoints from a Functions app.
/// </summary>
/// <remarks>
/// The isolated worker only indexes functions declared in the entry assembly, so each app must declare the
/// triggers itself. Subclass this and forward each trigger to the matching method, e.g.:
/// <code>
/// public class Health(DanHealthCheckRunner runner) : DanHealthFunctionsBase(runner)
/// {
///     [Function("health-alive")]
///     public Task&lt;HttpResponseData&gt; Alive(
///         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alive")] HttpRequestData req,
///         FunctionContext ctx) => AliveAsync(req, ctx.CancellationToken);
/// }
/// </code>
/// The services are registered by <c>ConfigureDanPluginDefaults()</c> (or <c>AddDanHealthChecks()</c>).
/// </remarks>
public abstract class DanHealthFunctionsBase
{
    private readonly DanHealthCheckRunner _runner;

    /// <summary>
    /// Creates the base with the runner that evaluates and renders health checks.
    /// </summary>
    protected DanHealthFunctionsBase(DanHealthCheckRunner runner)
    {
        _runner = runner;
    }

    /// <summary>Liveness: checks tagged <c>live</c>.</summary>
    protected Task<HttpResponseData> AliveAsync(HttpRequestData req, CancellationToken ct = default) =>
        _runner.RunAsync(req, HealthEndpointKind.Alive, ct);

    /// <summary>Readiness: checks tagged <c>critical</c> or <c>warmup</c>.</summary>
    protected Task<HttpResponseData> ReadinessAsync(HttpRequestData req, CancellationToken ct = default) =>
        _runner.RunAsync(req, HealthEndpointKind.Readiness, ct);

    /// <summary>Startup: checks tagged <c>dependencies</c>.</summary>
    protected Task<HttpResponseData> StartupAsync(HttpRequestData req, CancellationToken ct = default) =>
        _runner.RunAsync(req, HealthEndpointKind.Startup, ct);

    /// <summary>Health dashboard: checks tagged <c>dependencies</c>.</summary>
    protected Task<HttpResponseData> HealthAsync(HttpRequestData req, CancellationToken ct = default) =>
        _runner.RunAsync(req, HealthEndpointKind.Health, ct);

    /// <summary>Deep: checks tagged <c>dependencies</c> or <c>external</c>.</summary>
    protected Task<HttpResponseData> DeepAsync(HttpRequestData req, CancellationToken ct = default) =>
        _runner.RunAsync(req, HealthEndpointKind.Deep, ct);
}
