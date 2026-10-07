using Dan.Common.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Common.Health;

/// <summary>
/// The Altinn health endpoints (https://github.com/Altinn/altinn-aspnet-healthchecks) for every DAN Functions app.
/// </summary>
/// <remarks>
/// <para>
/// The Functions worker SDK indexes functions declared in referenced assemblies, so any app that references
/// Dan.Common (Dan.Core and all plugins) serves these routes without declaring anything itself. The services
/// are registered by <c>ConfigureDanPluginDefaults()</c> or <c>AddDanHealthChecks()</c>.
/// </para>
/// <para>
/// Routes: <c>alive</c>, <c>health/readiness</c> and <c>health/startup</c> are anonymous so platform probes can
/// call them; <c>health</c> and <c>health/deep</c> require a function key so dependency topology is not public.
/// Function names carry the <c>dan-</c> prefix to avoid clashing with app-defined function names; an app that
/// already owns one of the routes must rename its own, because attribute-declared routes cannot be disabled.
/// </para>
/// </remarks>
public class DanHealthFunctions
{
    /// <summary>Function name prefix shared by the health functions.</summary>
    public const string FunctionNamePrefix = "dan-health";

    /// <summary>Route of the liveness endpoint, relative to the Functions route prefix.</summary>
    public const string AliveRoute = "alive";

    /// <summary>Route of the readiness endpoint.</summary>
    public const string ReadinessRoute = "health/readiness";

    /// <summary>Route of the startup endpoint.</summary>
    public const string StartupRoute = "health/startup";

    /// <summary>Route of the dashboard endpoint.</summary>
    public const string HealthRoute = "health";

    /// <summary>Route of the deep endpoint.</summary>
    public const string DeepRoute = "health/deep";

    private readonly DanHealthCheckRunner _runner;

    /// <summary>
    /// Creates the functions with the runner that evaluates and renders health checks.
    /// </summary>
    public DanHealthFunctions(DanHealthCheckRunner runner)
    {
        _runner = runner;
    }

    /// <summary>Liveness probe: the process answers. Evaluates checks tagged <c>live</c>.</summary>
    [Function(FunctionNamePrefix + "-alive"), NoAuthentication]
    public Task<HttpResponseData> Alive(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = AliveRoute)] HttpRequestData req,
        FunctionContext context) => _runner.RunAsync(req, HealthEndpointKind.Alive, context.CancellationToken);

    /// <summary>Readiness probe: should this instance receive traffic. Evaluates checks tagged <c>critical</c> or <c>warmup</c>.</summary>
    [Function(FunctionNamePrefix + "-readiness"), NoAuthentication]
    public Task<HttpResponseData> Readiness(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ReadinessRoute)] HttpRequestData req,
        FunctionContext context) => _runner.RunAsync(req, HealthEndpointKind.Readiness, context.CancellationToken);

    /// <summary>Startup probe: are the dependencies reachable. Evaluates checks tagged <c>dependencies</c>.</summary>
    [Function(FunctionNamePrefix + "-startup"), NoAuthentication]
    public Task<HttpResponseData> Startup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = StartupRoute)] HttpRequestData req,
        FunctionContext context) => _runner.RunAsync(req, HealthEndpointKind.Startup, context.CancellationToken);

    /// <summary>Dashboard endpoint. Evaluates checks tagged <c>dependencies</c>.</summary>
    [Function(FunctionNamePrefix), NoAuthentication]
    public Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = HealthRoute)] HttpRequestData req,
        FunctionContext context) => _runner.RunAsync(req, HealthEndpointKind.Health, context.CancellationToken);

    /// <summary>Deep probe including outbound dependencies. Evaluates checks tagged <c>dependencies</c> or <c>external</c>.</summary>
    [Function(FunctionNamePrefix + "-deep"), NoAuthentication]
    public Task<HttpResponseData> Deep(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = DeepRoute)] HttpRequestData req,
        FunctionContext context) => _runner.RunAsync(req, HealthEndpointKind.Deep, context.CancellationToken);
}
