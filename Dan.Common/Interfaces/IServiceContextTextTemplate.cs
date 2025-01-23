namespace Dan.Common.Interfaces;

/// <summary>
/// All possible texts used in a service context with macros to be processed - either as actual strings or LocalisedString objects containing three languages
/// </summary>
public interface IServiceContextTextTemplate<out T>
{
    /// <summary>
    /// Localised texts for consent request message
    /// </summary>
    public LocalizedString ConsentDelegationContexts { get; }

    /// <summary>
    /// Email notification subject line
    /// </summary>
    public T EmailNotificationSubject { get; }

    /// <summary>
    /// Email notification content
    /// </summary>
    public T EmailNotificationContent { get; }

    /// <summary>
    /// SMS notification content
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public T SMSNotificationContent { get; }

    /// <summary>
    /// Correspondence sender name
    /// </summary>
    public T CorrespondenceSender { get; }

    /// <summary>
    /// Correspondence title
    /// </summary>
    public T CorrespondenceTitle { get; }

    /// <summary>
    /// Correspondence summary
    /// </summary>
    public T CorrespondenceSummary { get; }

    /// <summary>
    /// Correspondence body
    /// </summary>
    public T CorrespondenceBody { get; }

    /// <summary>
    /// Button text for giving consent
    /// </summary>
    public T ConsentButtonText { get; }

    /// <summary>
    /// Receipt when giving consent
    /// </summary>
    public T ConsentGivenReceiptText { get; }

    /// <summary>
    /// Receipt when denying consent
    /// </summary>
    public T ConsentDeniedReceiptText { get; }

    /// <summary>
    /// Consent title text
    /// </summary>
    public T ConsentTitleText { get; }
}