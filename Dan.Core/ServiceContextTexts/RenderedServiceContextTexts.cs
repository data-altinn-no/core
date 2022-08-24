using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// Contains the actual texts after language selection has been applied and macros processed
/// </summary>
public class RenderedServiceContextTexts : IServiceContextTextTemplate<string>
{
    public LocalizedString ConsentDelegationContexts { get; set; } = new();

    public string CorrespondenceBody { get; set; } = string.Empty;

    public string CorrespondenceSender { get; set; } = string.Empty;

    public string CorrespondenceSummary { get; set; } = string.Empty;

    public string CorrespondenceTitle { get; set; } = string.Empty;

    public string EmailNotificationContent { get; set; } = string.Empty;

    public string EmailNotificationSubject { get; set; } = string.Empty;

    public string SMSNotificationContent { get; set; } = string.Empty;

    public string ConsentButtonText { get; set; } = string.Empty;

    public string ConsentGivenReceiptText { get; set; } = string.Empty;

    public string ConsentDeniedReceiptText { get; set; } = string.Empty;

    public string ConsentTitleText { get; set; } = string.Empty;
}