using System.Net;
using System.Reflection;
using Altinn.AspNet.HealthChecks;
using AwesomeAssertions;
using Dan.Common.Attributes;
using Dan.Common.Extensions;
using Dan.Common.Health;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;

namespace Dan.Common.UnitTest.Health;

[TestClass]
public class DanHealthFunctionsTest
{
    private readonly IHealthCheck _dependencyCheck = A.Fake<IHealthCheck>();
    private readonly IHealthCheck _externalCheck = A.Fake<IHealthCheck>();

    private DanHealthFunctions CreateFunctions(HealthStatus dependencyStatus = HealthStatus.Healthy)
    {
        A.CallTo(() => _dependencyCheck.CheckHealthAsync(A<HealthCheckContext>._, A<CancellationToken>._))
            .Returns(new HealthCheckResult(dependencyStatus, "dep"));
        A.CallTo(() => _externalCheck.CheckHealthAsync(A<HealthCheckContext>._, A<CancellationToken>._))
            .Returns(HealthCheckResult.Degraded("ext"));

        var env = A.Fake<IHostEnvironment>();
        A.CallTo(() => env.EnvironmentName).Returns("Production");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDanHealthChecks(env, new ConfigurationBuilder().Build())
            .AddCheck("CosmosDb", _dependencyCheck, HealthStatus.Unhealthy, [HealthCheckTags.Dependencies])
            .AddCheck("Maskinporten", _externalCheck, HealthStatus.Degraded, [HealthCheckTags.External]);

        var runner = services.BuildServiceProvider().GetRequiredService<DanHealthCheckRunner>();
        return new DanHealthFunctions(runner);
    }

    private static FunctionContext Context() => A.Fake<FunctionContext>();

    private static JObject Json(Microsoft.Azure.Functions.Worker.Http.HttpResponseData response) =>
        JObject.Parse(HealthFakes.BodyAsString(response));

    [TestMethod]
    public async Task Alive_only_evaluates_self_check()
    {
        var functions = CreateFunctions(HealthStatus.Unhealthy);

        var response = await functions.Alive(HealthFakes.Request(), Context());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ((JObject)Json(response)["entries"]!).Properties().Select(p => p.Name).Should().BeEquivalentTo("self");
        A.CallTo(() => _dependencyCheck.CheckHealthAsync(A<HealthCheckContext>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Readiness_is_healthy_when_nothing_is_critical()
    {
        var functions = CreateFunctions(HealthStatus.Unhealthy);

        var response = await functions.Readiness(HealthFakes.Request(), Context());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ((JObject)Json(response)["entries"]!).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Startup_and_health_return_503_when_a_dependency_is_unhealthy()
    {
        var functions = CreateFunctions(HealthStatus.Unhealthy);

        var startup = await functions.Startup(HealthFakes.Request(), Context());
        var health = await functions.Health(HealthFakes.Request(), Context());

        startup.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        health.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        ((JObject)Json(health)["entries"]!).Properties().Select(p => p.Name).Should().BeEquivalentTo("CosmosDb");
    }

    [TestMethod]
    public async Task Deep_includes_external_probes_and_stays_200_when_they_are_only_degraded()
    {
        var functions = CreateFunctions();

        var response = await functions.Deep(HealthFakes.Request(), Context());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = Json(response);
        json["status"]!.Value<string>().Should().Be("degraded");
        ((JObject)json["entries"]!).Properties().Select(p => p.Name).Should().BeEquivalentTo("CosmosDb", "Maskinporten");
    }

    [TestMethod]
    public void Every_endpoint_is_a_function_that_skips_dan_core_authentication()
    {
        var functions = typeof(DanHealthFunctions)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<FunctionAttribute>() is not null)
            .ToList();

        functions.Select(m => m.GetCustomAttribute<FunctionAttribute>()!.Name).Should().BeEquivalentTo(
            "dan-health-alive", "dan-health-readiness", "dan-health-startup", "dan-health", "dan-health-deep");
        functions.Should().OnlyContain(m => m.GetCustomAttribute<NoAuthenticationAttribute>() != null);
    }
}
