using System.Diagnostics;
using AwesomeAssertions;
using Dan.Common.Telemetry;

namespace Dan.Common.UnitTest.Telemetry;

[TestClass]
public class SsnRedactingProcessorTests
{
    // Subject numbers below are synthetic Tenor test identifiers, not real national identity numbers.
    private const string SourceName = "Dan.Common.UnitTest.Telemetry";
    private static readonly ActivitySource Source = new(SourceName);

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        });
    }

    [TestMethod]
    [DataRow("https://api.skatteetaten.no/folkeregisteret/api/v1/personer/06815298657?part=person-basis", "https://api.skatteetaten.no/folkeregisteret/api/v1/personer/068152*****?part=person-basis")]
    [DataRow("https://api.skatteetaten.no/summertskattegrunnlag/v2/oppgjoer/pkg/2024/27907898224", "https://api.skatteetaten.no/summertskattegrunnlag/v2/oppgjoer/pkg/2024/279078*****")]
    [DataRow("https://example.no/api?fnr=66829700248&year=2024", "https://example.no/api?fnr=668297*****&year=2024")]
    [DataRow("/a/06815298657/b/27907898224", "/a/068152*****/b/279078*****")]
    [DataRow("https://api.skatteetaten.no/restanser/v2/ebevis/991825827", "https://api.skatteetaten.no/restanser/v2/ebevis/991825827")]
    [DataRow("https://api.skatteetaten.no/feed/?seq=123456789012", "https://api.skatteetaten.no/feed/?seq=123456789012")]
    [DataRow("", "")]
    [DataRow(null, null)]
    public void Mask_KeepsFirstSixDigitsAndLeavesOtherNumbersAlone(string? input, string? expected)
    {
        SsnRedactingProcessor.Mask(input).Should().Be(expected);
    }

    [TestMethod]
    public void OnEnd_MasksUrlTagsAndDisplayName()
    {
        using var activity = Source.StartActivity("GET /personer/06815298657");
        activity.Should().NotBeNull();
        activity!.SetTag("url.full", "https://api.skatteetaten.no/folkeregisteret/api/v1/personer/06815298657?part=person-basis");
        activity.SetTag("url.path", "/folkeregisteret/api/v1/personer/06815298657");
        activity.SetTag("server.address", "api.skatteetaten.no");

        new SsnRedactingProcessor().OnEnd(activity);

        activity.GetTagItem("url.full").Should().Be("https://api.skatteetaten.no/folkeregisteret/api/v1/personer/068152*****?part=person-basis");
        activity.GetTagItem("url.path").Should().Be("/folkeregisteret/api/v1/personer/068152*****");
        activity.GetTagItem("server.address").Should().Be("api.skatteetaten.no");
        activity.DisplayName.Should().Be("GET /personer/068152*****");
    }

    [TestMethod]
    public void OnEnd_LeavesSpanWithoutIdentityNumbersUnchanged()
    {
        using var activity = Source.StartActivity("GET /restanser/v2/ebevis/991825827");
        activity.Should().NotBeNull();
        activity!.SetTag("url.full", "https://api.skatteetaten.no/restanser/v2/ebevis/991825827");

        new SsnRedactingProcessor().OnEnd(activity);

        activity.GetTagItem("url.full").Should().Be("https://api.skatteetaten.no/restanser/v2/ebevis/991825827");
        activity.DisplayName.Should().Be("GET /restanser/v2/ebevis/991825827");
    }
}
