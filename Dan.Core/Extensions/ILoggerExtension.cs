using Dan.Common.Enums;
using Dan.Common.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Dan.Core.Extensions;

public static class LoggerExtensions
{
    private const string LogString = "{action}:{callingClass}.{callingMethod},a={accreditationId},c={consentReference},e={externalReference},o={owner},r={requestor},s={subject},d={evidenceCodeName},t={dateTime},sc={serviceContext}";

    public static void DanLog(
        this ILogger logger,
        Accreditation accreditation,
        LogAction action,
        string? dataSetName = null,
        [CallerFilePath] string callingClass = "",
        [CallerMemberName] string callingMethod = "")
    {

        logger.LogInformation(LogString, Enum.GetName(typeof(LogAction), action),
            Path.GetFileNameWithoutExtension(callingClass), callingMethod, accreditation.AccreditationId,
            accreditation.ConsentReference, accreditation.ExternalReference, accreditation.Owner,
            accreditation.RequestorParty?.ToString(), accreditation.SubjectParty?.ToString(), dataSetName,
            DateTime.UtcNow, accreditation.ServiceContext);
    }

    public static void DanLog(
        this ILogger logger,
        string subject,
        string evidenceCodeName,
        string serviceContext,
        LogAction action,
        [CallerFilePath] string callingClass = "",
        [CallerMemberName] string callingMethod = "")
    {
        logger.LogInformation(LogString, Enum.GetName(typeof(LogAction), action),
            Path.GetFileNameWithoutExtension(callingClass), callingMethod, null, "", "", "", "", subject,
            evidenceCodeName, DateTime.UtcNow, serviceContext);
    }
}