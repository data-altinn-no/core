using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Dan.Common.Enums;
using Dan.Common.Extensions;
using Dan.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Dan.Core.UnitTest;

/// <summary>
/// The usage statistics are read from the DanLog lines in Application Insights, by the names of their properties.
/// These tests log through the telemetry setup Core runs with.
/// </summary>
[TestClass]
[ExcludeFromCodeCoverage]
public class DanLogTelemetryTest
{
    [TestMethod]
    public void DanLog_ExportsThePropertiesTheUsageStatisticsRead()
    {
        var exported = Log(logger => logger.DanLog(LogAction.AccreditationsRetrieved, "991825827", "974760673", "eBevis"));

        var attributes = exported.Should().ContainSingle().Subject;
        attributes["action"].Should().Be("AccreditationsRetrieved");
        attributes["requestor"].Should().Be("974760673");
        attributes["serviceContext"].Should().Be("eBevis");
    }

    [TestMethod]
    public void DanLog_ExportsTheDatasetName_AndMasksASubjectThatIsAPerson()
    {
        // The subject number is a synthetic Tenor test identifier, not a real national identity number
        var exported = Log(logger => logger.DanLog("06815298657", "UnitBasicInformation", "eBevis", LogAction.DatasetRetrieved));

        var attributes = exported.Should().ContainSingle().Subject;
        attributes["action"].Should().Be("DatasetRetrieved");
        attributes["evidenceCodeName"].Should().Be("UnitBasicInformation");
        attributes["subject"].Should().Be("068152*****");
    }

    private static List<Dictionary<string, object?>> Log(Action<ILogger> write)
    {
        var exported = new List<Dictionary<string, object?>>();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddDanOpenTelemetryLogging());
        // Stands in for the export processor: records the properties that would have been exported
        services.Configure<OpenTelemetryLoggerOptions>(options => options.AddProcessor(new CapturingProcessor(exported)));

        using (var provider = services.BuildServiceProvider())
        {
            write(provider.GetRequiredService<ILoggerFactory>().CreateLogger("Dan.Core.Test"));
        }

        return exported;
    }

    private sealed class CapturingProcessor(List<Dictionary<string, object?>> exported) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data)
        {
            exported.Add(data.Attributes?.ToDictionary(attribute => attribute.Key, attribute => attribute.Value) ?? new Dictionary<string, object?>());
        }
    }
}
