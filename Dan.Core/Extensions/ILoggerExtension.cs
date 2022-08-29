using Dan.Common.Enums;
using Dan.Common.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Dan.Core.Extensions;

public static class LoggerExtensions
{
    public static string LogString = "{action}:{callingClass}.{callingMethod}, accreditationid={accreditationId}, consentreference={consentReference}, externalReference={externalReference}, owner={owner}, requestor={requestor}, subject={subject}, evidenceCode={evidenceCodeName}, timestamp={dateTime}, serviceContext={serviceContext}, customData={customData}";

    public static void DanLog(
        this ILogger logger,
        Accreditation accreditation,
        LogAction action,
        [CallerFilePath] string callingClass = "",
        [CallerMemberName] string callingMethod = "",
        string customData = "")
    {
        foreach (var a in accreditation.EvidenceCodes)
            logger.LogInformation(LogString, Enum.GetName(typeof(LogAction), action),
                Path.GetFileNameWithoutExtension(callingClass), callingMethod, accreditation.AccreditationId,
                accreditation.ConsentReference, accreditation.ExternalReference, accreditation.Owner,
                accreditation.RequestorParty?.ToString(), accreditation.SubjectParty?.ToString(), a.EvidenceCodeName,
                DateTime.UtcNow, accreditation.ServiceContext, customData);
    }

    public static void DanLog(
        this ILogger logger,
        string subject,
        string evidenceCodeName,
        string serviceContext,
        LogAction action,
        [CallerFilePath] string callingClass = "",
        [CallerMemberName] string callingMethod = "",
        string customData = "")
    {
        logger.LogInformation(LogString, Enum.GetName(typeof(LogAction), action),
            Path.GetFileNameWithoutExtension(callingClass), callingMethod, null, "", "", "", "", subject,
            evidenceCodeName, DateTime.UtcNow, serviceContext, customData);
    }
}