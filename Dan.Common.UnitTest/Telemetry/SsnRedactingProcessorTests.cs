using System.Diagnostics;
using AwesomeAssertions;
using Dan.Common.Telemetry;

namespace Dan.Common.UnitTest.Telemetry;

[TestClass]
public class SsnRedactingProcessorTests
{
    // Subject numbers below are synthetic Tenor test identifiers, not real national identity numbers.
    private const string SourceName = "Dan.Common.UnitTest.Telemetry";
    private static ActivitySource _source = null!;
    private static ActivityListener _listener = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _source = new ActivitySource(SourceName);
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // The listener is process-global; dispose it so it does not leak into other test classes.
        _listener.Dispose();
        _source.Dispose();
    }

    [TestMethod]
    [DataRow("https://api.skatteetaten.no/folkeregisteret/api/v1/personer/06815298657?part=person-basis", "https://api.skatteetaten.no/folkeregisteret/api/v1/personer/068152*****?part=person-basis")]
    [DataRow("https://api.skatteetaten.no/summertskattegrunnlag/v2/oppgjoer/pkg/2024/27907898224", "https://api.skatteetaten.no/summertskattegrunnlag/v2/oppgjoer/pkg/2024/279078*****")]
    [DataRow("https://example.no/api?fnr=66829700248&year=2024", "https://example.no/api?fnr=668297*****&year=2024")]
    [DataRow("/a/06815298657/b/27907898224", "/a/068152*****/b/279078*****")]
    [DataRow("https://api.skatteetaten.no/restanser/v2/ebevis/991825827", "https://api.skatteetaten.no/restanser/v2/ebevis/991825827")]
    [DataRow("https://api.skatteetaten.no/feed/?seq=123456789012", "https://api.skatteetaten.no/feed/?seq=123456789012")]
    [DataRow("/odata/%2706815298657%27/x", "/odata/%27068152*****%27/x")]
    [DataRow("/personer/%2006815298657", "/personer/%20068152*****")]
    [DataRow("/personer/%20123456789012", "/personer/%20123456789012")]
    [DataRow("", "")]
    [DataRow(null, null)]
    public void Mask_KeepsFirstSixDigitsAndLeavesOtherNumbersAlone(string? input, string? expected)
    {
        SsnRedactingProcessor.Mask(input).Should().Be(expected);
    }

    [TestMethod]
    public void OnEnd_MasksUrlTagsAndDisplayName()
    {
        using var activity = _source.StartActivity("GET /personer/06815298657");
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
    public void OnEnd_MasksEveryStringTagAndLeavesOtherTypesAlone()
    {
        using var activity = _source.StartActivity("query");
        activity.Should().NotBeNull();
        activity!.SetTag("http.route", "/personer/06815298657");
        activity.SetTag("db.query.text", "SELECT * FROM p WHERE fnr = '66829700248'");
        activity.SetTag("plugin.custom", "subject 27907898224");
        activity.SetTag("http.response.status_code", 200);
        activity.SetTag("plugin.orgnr", "991825827");

        new SsnRedactingProcessor().OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be("/personer/068152*****");
        activity.GetTagItem("db.query.text").Should().Be("SELECT * FROM p WHERE fnr = '668297*****'");
        activity.GetTagItem("plugin.custom").Should().Be("subject 279078*****");
        activity.GetTagItem("http.response.status_code").Should().Be(200);
        activity.GetTagItem("plugin.orgnr").Should().Be("991825827");
    }

    [TestMethod]
    public void OnEnd_LeavesSpanWithoutIdentityNumbersUnchanged()
    {
        using var activity = _source.StartActivity("GET /restanser/v2/ebevis/991825827");
        activity.Should().NotBeNull();
        activity!.SetTag("url.full", "https://api.skatteetaten.no/restanser/v2/ebevis/991825827");

        new SsnRedactingProcessor().OnEnd(activity);

        activity.GetTagItem("url.full").Should().Be("https://api.skatteetaten.no/restanser/v2/ebevis/991825827");
        activity.DisplayName.Should().Be("GET /restanser/v2/ebevis/991825827");
    }
}
