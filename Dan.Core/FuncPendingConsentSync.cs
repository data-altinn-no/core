using System.Net;
using Dan.Core.Attributes;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core;

/// <summary>
/// Azure Function storing the answer to consent requests that were answered in Altinn without the subject returning
/// to the consent receipt
/// </summary>
public class FuncPendingConsentSync
{
    /// <summary>
    /// Monday to Friday at 07, 11, 15 and 19 UTC, which is 08, 12, 16 and 20 in Norway in winter and an hour later in
    /// summer. Consent requests are answered by people, so the runs follow the day rather than the clock around.
    /// </summary>
    public const string WorkdaySchedule = "0 0 7,11,15,19 * * 1-5";

    /// <summary>
    /// Saturday and Sunday at 11 UTC, which is 12 in Norway in winter and 13 in summer
    /// </summary>
    public const string WeekendSchedule = "0 0 11 * * 0,6";

    private readonly IPendingConsentSyncService _pendingConsentSyncService;
    private readonly ILogger<FuncPendingConsentSync> _logger;

    public FuncPendingConsentSync(IPendingConsentSyncService pendingConsentSyncService, ILoggerFactory loggerFactory)
    {
        _pendingConsentSyncService = pendingConsentSyncService;
        _logger = loggerFactory.CreateLogger<FuncPendingConsentSync>();
    }

    /// <summary>
    /// Runs the sync on request, for testing and debugging. Unlike the timer this is also registered in a debug build.
    /// The "code" query parameter must be the function key in settings, the same value that forces a refresh of the
    /// evidence codes.
    /// </summary>
    /// <param name="req">The HTTP request object</param>
    /// <returns>The number of accreditations checked, and how many were stored as granted or denied or failed</returns>
    [Function("PendingConsentSyncTrigger"), NoAuthentication]
    public async Task<HttpResponseData> Trigger(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "debug/pendingconsentsync")]
        HttpRequestData req)
    {
        var code = req.GetQueryParam("code");
        if (string.IsNullOrEmpty(code) || code != Settings.FunctionKeyValue)
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        return req.CreateExternalResponse(HttpStatusCode.OK, await RunSync());
    }

#if !DEBUG
    /// <summary>
    /// Runs four times a day on workdays, see <see cref="WorkdaySchedule"/>. The answer is normally stored when the
    /// subject is redirected to the consent receipt; this picks up the ones where that never happened, so that the
    /// status and list endpoints, which only read the stored status, show the consent as given or denied.
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <returns></returns>
    [Function("PendingConsentSyncWorkdays")]
    public async Task RunOnWorkdays([TimerTrigger(WorkdaySchedule)] TimerInfo timerInfo)
    {
        await RunSync();
    }

    /// <summary>
    /// Runs once a day in the weekend, see <see cref="WeekendSchedule"/>. A timer has one schedule, hence two timers.
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <returns></returns>
    [Function("PendingConsentSyncWeekends")]
    public async Task RunOnWeekends([TimerTrigger(WeekendSchedule)] TimerInfo timerInfo)
    {
        await RunSync();
    }
#endif

    private async Task<PendingConsentSyncResult> RunSync()
    {
        PendingConsentSyncResult result;
        using (_logger.Timer("pending-consent-sync"))
        {
            result = await _pendingConsentSyncService.Sync();
        }

        _logger.LogInformation("Completed pending consent sync checked={checked} granted={granted} denied={denied} failed={failed} notChecked={notChecked}",
            result.Checked, result.Granted, result.Denied, result.Failed, result.NotChecked);

        return result;
    }
}
