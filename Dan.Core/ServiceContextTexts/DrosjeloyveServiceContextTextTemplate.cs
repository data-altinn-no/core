using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// All texts pertaining to the drosjeløyve service context
/// </summary>
public class DrosjeloyveServiceContextTextTemplate : IServiceContextTextTemplate<LocalizedString>
{
    public LocalizedString ConsentDelegationContexts => new LocalizedString()
    {
        En = $"In relation to your application for a taxi permit, {TextMacros.RequestorName} wants to retrieve information about the enterprise's financial standing. The information in question is a confirmation that the enterprise is not insolvent and a survey of outstanding taxes from the Tax Department.",
        NoNn = $"I samband med søknaden din om drosjeløyve, ønskjer {TextMacros.RequestorName} å hente opplysingar om den økonomiske evna til føretaket. Opplysingane det gjeld er ei stadfesting frå Konkursregisteret på at føretaket ikkje er under konkurshandsaming, og eit oversyn over restansar føretaket har hos Skatteetaten.",
        NoNb = $"I forbindelse med din søknad om drosjeløyve, ønsker {TextMacros.RequestorName} å hente opplysninger om foretakets økonomiske evne. Opplysningene det gjelder er en bekreftelse fra Konkursregisteret på at foretaket ikke er under konkursbehandling, og en oversikt over foretakets restanser hos Skatteetaten.​"

    };

    public LocalizedString CorrespondenceBody => new LocalizedString()
    {
        NoNb = $"{TextMacros.RequestorName} har sendt en forespørsel om fullmakt til å hente ut skatteattest og konkursattest for din virksomhet. For at {TextMacros.RequestorName} skal kunne behandle din søknad om drosjeløyve, må du gi fullmakt til å hente ut disse attestene for {TextMacros.SubjectName}. <br><br>Klikk på knappen under for å få vite mer om hvilke opplysninger løyvemyndigheten spør etter. <br></br> Dersom du ikke vil gi fullmakt til at løyvemyndigheten henter opplysninger om foretakets økonomiske evne eller trekker en gitt fullmakt, men likevel ønsker å søke om drosjeløyve, må du sende søknaden med vedlegg per post til din fylkeskommune.​<br><br>  {TextMacros.Button}",
        NoNn = $"{TextMacros.RequestorName} har sendt ein førespurnad om fullmakt til å hente ut skatteattest og konkursattest for verksemda di. For at {TextMacros.RequestorName} skal kunne handsame søknaden din om drosjeløyve, må du gje fullmakt til å hente ut desse attestane for {TextMacros.SubjectName}. <br><br>Klikk på knappen under for å få vite meir om kva slags opplysingar løyvestyresmakta spør etter. <br></br> Dersom du ikkje vil gje fullmakt til at løyvestyremakta hentar opplysingar om den økonomiske evna til føretaket eller trekker ein fullmakt som er gjeve, men likevel ønskjer å søke om drosjeløyve, må du sende søknaden med vedlegg per post til din fylkeskommune.​<br><br>  {TextMacros.Button}",
    };

    public LocalizedString CorrespondenceSender => new LocalizedString()
    {
        NoNb = $"{TextMacros.RequestorName}",
        NoNn = $"{TextMacros.RequestorName}"
    };


    public LocalizedString CorrespondenceSummary => new LocalizedString()
    {
        NoNn = $"{TextMacros.RequestorName} har sendt ein førespurnad om fullmakt for uthenting av opplysingar om verksemda di",
        NoNb = $"{TextMacros.RequestorName} har sendt en forespørsel om fullmakt for uthenting av opplysninger om din virksomhet."
    };


    public LocalizedString CorrespondenceTitle => new LocalizedString()
    {
        NoNn = $"{TextMacros.RequestorName} ber om fullmakt {TextMacros.ConsentAndExternalReference}",
        NoNb = $"{TextMacros.RequestorName} ber om fullmakt {TextMacros.ConsentAndExternalReference}"
    };

    public LocalizedString EmailNotificationContent => new LocalizedString()
    {
        NoNn = $"Førespurnaden gjeld søknaden din om drosjeløyve: {TextMacros.ConsentReference}.",
        NoNb = $"Forespørselen gjelder din søknad om drosjeløyve: {TextMacros.ConsentReference}."
    };


    public LocalizedString EmailNotificationSubject => new LocalizedString()
    {
        NoNn = $"{TextMacros.SubjectName} har mottatt ein ny førespurnad om fullmakt frå {TextMacros.RequestorName} i Altinn",
        NoNb = $"{TextMacros.SubjectName} har mottatt en ny forespørsel om fullmakt fra {TextMacros.RequestorName} i Altinn"
    };


    public LocalizedString SMSNotificationContent => new LocalizedString()
    {
        NoNn = $"{TextMacros.SubjectName} har mottatt ein ny førespurnad om fullmakt frå {TextMacros.RequestorName} i Altinn",
        NoNb = $"{TextMacros.SubjectName} har mottatt en ny forespørsel om fullmakt fra {TextMacros.RequestorName} i Altinn"
    };

    public LocalizedString ConsentButtonText => new LocalizedString()
    {
        NoNb = "Behandle forespørsel om fullmakt",
        NoNn = "Svar på førespurnad om fullmakt"
    };


    public LocalizedString ConsentGivenReceiptText => new LocalizedString()
    {
        NoNn = $"Det er registrert at du på vegner av {TextMacros.SubjectName} har gjeve fullmakt til at {TextMacros.RequestorName} hentar opplysingar om den økonomiske evna til føretaket frå Skatteetaten og Konkursregisteret. </br>Informasjonen vil no bli henta og brukt til å handsame søknad om drosjeløyve, med referansenummer i Altinn {TextMacros.ConsentReference}.",
        NoNb = $"Det er registrert at du på vegne av {TextMacros.SubjectName} har gitt fullmakt til at {TextMacros.RequestorName} henter opplysninger om foretakets økonomiske evne fra Skatteetaten og Konkursregisteret. </br>Informasjonen vil nå bli hentet og brukt til å behandle søknad om drosjeløyve, med referansenummer i Altinn {TextMacros.ConsentReference}."
    };

    public LocalizedString ConsentDeniedReceiptText => new LocalizedString()
    {
        NoNn = $"Fullmakt til {TextMacros.RequestorName} på vegner av {TextMacros.SubjectName} er registrert som avslått. </br>Dataa som er bedne om i samband med søknad om drosjeløyve med referanse {TextMacros.ConsentReference} vil ikkje bli utleverte.",
        NoNb = $"Fullmakt til {TextMacros.RequestorName} på vegne av {TextMacros.SubjectName} er registrert som avslått. </br>Dataene som er bedt om i forbindelse med søknad om drosjeløyve med referanse {TextMacros.ConsentReference} vil ikke bli utlevert."
    };

    public LocalizedString ConsentTitleText => new LocalizedString()
    {
        NoNn = "Fullmakt",
        NoNb = "Fullmakt",
        En = "Power of attorney"
    };
}