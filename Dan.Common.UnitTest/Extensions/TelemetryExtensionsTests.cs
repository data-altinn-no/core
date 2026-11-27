using AwesomeAssertions;
using Azure.Monitor.OpenTelemetry.Exporter;
using Dan.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Dan.Common.UnitTest.Extensions;

[TestClass]
public class TelemetryExtensionsTests
{
    private const string ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

    [TestMethod]
    public void AddDanOpenTelemetry_ExportsEverythingUnsampled_WhenAConnectionStringIsConfigured()
    {
        using var provider = BuildProvider(("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString));

        var options = provider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;

        options.ConnectionString.Should().Be(ConnectionString);
        // A log line must arrive whether or not the trace it belongs to is sampled, and no trace is sampled out
        options.EnableTraceBasedLogsSampler.Should().BeFalse();
        options.SamplingRatio.Should().Be(1.0f);
        options.TracesPerSecond.Should().BeNull();
    }

    [TestMethod]
    public void AddDanOpenTelemetry_LimitsTracesButNotLogLines_WhenARateIsGiven()
    {
        using var provider = BuildProvider(maxTracesPerSecond: 5, ("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString));

        var options = provider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;

        options.TracesPerSecond.Should().Be(5);
        // Log lines, and with them warnings, errors and logged exceptions, do not follow the sampling of traces
        options.EnableTraceBasedLogsSampler.Should().BeFalse();
    }

    [TestMethod]
    public void AddDanOpenTelemetry_UsesTheInstrumentationKey_WhenThereIsNoConnectionString()
    {
        using var provider = BuildProvider(("APPINSIGHTS_INSTRUMENTATIONKEY", "11111111-1111-1111-1111-111111111111"));

        var options = provider.GetRequiredService<IOptions<AzureMonitorExporterOptions>>().Value;

        options.ConnectionString.Should().Be("InstrumentationKey=11111111-1111-1111-1111-111111111111");
    }

    [TestMethod]
    public void AddDanOpenTelemetry_AddsNoExporter_WhenNeitherIsConfigured()
    {
        // The exporter throws at startup without a connection string, which would stop a local run
        using var provider = BuildProvider();

        provider.GetService<TracerProvider>().Should().BeNull();
    }

    [TestMethod]
    public void AddDanOpenTelemetryLogging_MasksNationalIdentityNumbers()
    {
        // The subject number is a synthetic Tenor test identifier, not a real national identity number
        var exported = Log(logger => logger.LogInformation("Lookup for {Subject}", "06815298657"), "Test");

        exported.Should().ContainSingle();
        exported[0].Message.Should().Be("Lookup for 068152*****");
        exported[0].Attributes["Subject"].Should().Be("068152*****");
    }

    [TestMethod]
    public void AddDanOpenTelemetryLogging_LeavesOutRequestLinesFromHttpClients()
    {
        // These lines hold the full url, which for many data sources includes the subject's national identity number
        var exported = Log(logger =>
        {
            logger.LogInformation("Start processing HTTP request GET https://api.example.no/personer/06815298657");
            logger.LogWarning("Polly retry");
        }, "System.Net.Http.HttpClient.SafeHttpClient.LogicalHandler");

        exported.Should().ContainSingle().Which.Message.Should().Be("Polly retry");
    }

    private static ServiceProvider BuildProvider(params (string Key, string Value)[] settings)
    {
        return BuildProvider(null, settings);
    }

    private static ServiceProvider BuildProvider(double? maxTracesPerSecond, params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(setting => setting.Key, setting => (string?)setting.Value))
            .Build();

        var services = new ServiceCollection();
        services.AddDanOpenTelemetry(configuration, maxTracesPerSecond);
        return services.BuildServiceProvider();
    }

    private static List<(string? Message, Dictionary<string, object?> Attributes)> Log(Action<ILogger> write, string category)
    {
        var exported = new List<(string? Message, Dictionary<string, object?> Attributes)>();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddDanOpenTelemetryLogging());
        // Stands in for the export processor: records what would have been exported. Added after the setup under
        // test, so it sees a record the way an exporter would.
        services.Configure<OpenTelemetryLoggerOptions>(options => options.AddProcessor(new CapturingProcessor(exported)));

        using (var provider = services.BuildServiceProvider())
        {
            write(provider.GetRequiredService<ILoggerFactory>().CreateLogger(category));
        }

        return exported;
    }

    private sealed class CapturingProcessor(List<(string? Message, Dictionary<string, object?> Attributes)> exported) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data)
        {
            exported.Add((data.FormattedMessage, data.Attributes?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, object?>()));
        }
    }
}
