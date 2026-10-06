using System.Net;
using Altinn.AspNet.HealthChecks;
using AwesomeAssertions;
using Dan.Common.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;

namespace Dan.Common.UnitTest.Health;

[TestClass]
public class HttpRequestDataHealthReportWriterTest
{
    private static readonly HealthReport Mixed = HealthFakes.Report(
        ("self", HealthStatus.Healthy, "up", null, [HealthCheckTags.Live]),
        ("redis", HealthStatus.Degraded, "slow", null, [HealthCheckTags.Dependencies]));

    private static readonly HealthReport Failing = HealthFakes.Report(
        ("cosmos", HealthStatus.Unhealthy, "boom", new InvalidOperationException("AccountKey=secret"), [HealthCheckTags.Dependencies, HealthCheckTags.Critical]));

    [TestMethod]
    public async Task Healthy_or_degraded_report_gives_200_and_altinn_json_media_type()
    {
        var writer = new HttpRequestDataHealthReportWriter(HealthReportDetailLevel.Summary);
        var request = HealthFakes.Request();

        var response = await writer.WriteAsync(request, Mixed);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Type").Single().Should().StartWith("application/vnd.altinn.health.v1+json");
        response.Headers.GetValues("Cache-Control").Single().Should().Contain("no-store");

        var json = JObject.Parse(HealthFakes.BodyAsString(response));
        json["status"]!.Value<string>().Should().Be("degraded");
        json["entries"]!["self"]!["status"]!.Value<string>().Should().Be("healthy");
        json["entries"]!["redis"]!["tags"]!.Values<string>().Should().Contain(HealthCheckTags.Dependencies);
    }

    [TestMethod]
    public async Task Unhealthy_report_gives_503()
    {
        var writer = new HttpRequestDataHealthReportWriter(HealthReportDetailLevel.Summary);

        var response = await writer.WriteAsync(HealthFakes.Request(), Failing);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        JObject.Parse(HealthFakes.BodyAsString(response))["status"]!.Value<string>().Should().Be("unhealthy");
    }

    [TestMethod]
    public async Task Accept_text_plain_gives_single_word_body()
    {
        var writer = new HttpRequestDataHealthReportWriter(HealthReportDetailLevel.Summary);

        var response = await writer.WriteAsync(HealthFakes.Request(accept: "text/plain"), Failing);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.GetValues("Content-Type").Single().Should().StartWith("text/plain");
        HealthFakes.BodyAsString(response).Trim().Should().Be("unhealthy");
    }

    [TestMethod]
    public async Task Summary_level_hides_exception_description_and_data()
    {
        var writer = new HttpRequestDataHealthReportWriter(HealthReportDetailLevel.Summary);

        var response = await writer.WriteAsync(HealthFakes.Request(), Failing);
        var body = HealthFakes.BodyAsString(response);

        body.Should().NotContain("AccountKey=secret");
        var entry = (JObject)JObject.Parse(body)["entries"]!["cosmos"]!;
        entry.Should().NotContainKey("description");
        entry.Should().NotContainKey("data");
        entry.Should().NotContainKey("exception");
        entry.Should().ContainKey("tags");
    }

    [TestMethod]
    public async Task Full_level_includes_exception_description_and_data()
    {
        var writer = new HttpRequestDataHealthReportWriter(HealthReportDetailLevel.Full);

        var response = await writer.WriteAsync(HealthFakes.Request(), Failing);
        var body = HealthFakes.BodyAsString(response);

        var entry = (JObject)JObject.Parse(body)["entries"]!["cosmos"]!;
        entry["description"]!.Value<string>().Should().Be("boom");
        entry["data"]!["pool"]!.Value<string>().Should().Be("primary");
        body.Should().Contain("AccountKey=secret");
    }
}
