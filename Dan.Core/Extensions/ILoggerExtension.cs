using Dan.Common.Enums;
using Dan.Common.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Dan.Core.Extensions;

public static class LoggerExtensions
{
    public static readonly Action<ILogger, string, Exception?> _danLog;

    static LoggerExtensions()
    {
        _danLog = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(DanLog)),
            "{msg}"
        );
    }

    public static void DanLog(
        this ILogger logger,
        Accreditation accreditation,
        LogAction action,
        [CallerFilePath] string callingClass = "",
        [CallerMemberName] string callingMethod = "",
        string customData = "")
    {
        foreach (var a in accreditation.EvidenceCodes)
        {
            var msg = $"{action}:{Path.GetFileNameWithoutExtension(callingClass)}.{callingMethod}, accreditationid={accreditation.AccreditationId}, "
            + $"consentreference={accreditation.ConsentReference}, externalReference={accreditation.ExternalReference}, owner={accreditation.Owner}, "
            + $"requestor={accreditation.Requestor}, subject={accreditation.Subject}, evidenceCode={a.EvidenceCodeName}, timestamp={DateTime.UtcNow}, "
            + $"serviceContext={accreditation.ServiceContext}, customData={customData}";
            _danLog(logger, msg, null);
        }
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
            var msg = $"{action}:{Path.GetFileNameWithoutExtension(callingClass)}.{callingMethod}, accreditationid=null, "
            + $"subject={subject}, evidenceCode={evidenceCodeName}, timestamp={DateTime.UtcNow}, "
            + $"serviceContext={serviceContext}, customData={customData}";
            _danLog(logger, msg, null);
    }
}