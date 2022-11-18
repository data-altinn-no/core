using Dan.Common.Enums;
using Dan.Common.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Dan.Core.Extensions;

public static class LoggerExtensions
{
    // Add "prop__" to emulate in-process behaviour. This is needed to be consistent with historic data.
    public static string LogString = "{prop__action}:{prop__callingClass}.{prop__callingMethod}, accreditationid={prop__accreditationId}, consentreference={prop__consentReference}, externalReference={prop__externalReference}, owner={prop__owner}, requestor={prop__requestor}, subject={prop__subject}, evidenceCode={prop__evidenceCodeName}, timestamp={prop__dateTime}, serviceContext={prop__serviceContext}, customData={prop__customData}";

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