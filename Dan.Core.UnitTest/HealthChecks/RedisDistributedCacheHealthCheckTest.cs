using AwesomeAssertions;
using Dan.Core.HealthChecks;
using FakeItEasy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dan.Core.UnitTest.HealthChecks;

[TestClass]
public class RedisDistributedCacheHealthCheckTest
{
    private static HealthCheckContext Context(HealthStatus failureStatus = HealthStatus.Unhealthy) => new()
    {
        Registration = new HealthCheckRegistration("Redis", A.Fake<IHealthCheck>(), failureStatus, null)
    };

    [TestMethod]
    public async Task Round_trip_success_is_healthy()
    {
        var cache = A.Fake<IDistributedCache>();
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).Returns((byte[])null);

        var result = await new RedisDistributedCacheHealthCheck(cache).CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [TestMethod]
    public async Task Exception_maps_to_registered_failure_status_with_exception_attached()
    {
        var cache = A.Fake<IDistributedCache>();
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).Throws(new TimeoutException("no redis"));

        var result = await new RedisDistributedCacheHealthCheck(cache).CheckHealthAsync(Context(HealthStatus.Degraded));

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Redis unreachable");
        result.Exception.Should().BeOfType<TimeoutException>();
    }
}
