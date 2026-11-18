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

    [TestMethod]
    public void OnEnd_MasksElevenDigitNumericAttributes()
    {
        var exported = new List<(string? Message, string? Body, Dictionary<string, object?> Attributes)>();
        using (var loggerFactory = CreateLoggerFactory(exported))
        {
            loggerFactory.CreateLogger("Test").LogInformation("Subject {Subject} got {StatusCode}", 66829700248L, 200);
        }

        exported.Should().ContainSingle();
        exported[0].Message.Should().Be("Subject 668297***** got 200");
        exported[0].Attributes["Subject"].Should().Be("668297*****");
        exported[0].Attributes["StatusCode"].Should().Be(200);
    }

    [TestMethod]
    public void OnEnd_ReplacesExceptionContainingIdentityNumber()
    {
        var exported = new List<(string? Message, string? Body, Dictionary<string, object?> Attributes)>();
        Exception? exportedException = null;
        using (var loggerFactory = CreateLoggerFactory(exported, e => exportedException = e))
        {
            var inner = new InvalidOperationException("Upstream said: unknown person 27907898224");
            var exception = new HttpRequestException("GET https://api.example.no/personer/06815298657 failed", inner);
            loggerFactory.CreateLogger("Test").LogError(exception, "Lookup failed");
        }

        var redacted = exportedException.Should().BeOfType<RedactedException>().Subject;
        redacted.OriginalType.Should().Be("System.Net.Http.HttpRequestException");
        redacted.Message.Should().Be("System.Net.Http.HttpRequestException: GET https://api.example.no/personer/068152***** failed");
        redacted.ToString().Should().Contain("068152*****").And.Contain("279078*****")
            .And.NotContain("06815298657").And.NotContain("27907898224");
        redacted.InnerException.Should().BeOfType<RedactedException>()
            .Which.Message.Should().Be("System.InvalidOperationException: Upstream said: unknown person 279078*****");

        exported.Should().ContainSingle();
        exported[0].Attributes[SsnRedactingLogProcessor.OriginalExceptionAttribute].Should().BeOfType<string>()
            .Which.Should().StartWith("System.Net.Http.HttpRequestException: GET https://api.example.no/personer/068152***** failed")
            .And.Contain("System.InvalidOperationException: Upstream said: unknown person 279078*****")
            .And.NotContain("06815298657").And.NotContain("27907898224");
    }

    [TestMethod]
    public void OnEnd_ReplacesOuterExceptionWhenOnlyInnerContainsIdentityNumber()
    {
        Exception? exportedException = null;
        var cleanInner = new TimeoutException("Timed out");
        var dirtyInner = new InvalidOperationException("Unknown person 27907898224", cleanInner);
        var exception = new HttpRequestException("Lookup failed", dirtyInner);
        using (var loggerFactory = CreateLoggerFactory([], e => exportedException = e))
        {
            loggerFactory.CreateLogger("Test").LogError(exception, "Lookup failed");
        }

        // The original's inner exception cannot be swapped, so the outer exception has to be replaced as well.
        var redacted = exportedException.Should().BeOfType<RedactedException>().Subject;
        redacted.Message.Should().Be("System.Net.Http.HttpRequestException: Lookup failed");
        var redactedInner = redacted.InnerException.Should().BeOfType<RedactedException>().Subject;
        redactedInner.Message.Should().Be("System.InvalidOperationException: Unknown person 279078*****");
        // Inner exceptions without an identity number are kept as they are.
        redactedInner.InnerException.Should().BeSameAs(cleanInner);
    }

    [TestMethod]
    public void OnEnd_ReplacesAggregateExceptionWhenAnyInnerContainsIdentityNumber()
    {
        Exception? exportedException = null;
        var exception = new AggregateException("Several lookups failed",
            new TimeoutException("Timed out"), new InvalidOperationException("Unknown person 27907898224"));
        using (var loggerFactory = CreateLoggerFactory([], e => exportedException = e))
        {
            loggerFactory.CreateLogger("Test").LogError(exception, "Lookup failed");
        }

        exportedException.Should().BeOfType<RedactedException>()
            .Which.ToString().Should().Contain("279078*****").And.NotContain("27907898224");
    }

    [TestMethod]
    public void OnEnd_KeepsExceptionWithoutIdentityNumber()
    {
        var exported = new List<(string? Message, string? Body, Dictionary<string, object?> Attributes)>();
        Exception? exportedException = null;
        var exception = new HttpRequestException("GET https://api.example.no/organisasjoner/991825827 failed",
            new InvalidOperationException("Upstream returned 500"));
        using (var loggerFactory = CreateLoggerFactory(exported, e => exportedException = e))
        {
            loggerFactory.CreateLogger("Test").LogError(exception, "Lookup failed");
        }

        exportedException.Should().BeSameAs(exception);
        exported.Should().ContainSingle();
        exported[0].Attributes.Should().NotContainKey(SsnRedactingLogProcessor.OriginalExceptionAttribute);
    }

    [TestMethod]
    public void OnEnd_AddsMaskedAttributeForScopeValueWithIdentityNumber()
    {
        var exported = new List<(string? Message, string? Body, Dictionary<string, object?> Attributes)>();
        using (var loggerFactory = CreateLoggerFactory(exported))
        {
            var logger = loggerFactory.CreateLogger("Test");
            using (logger.BeginScope(new Dictionary<string, object?>
                   {
                       ["Uri"] = new Uri("https://api.example.no/personer/06815298657"),
                       ["Dataset"] = "Freg"
                   }))
            {
                logger.LogInformation("Done after {ElapsedMs} ms", 12);
            }
        }

        exported.Should().ContainSingle();
        exported[0].Attributes["Uri"].Should().Be("https://api.example.no/personer/068152*****");
        exported[0].Attributes.Should().NotContainKey("Dataset");
        exported[0].Attributes["ElapsedMs"].Should().Be(12);
    }

    private static ILoggerFactory CreateLoggerFactory(
        List<(string? Message, string? Body, Dictionary<string, object?> Attributes)> exported,
        Action<Exception?>? onException = null)
    {
        return LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddProcessor(new SsnRedactingLogProcessor());
            // Stands in for the export processor: records what would have been exported.
            options.AddProcessor(new CapturingProcessor(exported, onException));
        }));
    }

    private sealed class CapturingProcessor(
        List<(string? Message, string? Body, Dictionary<string, object?> Attributes)> exported,
        Action<Exception?>? onException) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data)
        {
            onException?.Invoke(data.Exception);
            var attributes = data.Attributes?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, object?>();
            exported.Add((data.FormattedMessage, data.Body, attributes));
        }
    }
}
