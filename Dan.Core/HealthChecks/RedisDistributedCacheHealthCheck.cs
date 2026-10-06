using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dan.Core.HealthChecks;

/// <summary>
/// Verifies that the configured Redis-backed <see cref="IDistributedCache"/> answers. Redis is registered via
/// <c>AddStackExchangeRedisCache</c> (no <c>IConnectionMultiplexer</c> in DI), so this exercises the actual
/// connection Dan.Core uses (Entra ID token or access key) instead of opening a separate one.
/// </summary>
public sealed class RedisDistributedCacheHealthCheck : IHealthCheck
{
    private const string ProbeKey = "dan:health:probe";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private readonly IDistributedCache _cache;

    public RedisDistributedCacheHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            // A miss is fine; we only care that the round trip succeeds.
            await _cache.GetAsync(ProbeKey, timeout.Token);
            return HealthCheckResult.Healthy("Redis reachable");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"Redis did not answer within {Timeout.TotalSeconds:0}s");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis unreachable", ex);
        }
    }
}
