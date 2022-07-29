using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// Contains the actual texts after language selection has been applied and macros processed
/// </summary>
public class RenderedServiceContextTexts : IServiceContextTextTemplate<string>
{
    public LocalizedString ConsentDelegationContexts { get; set; }

    public string CorrespondenceBody { get; set; }

    public string CorrespondenceSender { get; set; }

    public string CorrespondenceSummary { get; set; }

    public string CorrespondenceTitle { get; set; }

    public string EmailNotificationContent { get; set; }

    public string EmailNotificationSubject { get; set; }

    public string SMSNotificationContent { get; set; }

    public string ConsentButtonText { get; set; }

    public string ConsentGivenReceiptText { get; set; }

    public string ConsentDeniedReceiptText { get; set; }

    public string ConsentTitleText { get; set; }
}