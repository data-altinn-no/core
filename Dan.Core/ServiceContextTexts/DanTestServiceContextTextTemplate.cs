using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// All texts pertaining to the DAN-test service context
/// </summary>
public class DanTestServiceContextTextTemplate : IServiceContextTextTemplate<LocalizedString>
{
    public LocalizedString ConsentDelegationContexts => new()
    {
        En = $"In relation to a data.altinn.no test process {TextMacros.RequestorName} wants to get consent from you.",
        NoNn = $"I forbindelse med en testprosess i data.altinn.no, ønskjer {TextMacros.RequestorName} å få samtykkje frå deg.",
        NoNb = $"I forbindelse med en testprosess i data.altinn.no, ønsker {TextMacros.RequestorName} å få samtykke fra deg."
    };

    public LocalizedString CorrespondenceBody => new()
    {
        NoNb = $"For at {TextMacros.RequestorName} skal kunne gjennomføre testprosessen må det utstedes fullmakt for {TextMacros.SubjectName}."
    };

    public LocalizedString CorrespondenceSender => new()
    {
        NoNb = "DAN-test"
    };

    public LocalizedString CorrespondenceSummary => new()
    {
        NoNb = $"{TextMacros.RequestorName} har sendt en forespørsel om fullmakt for en testprosess."
    };

    public LocalizedString CorrespondenceTitle => new()
    {
        NoNb = $"{TextMacros.RequestorName} trenger fullmakt {TextMacros.ConsentReference}"
    };

    public LocalizedString EmailNotificationContent => new()
    {
        NoNb = $"Forespørselen gjelder testprosessen knyttet til {TextMacros.ConsentReference}."
    };

    public LocalizedString EmailNotificationSubject => new()
    {
        NoNb = $"{TextMacros.SubjectName} har mottatt en ny forespørsel om fullmakt fra {TextMacros.RequestorName} i Altinn"
    };


    public LocalizedString SMSNotificationContent => new()
    {
        NoNb = $"{TextMacros.SubjectName} har mottatt en ny forespørsel om fullmakt fra {TextMacros.RequestorName} i Altinn"
    };


    public LocalizedString ConsentButtonText => new()
    {
        NoNb = "Behandle forespørsel om fullmakt"
    };


    public LocalizedString ConsentGivenReceiptText => new()
    {
        NoNb = $"Din fullmakt til {TextMacros.Requestor} {TextMacros.RequestorName} på vegne av {TextMacros.Subject} {TextMacros.SubjectName} er registrert som gitt."
    };

    public LocalizedString ConsentDeniedReceiptText => new()
    {
        NoNb = $"Fullmakt til {TextMacros.Requestor} {TextMacros.RequestorName} på vegne av {TextMacros.Subject} {TextMacros.SubjectName} er registrert som avslått."
    };


    public LocalizedString ConsentTitleText => new()
    {
        NoNb = "Fullmakt"
    };

}