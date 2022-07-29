using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// All texts pertaining to the seriøsitetsinformasjon service context
/// </summary>
public class SeriositetServiceContextTextTemplate : IServiceContextTextTemplate<LocalizedString>
{
    public LocalizedString ConsentDelegationContexts => new LocalizedString()
    {
        En = $"{TextMacros.RequestorName} ønsker å hente opplysninger om din virksomhet til bruk i sin vurderingstjeneste. Ved å gi fullmakt gir du Skatteetaten rett til å utlevere opplysninger om virksomheten til Digitaliserinsgdirektoratet, som vil presentere disse sammen med opplysninger fra Brønnøysundregistrene og deretter dele dem med {TextMacros.RequestorName}. Hvis du ønsker å se hvilke opplysninger som leveres ut for din virksomhet fra Skatteetaten kan du bestille Opplysninger om skatt og avgift i Altinn(https://www.altinn.no/skjemaoversikt/skatteetaten/bestilling-av-opplysninger-om-skatt-og-avgift-rf-1507), da får du de samme opplysningene levert i din innboks i Altinn. Fullmakten er gyldig i 3 måneder. Du kan når som helst trekke fullmakten i Altinn. {TextMacros.RequestorName} kan hente opplysningene så mange ganger de vil i perioden.",
        NoNn = $"{TextMacros.RequestorName} ønsker å hente opplysninger om din virksomhet til bruk i sin vurderingstjeneste. Ved å gi fullmakt gir du Skatteetaten rett til å utlevere opplysninger om virksomheten til Digitaliserinsgdirektoratet, som vil presentere disse sammen med opplysninger fra Brønnøysundregistrene og deretter dele dem med {TextMacros.RequestorName}. Hvis du ønsker å se hvilke opplysninger som leveres ut for din virksomhet fra Skatteetaten kan du bestille Opplysninger om skatt og avgift i Altinn(https://www.altinn.no/skjemaoversikt/skatteetaten/bestilling-av-opplysninger-om-skatt-og-avgift-rf-1507), da får du de samme opplysningene levert i din innboks i Altinn. Fullmakten er gyldig i 3 måneder. Du kan når som helst trekke fullmakten i Altinn. {TextMacros.RequestorName} kan hente opplysningene så mange ganger de vil i perioden.",
        NoNb = $"{TextMacros.RequestorName} ønsker å hente opplysninger om din virksomhet til bruk i sin vurderingstjeneste. Ved å gi fullmakt gir du Skatteetaten rett til å utlevere opplysninger om virksomheten til Digitaliserinsgdirektoratet, som vil presentere disse sammen med opplysninger fra Brønnøysundregistrene og deretter dele dem med {TextMacros.RequestorName}. Hvis du ønsker å se hvilke opplysninger som leveres ut for din virksomhet fra Skatteetaten kan du bestille Opplysninger om skatt og avgift i Altinn(https://www.altinn.no/skjemaoversikt/skatteetaten/bestilling-av-opplysninger-om-skatt-og-avgift-rf-1507), da får du de samme opplysningene levert i din innboks i Altinn. Fullmakten er gyldig i 3 måneder. Du kan når som helst trekke fullmakten i Altinn. {TextMacros.RequestorName} kan hente opplysningene så mange ganger de vil i perioden."
    };

    public LocalizedString CorrespondenceBody => new LocalizedString()
    {
        NoNb = $"I forbindelse med et prøveprosjekt (pilot) trenger {TextMacros.RequestorName} fullmakt for å få tilgang til informasjon fra Skatteetaten.<br> Ved å gi fullmakt godtar du at opplysninger om {TextMacros.SubjectName} fra Skatteetaten presenteres sammen med opplysninger fra Brønnøysundregistrene og sendes til {TextMacros.RequestorName}. Dette prøveprosjektet er del av regjeringens strategi mot arbeidslivskriminalitet.<br><br>For at {TextMacros.RequestorName} skal kunne motta de aktuelle opplysningene, må du gi fullmakt til {TextMacros.RequestorName} på vegne av {TextMacros.SubjectName}. <br><br>Klikk på knappen under for å se hvilke opplysninger som hentes inn fra Skatteetaten når du gir fullmakt. {TextMacros.Button}"
    };

    public LocalizedString CorrespondenceSender => new LocalizedString()
    {
        NoNb = $"{TextMacros.RequestorName}"
    };

    public LocalizedString CorrespondenceSummary => new LocalizedString()

    {
        NoNb = $"{TextMacros.RequestorName} trenger fullmakt for å få tilgang til informasjon fra Skatteetaten, ref Vurderingstjeneste - virksomhet - (Tjenesteeier: Brønnøysundregistrene)"
    };

    public LocalizedString CorrespondenceTitle => new LocalizedString()
    {
        NoNb = $"{TextMacros.RequestorName} trenger fullmakt for å få tilgang til informasjon fra Skatteetaten"
    };


    public LocalizedString EmailNotificationContent => new LocalizedString()
    {
        NoNb = $"Forespørselen gjelder seriøsitetsinformasjon knyttet til {TextMacros.ConsentReference}."
    };


    public LocalizedString EmailNotificationSubject => new LocalizedString()

    {
        NoNb = $"{TextMacros.SubjectName} har mottatt en ny forespørsel om fullmakt fra {TextMacros.RequestorName} i Altinn"
    };

    public LocalizedString SMSNotificationContent => new LocalizedString()
    {
        NoNb = $"{TextMacros.SubjectName} har mottatt en ny forespørsel om fullmakt fra {TextMacros.RequestorName} i Altinn"
    };


    public LocalizedString ConsentButtonText => new LocalizedString()
    {
        NoNb = "Behandle forespørsel om fullmakt"
    };


    public LocalizedString ConsentGivenReceiptText => new LocalizedString()

    {
        NoNb = $"Det er registrert at du på vegne av {TextMacros.SubjectName} har gitt fullmakt til at {TextMacros.RequestorName} kan hente statusinformasjon om {TextMacros.SubjectName} fra Skatteetaten til Vurderingstjeneste - virksomhet"
    };


    public LocalizedString ConsentDeniedReceiptText => new LocalizedString()
    {
        NoNb = $"Det er registrert at du på vegne av {TextMacros.SubjectName} har avslått fullmaktsforespørselen fra {TextMacros.RequestorName}. </br>Dataene som er bedt om vil ikke bli utlevert."
    };


    public LocalizedString ConsentTitleText => new LocalizedString()
    {
        NoNb = "Fullmakt"
    };
}