using Dan.Common.Health;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.PluginTest;

/// <summary>
/// Reference implementation of the Altinn health endpoints for a plugin. The services are registered by
/// ConfigureDanPluginDefaults(); the plugin only declares the triggers, since the isolated worker indexes
/// functions from the entry assembly only.
/// </summary>
public class Health : DanHealthFunctionsBase
{
    public Health(DanHealthCheckRunner runner) : base(runner)
    {
    }

    [Function("health-alive")]
    public Task<HttpResponseData> Alive(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alive")] HttpRequestData req,
        FunctionContext context) => AliveAsync(req, context.CancellationToken);

    [Function("health-readiness")]
    public Task<HttpResponseData> Readiness(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/readiness")] HttpRequestData req,
        FunctionContext context) => ReadinessAsync(req, context.CancellationToken);

    [Function("health-startup")]
    public Task<HttpResponseData> Startup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/startup")] HttpRequestData req,
        FunctionContext context) => StartupAsync(req, context.CancellationToken);

    [Function("health")]
    public Task<HttpResponseData> HealthDashboard(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequestData req,
        FunctionContext context) => HealthAsync(req, context.CancellationToken);

    [Function("health-deep")]
    public Task<HttpResponseData> Deep(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health/deep")] HttpRequestData req,
        FunctionContext context) => DeepAsync(req, context.CancellationToken);
}
