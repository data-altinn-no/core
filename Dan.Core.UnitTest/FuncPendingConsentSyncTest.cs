using System.Diagnostics.CodeAnalysis;
using System.Net;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Dan.Core.UnitTest;

[TestClass]
[ExcludeFromCodeCoverage]
public class FuncPendingConsentSyncTest
{
    // FunctionKeyValue in appsettings.unittest.json
    private const string FunctionKey = "x";
    private const string Url = "https://localhost/api/debug/pendingconsentsync";

    private readonly IPendingConsentSyncService _syncService = A.Fake<IPendingConsentSyncService>();

    [TestMethod]
    public async Task Trigger_RunsTheSyncAndReturnsTheCounts_WhenCodeIsTheFunctionKey()
    {
        var counts = new PendingConsentSyncResult(Checked: 4, Granted: 2, Denied: 1, Failed: 1);
        A.CallTo(() => _syncService.Sync()).Returns(Task.FromResult(counts));

        var response = await CreateFunction().Trigger(TestHelpers.CreateHttpRequestData($"{Url}?code={FunctionKey}"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(counts, JsonConvert.DeserializeObject<PendingConsentSyncResult>(TestHelpers.ReadBody(response)));
        A.CallTo(() => _syncService.Sync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("?code=")]
    [DataRow("?code=wrong")]
    [DataRow("?code=X")]
    [DataRow("?forceRefresh=x")]
    public async Task Trigger_DoesNotRunTheSync_WhenCodeIsNotTheFunctionKey(string queryString)
    {
        var response = await CreateFunction().Trigger(TestHelpers.CreateHttpRequestData(Url + queryString));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(string.Empty, TestHelpers.ReadBody(response));
        A.CallTo(() => _syncService.Sync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Trigger_LogsTheTimeTheSyncTook()
    {
        var logger = A.Fake<ILogger>();
        var loggerFactory = A.Fake<ILoggerFactory>();
        A.CallTo(() => loggerFactory.CreateLogger(A<string>._)).Returns(logger);
        var function = new FuncPendingConsentSync(_syncService, loggerFactory);

        await function.Trigger(TestHelpers.CreateHttpRequestData($"{Url}?code={FunctionKey}"));

        var logged = Fake.GetCalls(logger)
            .Where(call => call.Method.Name == nameof(ILogger.Log))
            .Select(call => call.Arguments[2]?.ToString() ?? string.Empty)
            .ToList();
        Assert.IsTrue(logged.Any(message => message.StartsWith("[NadobeTimer] pending-consent-sync elapsedMs=")),
            $"No timer line among: {string.Join(" | ", logged)}");
    }

    private FuncPendingConsentSync CreateFunction()
    {
        return new FuncPendingConsentSync(_syncService, new NullLoggerFactory());
    }
}
