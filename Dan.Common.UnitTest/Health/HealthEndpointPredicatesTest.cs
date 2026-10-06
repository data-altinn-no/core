using Altinn.AspNet.HealthChecks;
using AwesomeAssertions;
using Dan.Common.Health;
using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dan.Common.UnitTest.Health;

[TestClass]
public class HealthEndpointPredicatesTest
{
    private static HealthCheckRegistration Reg(params string[] tags) =>
        new("x", A.Fake<IHealthCheck>(), null, tags);

    [TestMethod]
    [DataRow(HealthEndpointKind.Alive, "live", true)]
    [DataRow(HealthEndpointKind.Alive, "dependencies", false)]
    [DataRow(HealthEndpointKind.Readiness, "critical", true)]
    [DataRow(HealthEndpointKind.Readiness, "warmup", true)]
    [DataRow(HealthEndpointKind.Readiness, "dependencies", false)]
    [DataRow(HealthEndpointKind.Readiness, "live", false)]
    [DataRow(HealthEndpointKind.Startup, "dependencies", true)]
    [DataRow(HealthEndpointKind.Startup, "external", false)]
    [DataRow(HealthEndpointKind.Health, "dependencies", true)]
    [DataRow(HealthEndpointKind.Health, "external", false)]
    [DataRow(HealthEndpointKind.Deep, "dependencies", true)]
    [DataRow(HealthEndpointKind.Deep, "external", true)]
    [DataRow(HealthEndpointKind.Deep, "live", false)]
    public void Predicate_matches_library_convention(HealthEndpointKind kind, string tag, bool expected)
    {
        HealthEndpointPredicates.For(kind)(Reg(tag)).Should().Be(expected);
    }

    [TestMethod]
    public void Tag_constants_are_the_lowercase_words_used_in_the_matrix()
    {
        HealthCheckTags.Live.Should().Be("live");
        HealthCheckTags.Dependencies.Should().Be("dependencies");
        HealthCheckTags.Critical.Should().Be("critical");
        HealthCheckTags.Warmup.Should().Be("warmup");
        HealthCheckTags.External.Should().Be("external");
    }
}
