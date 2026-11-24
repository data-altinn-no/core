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
    /// Runs four times a day (00, 06, 12 and 18 UTC). The answer is normally stored when the subject is redirected to
    /// the consent receipt; this picks up the ones where that never happened, so that the status and list endpoints,
    /// which only read the stored status, show the consent as given or denied.
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <returns></returns>
    [Function("PendingConsentSync")]
    public async Task Run([TimerTrigger("0 0 */6 * * *")] TimerInfo timerInfo)
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

        _logger.LogInformation("Completed pending consent sync checked={checked} granted={granted} denied={denied} failed={failed}",
            result.Checked, result.Granted, result.Denied, result.Failed);

        return result;
    }
}
