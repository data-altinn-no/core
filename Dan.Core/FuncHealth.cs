using Dan.Common.Health;
using Dan.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core;

/// <summary>
/// Health endpoints following the Altinn convention (https://github.com/Altinn/altinn-aspnet-healthchecks).
/// Functions has no endpoint routing, so each path is an HTTP trigger that evaluates the convention's tag filter
/// and renders the report with the library's formatters via <see cref="DanHealthCheckRunner"/>.
/// </summary>
/// <remarks>
/// All endpoints carry <see cref="NoAuthenticationAttribute"/> so the Maskinporten JWT middleware is skipped.
/// The probe endpoints (alive, readiness, startup) are anonymous because App Service health checks cannot send keys.
/// The dashboard and deep endpoints require a function key so dependency topology is not public.
/// </remarks>
public class FuncHealth : DanHealthFunctionsBase
{
    public FuncHealth(DanHealthCheckRunner runner) : base(runner)
    {
    }

    /// <summary>Liveness probe: process is up. Evaluates checks tagged <c>live</c>.</summary>
    [Function("health-alive"), NoAuthentication]
    public Task<HttpResponseData> Alive(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alive")] HttpRequestData req,
        FunctionContext context) => AliveAsync(req, context.CancellationToken);

    /// <summary>Readiness probe: should this instance receive traffic. Evaluates checks tagged <c>critical</c> or <c>warmup</c>.</summary>
    [Function("health-readiness"), NoAuthentication]
    public Task<HttpResponseData> Readiness(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/readiness")] HttpRequestData req,
        FunctionContext context) => ReadinessAsync(req, context.CancellationToken);

    /// <summary>Startup probe: are the dependencies reachable. Evaluates checks tagged <c>dependencies</c>.</summary>
    [Function("health-startup"), NoAuthentication]
    public Task<HttpResponseData> Startup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/startup")] HttpRequestData req,
        FunctionContext context) => StartupAsync(req, context.CancellationToken);

    /// <summary>Dashboard endpoint. Evaluates checks tagged <c>dependencies</c>.</summary>
    [Function("health"), NoAuthentication]
    public Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequestData req,
        FunctionContext context) => HealthAsync(req, context.CancellationToken);

    /// <summary>Deep probe including outbound dependencies. Evaluates checks tagged <c>dependencies</c> or <c>external</c>.</summary>
    [Function("health-deep"), NoAuthentication]
    public Task<HttpResponseData> Deep(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health/deep")] HttpRequestData req,
        FunctionContext context) => DeepAsync(req, context.CancellationToken);
}
