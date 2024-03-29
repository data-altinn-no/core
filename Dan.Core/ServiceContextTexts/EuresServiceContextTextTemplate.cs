﻿using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;

namespace Dan.Core.ServiceContextTexts;

/// <summary>
/// All texts pertaining to the eures service context
/// </summary>
public class EuresServiceContextTextTemplate : IServiceContextTextTemplate<LocalizedString>
{
    public LocalizedString ConsentDelegationContexts => new()
    {
        En = $"In relation to an eBevis qualification process {TextMacros.RequestorName} wants to retrieve information about your enterprise.",
        NoNn = $"I forbindelse med en kvalifikasjonsprosess i eBevis, ønskjer {TextMacros.RequestorName} å hente opplysninger om din virksomhet.",
        NoNb = $"I forbindelse med en kvalifikasjonsprosess i eBevis, ønsker {TextMacros.RequestorName} å hente opplysninger om din virksomhet."
    };


    public LocalizedString CorrespondenceBody => new()
    {
        NoNb = $"For at {TextMacros.RequestorName} skal kunne gjennomføre en kvalifiseringsprosess må det utstedes fullmakt til å hente ut opplysninger om {TextMacros.SubjectName}.<br><br>Referanse: {TextMacros.EbevisReference} <br><br>Klikk på knappen under for å vite mer om hvilke opplysninger som er forespurt før du eventuelt gir fullmakten.<br> {TextMacros.Button}"
    };


    public LocalizedString CorrespondenceSender => new()
    {
        NoNb = "eBevis"
    };


    public LocalizedString CorrespondenceSummary => new()
    {
        NoNb = $"{TextMacros.RequestorName} har sendt en forespørsel om fullmakt for uthenting av opplysninger om din virksomhet."
    };


    public LocalizedString CorrespondenceTitle => new()
    {
        NoNb = $"{TextMacros.RequestorName} trenger fullmakt {TextMacros.ConsentReference}"
    };


    public LocalizedString EmailNotificationContent => new()
    {
        NoNb = $"Forespørselen gjelder deres tilbud knyttet til {TextMacros.ConsentReference}."
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
        NoNb = $"Din fullmakt til {TextMacros.Requestor} {TextMacros.RequestorName} på vegne av {TextMacros.Subject} {TextMacros.SubjectName} er registrert som gitt. </br>Informasjonen som er bedt om kan n&aring; hentes fra de respektive datakilder i forbindelse med {TextMacros.EbevisReference} på tjenesten {TextMacros.ServiceContextName}."
    };


    public LocalizedString ConsentDeniedReceiptText => new()
    {
        NoNb = $"Fullmakt til {TextMacros.Requestor} {TextMacros.RequestorName} på vegne av {TextMacros.Subject} {TextMacros.SubjectName} er registrert som avslått. </br>Dataene som er bedt om i forbindelse med {TextMacros.EbevisReference} på tjenesten {TextMacros.ServiceContextName} vil ikke bli utlevert."
    };


    public LocalizedString ConsentTitleText => new()
    {
        NoNb = "Fullmakt"
    };

}