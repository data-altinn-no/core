namespace Dan.Common.Interfaces;

/// <summary>
/// All possible texts used in a service context with macros to be processed - either as actual strings or LocalisedString objects containing three languages
/// </summary>
public interface IServiceContextTextTemplate<out T>
{
    public LocalizedString ConsentDelegationContexts { get; }

    public T EmailNotificationSubject { get; }

    public T EmailNotificationContent { get; }

    // ReSharper disable once InconsistentNaming
    public T SMSNotificationContent { get; }

    public T CorrespondenceSender { get; }

    public T CorrespondenceTitle { get; }

    public T CorrespondenceSummary { get; }

    public T CorrespondenceBody { get; }

    public T ConsentButtonText { get; }

    public T ConsentGivenReceiptText { get; }

    public T ConsentDeniedReceiptText { get; }

    public T ConsentTitleText { get; }
}