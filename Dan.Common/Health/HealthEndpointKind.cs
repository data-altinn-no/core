using Altinn.AspNet.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dan.Common.Health;

/// <summary>
/// The five endpoints in the Altinn health check convention
/// (see https://github.com/Altinn/altinn-aspnet-healthchecks).
/// </summary>
public enum HealthEndpointKind
{
    /// <summary>Liveness probe (<c>/alive</c>). Evaluates checks tagged <c>live</c>.</summary>
    Alive,

    /// <summary>Readiness probe (<c>/health/readiness</c>). Evaluates checks tagged <c>critical</c> or <c>warmup</c>.</summary>
    Readiness,

    /// <summary>Startup probe (<c>/health/startup</c>). Evaluates checks tagged <c>dependencies</c>.</summary>
    Startup,

    /// <summary>Dashboard / human endpoint (<c>/health</c>). Evaluates checks tagged <c>dependencies</c>.</summary>
    Health,

    /// <summary>Deep probe (<c>/health/deep</c>). Evaluates checks tagged <c>dependencies</c> or <c>external</c>.</summary>
    Deep
}

/// <summary>
/// Tag predicates for each endpoint kind. These mirror the predicates used by
/// <c>MapAltinnHealthChecks()</c> in Altinn.AspNet.HealthChecks, which are internal to that library.
/// Azure Functions isolated workers have no endpoint routing, so we evaluate the predicates ourselves.
/// </summary>
public static class HealthEndpointPredicates
{
    /// <summary>
    /// Returns the registration filter used by the given endpoint kind.
    /// </summary>
    public static Func<HealthCheckRegistration, bool> For(HealthEndpointKind kind) => kind switch
    {
        HealthEndpointKind.Alive => static r => r.Tags.Contains(HealthCheckTags.Live),
        HealthEndpointKind.Readiness => static r => r.Tags.Contains(HealthCheckTags.Critical) || r.Tags.Contains(HealthCheckTags.Warmup),
        HealthEndpointKind.Startup => static r => r.Tags.Contains(HealthCheckTags.Dependencies),
        HealthEndpointKind.Health => static r => r.Tags.Contains(HealthCheckTags.Dependencies),
        HealthEndpointKind.Deep => static r => r.Tags.Contains(HealthCheckTags.Dependencies) || r.Tags.Contains(HealthCheckTags.External),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
