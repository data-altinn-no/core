using System.Net;
using Altinn.AspNet.HealthChecks;
using AwesomeAssertions;
using Dan.Common.Extensions;
using Dan.Common.Health;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;

namespace Dan.Core.UnitTest;

[TestClass]
public class FuncHealthTest
{
    private readonly IHealthCheck _dependencyCheck = A.Fake<IHealthCheck>();
    private readonly IHealthCheck _externalCheck = A.Fake<IHealthCheck>();

    private FuncHealth CreateFunction(HealthStatus dependencyStatus = HealthStatus.Healthy)
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
            .AddCheck("CosmosDb", _dependencyCheck, HealthStatus.Unhealthy, new[] { HealthCheckTags.Dependencies })
            .AddCheck("Maskinporten", _externalCheck, HealthStatus.Degraded, new[] { HealthCheckTags.External });

        var runner = services.BuildServiceProvider().GetRequiredService<DanHealthCheckRunner>();
        return new FuncHealth(runner);
    }

    [TestMethod]
    public async Task Alive_only_evaluates_self_check()
    {
        var func = CreateFunction(HealthStatus.Unhealthy);

        var response = await func.Alive(CreateRequest(), CreateContext());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = (JObject)JObject.Parse(Body(response))["entries"];
        entries.Properties().Select(p => p.Name).Should().BeEquivalentTo("self");
        A.CallTo(() => _dependencyCheck.CheckHealthAsync(A<HealthCheckContext>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Readiness_is_healthy_when_nothing_is_critical()
    {
        var func = CreateFunction(HealthStatus.Unhealthy);

        var response = await func.Readiness(CreateRequest(), CreateContext());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ((JObject)JObject.Parse(Body(response))["entries"]).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Startup_and_health_return_503_when_a_dependency_is_unhealthy()
    {
        var func = CreateFunction(HealthStatus.Unhealthy);

        var startup = await func.Startup(CreateRequest(), CreateContext());
        var health = await func.Health(CreateRequest(), CreateContext());

        startup.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        health.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var entries = (JObject)JObject.Parse(Body(health))["entries"];
        entries.Properties().Select(p => p.Name).Should().BeEquivalentTo("CosmosDb");
    }

    [TestMethod]
    public async Task Deep_includes_external_probes_and_stays_200_when_they_are_only_degraded()
    {
        var func = CreateFunction();

        var response = await func.Deep(CreateRequest(), CreateContext());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JObject.Parse(Body(response));
        json["status"].Value<string>().Should().Be("degraded");
        ((JObject)json["entries"]).Properties().Select(p => p.Name).Should().BeEquivalentTo("CosmosDb", "Maskinporten");
    }

    private static FunctionContext CreateContext() => A.Fake<FunctionContext>();

    private static HttpRequestData CreateRequest()
    {
        var functionContext = A.Fake<FunctionContext>();
        var request = A.Fake<HttpRequestData>(o => o.WithArgumentsForConstructor(new object[] { functionContext }));
        A.CallTo(() => request.Url).Returns(new Uri("https://localhost/api/health"));
        A.CallTo(() => request.Headers).Returns(new HttpHeadersCollection());

        var response = A.Fake<HttpResponseData>(o => o.WithArgumentsForConstructor(new object[] { functionContext }));
        response.Headers = new HttpHeadersCollection();
        response.Body = new MemoryStream();
        A.CallTo(() => request.CreateResponse()).Returns(response);
        return request;
    }

    private static string Body(HttpResponseData response)
    {
        response.Body.Position = 0;
        return new StreamReader(response.Body).ReadToEnd();
    }
}
