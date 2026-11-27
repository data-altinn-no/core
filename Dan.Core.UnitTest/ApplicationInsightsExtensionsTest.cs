using System.Diagnostics.CodeAnalysis;
using Dan.Core.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dan.Core.UnitTest;

[TestClass]
[ExcludeFromCodeCoverage]
public class ApplicationInsightsExtensionsTest
{
    public TestContext TestContext { get; set; } = null!;

    // Log lines and exceptions are what operations are followed up by, such as the summary of the pending consent
    // sync, so they must arrive no matter how much other telemetry is written at the same time
    [TestMethod]
    [DataRow("Trace", 0)]
    [DataRow("Exception", 0)]
    [DataRow("Dependency", 1)]
    [DataRow("Request", 1)]
    [DataRow("Event", 1)]
    public async Task AddCoreApplicationInsights_SamplesEverythingButLogLinesAndExceptions(string telemetryType, int expectedSamplers)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCoreApplicationInsights();

        await using var provider = services.BuildServiceProvider();
        var processors = provider.GetRequiredService<TelemetryConfiguration>().DefaultTelemetrySink.TelemetryProcessors;
        var chain = string.Join(", ", processors.Select(Describe));
        TestContext.WriteLine($"Processors: {chain}");

        var samplers = processors.OfType<AdaptiveSamplingTelemetryProcessor>().Count(sampler => Samples(sampler, telemetryType));

        Assert.AreEqual(expectedSamplers, samplers, $"Samplers for {telemetryType} among: {chain}");
    }

    /// <summary>
    /// A sampler handles the included types, or every type if none are included, except the excluded ones
    /// </summary>
    private static bool Samples(AdaptiveSamplingTelemetryProcessor sampler, string telemetryType)
    {
        var included = Split(sampler.IncludedTypes);
        return (included.Length == 0 || included.Contains(telemetryType)) && !Split(sampler.ExcludedTypes).Contains(telemetryType);
    }

    private static string[] Split(string? types)
    {
        return (types ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Describe(ITelemetryProcessor processor)
    {
        return processor is AdaptiveSamplingTelemetryProcessor sampler
            ? $"AdaptiveSampling(included='{sampler.IncludedTypes}' excluded='{sampler.ExcludedTypes}')"
            : processor.GetType().Name;
    }
}
