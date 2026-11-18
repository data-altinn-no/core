using AwesomeAssertions;
using Dan.Common.Telemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Dan.Common.UnitTest.Telemetry;

[TestClass]
public class SsnRedactingLogProcessorTests
{
    // Subject numbers below are synthetic Tenor test identifiers, not real national identity numbers.

    [TestMethod]
    public void OnEnd_MasksFormattedMessageAndStringAttributes()
    {
        var exported = new List<(string? Message, string? Body, Dictionary<string, object?> Attributes)>();
        using (var loggerFactory = CreateLoggerFactory(exported))
        {
            var logger = loggerFactory.CreateLogger("Test");
            logger.LogError("Call to {Url} failed for {Subject} with {StatusCode}",
                "https://api.example.no/personer/06815298657", "66829700248", 500);
        }

        exported.Should().ContainSingle();
        var (message, body, attributes) = exported[0];
        message.Should().Be("Call to https://api.example.no/personer/068152***** failed for 668297***** with 500");
        body.Should().Be("Call to {Url} failed for {Subject} with {StatusCode}");
        attributes["Url"].Should().Be("https://api.example.no/personer/068152*****");
        attributes["Subject"].Should().Be("668297*****");
        attributes["StatusCode"].Should().Be(500);
    }

    [TestMethod]
    public void OnEnd_LeavesRecordsWithoutIdentityNumbersUnchanged()
    {
        var exported = new List<(string? Message, string? Body, Dictionary<string, object?> Attributes)>();
        using (var loggerFactory = CreateLoggerFactory(exported))
        {
            var logger = loggerFactory.CreateLogger("Test");
            logger.LogInformation("Harvesting {Dataset} for {OrgNumber}", "Grunnbok", "991825827");
        }

        exported.Should().ContainSingle();
        exported[0].Message.Should().Be("Harvesting Grunnbok for 991825827");
        exported[0].Attributes["OrgNumber"].Should().Be("991825827");
    }

    private static ILoggerFactory CreateLoggerFactory(
        List<(string? Message, string? Body, Dictionary<string, object?> Attributes)> exported)
    {
        return LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(new SsnRedactingLogProcessor());
            // Stands in for the export processor: records what would have been exported.
            options.AddProcessor(new CapturingProcessor(exported));
        }));
    }

    private sealed class CapturingProcessor(
        List<(string? Message, string? Body, Dictionary<string, object?> Attributes)> exported) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data)
        {
            var attributes = data.Attributes?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, object?>();
            exported.Add((data.FormattedMessage, data.Body, attributes));
        }
    }
}
