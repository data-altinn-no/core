using Altinn.AspNet.HealthChecks;
using AwesomeAssertions;
using Dan.Common.Health;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dan.Common.UnitTest.Health;

[TestClass]
public class HealthReportDetailLevelResolverTest
{
    private static IHostEnvironment Env(string name)
    {
        var env = A.Fake<IHostEnvironment>();
        A.CallTo(() => env.EnvironmentName).Returns(name);
        return env;
    }

    private static IConfiguration Config(string? level = null)
    {
        var values = new Dictionary<string, string?>();
        if (level is not null)
        {
            values[HealthReportDetailLevelResolver.ConfigurationKey] = level;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [TestMethod]
    [DataRow("Production", HealthReportDetailLevel.Summary)]
    [DataRow("Development", HealthReportDetailLevel.Full)]
    [DataRow("Staging", HealthReportDetailLevel.Diagnostic)]
    public void Defaults_follow_library_environment_mapping(string environment, HealthReportDetailLevel expected)
    {
        HealthReportDetailLevelResolver.Resolve(Env(environment), Config()).Should().Be(expected);
    }

    [TestMethod]
    public void Configuration_override_wins()
    {
        HealthReportDetailLevelResolver.Resolve(Env("Production"), Config("diagnostic"))
            .Should().Be(HealthReportDetailLevel.Diagnostic);
    }
}
