using Altinn.AspNet.HealthChecks;
using Dan.Common.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Dan.Common.Extensions;

/// <summary>
/// Registers the Altinn health check convention for Functions apps.
/// </summary>
public static class HealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// Adds <c>AddAltinnHealthChecks()</c> (baseline <c>live</c> self check) plus the pieces needed to serve the
    /// convention's endpoints from HTTP-trigger functions: <see cref="HttpRequestDataHealthReportWriter"/> and
    /// <see cref="DanHealthCheckRunner"/>. Returns the builder so callers can chain dependency checks and outbound probes.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="environment">Host environment, used to pick the default detail level</param>
    /// <param name="configuration">Configuration, consulted for the <c>HealthReportDetailLevel</c> override</param>
    /// <param name="configure">Optional library options (e.g. the self check name)</param>
    public static IHealthChecksBuilder AddDanHealthChecks(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration,
        Action<AltinnHealthCheckOptions>? configure = null)
    {
        var builder = services.AddAltinnHealthChecks(configure ?? (_ => { }));

        services.TryAddSingleton(_ =>
            new HttpRequestDataHealthReportWriter(HealthReportDetailLevelResolver.Resolve(environment, configuration)));
        services.TryAddSingleton<DanHealthCheckRunner>();

        return builder;
    }
}
