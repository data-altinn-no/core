using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// Contains the actual texts after language selection has been applied and macros processed.
///
/// The texts have different formats. <see cref="ConsentGivenReceiptText"/>, <see cref="ConsentDeniedReceiptText"/>
/// and <see cref="CorrespondenceBody"/> are HTML, and <see cref="CorrespondenceBodyA3"/> is Markdown. The values
/// inserted in these are encoded for that format, so they can be used as markup as they are. All other texts are
/// plain text with the values as they were supplied, and must be encoded by whoever places them in HTML or Markdown.
/// </summary>
public class RenderedServiceContextTexts : IServiceContextTextTemplate<string>
{
    public LocalizedString ConsentDelegationContexts { get; set; } = new();

    public string CorrespondenceBody { get; set; } = string.Empty;

    public string CorrespondenceBodyA3 { get; set; } = string.Empty;

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