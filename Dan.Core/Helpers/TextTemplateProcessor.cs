using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.ServiceContextTexts;
using System.Text.RegularExpressions;
using Dan.Common;

namespace Dan.Core.Helpers;

public static class TextTemplateProcessor
{

    private static string GetConsentButton(string buttonText, string consenturl)
    {
        return $"<a href =\"{consenturl}\" class=\"a-btn mt-2\">{buttonText}</a>";
    }

    public static IServiceContextTextTemplate<string> GetRenderedTexts(ServiceContext context, Accreditation acc, string requestorName, string subjectName, string consentUrl)
    {
        var template = context.ServiceContextTextTemplate;

        string buttonText = GetLocalisedTemplate(template.ConsentButtonText, acc.LanguageCode);

        RenderedServiceContextTexts result = new RenderedServiceContextTexts()
        {
            ConsentButtonText = buttonText,
            ConsentDelegationContexts = ProcessConsentRequestMacros(template.ConsentDelegationContexts, acc, requestorName, subjectName, context.Name),

            ConsentDeniedReceiptText = ProcessMacros(GetLocalisedTemplate(template.ConsentDeniedReceiptText, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            ConsentGivenReceiptText = ProcessMacros(GetLocalisedTemplate(template.ConsentGivenReceiptText, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            ConsentTitleText = ProcessMacros(GetLocalisedTemplate(template.ConsentTitleText, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            CorrespondenceBody = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceBody, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            CorrespondenceSender = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceSender, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            CorrespondenceSummary = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceSummary, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            CorrespondenceTitle = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceTitle, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            EmailNotificationContent = ProcessMacros(GetLocalisedTemplate(template.EmailNotificationContent, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            EmailNotificationSubject = ProcessMacros(GetLocalisedTemplate(template.EmailNotificationSubject, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText),
            SMSNotificationContent = ProcessMacros(GetLocalisedTemplate(template.SMSNotificationContent, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText)
        };

        return result;
    }

    private static string ProcessMacros(string input, Accreditation acc, string requestorName = "", string subjectName = "", string serviceContextName = "", string consentUrl = "", string buttonText = "")
    {
        input = acc.Requestor != "" ? input.Replace(TextMacros.Requestor, acc.Requestor, StringComparison.InvariantCultureIgnoreCase) : input;
        input = requestorName != "" ? input.Replace(TextMacros.RequestorName, requestorName, StringComparison.InvariantCultureIgnoreCase) : input;
        input = acc.Subject != "" ? input.Replace(TextMacros.Subject, acc.Subject, StringComparison.InvariantCultureIgnoreCase) : input;
        input = subjectName != "" ? input.Replace(TextMacros.SubjectName, subjectName, StringComparison.InvariantCultureIgnoreCase) : input;
        input = serviceContextName != "" ? input.Replace(TextMacros.ServiceContextName, serviceContextName, StringComparison.InvariantCultureIgnoreCase) : input;
        input = acc.ConsentReference != "" ? input.Replace(TextMacros.ConsentReference, acc.ConsentReference, StringComparison.InvariantCultureIgnoreCase) : input;
        input = acc.ExternalReference != "" ? input.Replace(TextMacros.ExternalReference, acc.ExternalReference, StringComparison.InvariantCultureIgnoreCase) : input;
        input = buttonText != "" ? input.Replace(TextMacros.Button, GetConsentButton(buttonText, consentUrl), StringComparison.InvariantCultureIgnoreCase) : input;
        input = acc.ConsentReference != "" ? input.Replace(TextMacros.EbevisReference, GetEbevisRef(acc.ConsentReference), StringComparison.InvariantCultureIgnoreCase) : input;

        input = input.Replace(TextMacros.ConsentOrExternalReference, acc.ConsentReference != "" ? acc.ConsentReference : acc.ExternalReference);
        input = input.Replace(TextMacros.ConsentAndExternalReference, acc.ConsentReference + acc.ExternalReference != "" ? ", " + acc.ExternalReference : "");
        return input;
    }

    public static LocalizedString ProcessConsentRequestMacros(LocalizedString input, Accreditation acc, string requestorName = "", string subjectName = "", string serviceContextName = "")
    {
        return new LocalizedString()
        {
            En = ProcessMacros(input.En, acc, requestorName, subjectName, serviceContextName),
            NoNb = ProcessMacros(input.NoNb, acc, requestorName, subjectName, serviceContextName),
            NoNn = ProcessMacros(input.NoNn, acc, requestorName, subjectName, serviceContextName),
        };
    }


    private static string GetLocalisedTemplate(LocalizedString stringTemplates, string accrediationLanguageCode)
    {
        string res = string.Empty;

        if (accrediationLanguageCode == Constants.LANGUAGE_CODE_ENGLISH)
        {
            res = stringTemplates.En;
        }
        else if (accrediationLanguageCode == Constants.LANGUAGE_CODE_NORWEGIAN_NN)
        {
            res = stringTemplates.NoNn;
        }
        else
        {
            res = stringTemplates.NoNb;
        }

        //Indicates we have forgot to define texts for servicecontext
        if (res == string.Empty)
            throw new ServiceContextException();

        return res;
    }

    private static string GetEbevisRef(string consentReference)
    {
        string caseReferenceBody = string.Empty;

        if (IsDoffinReference(consentReference))
        {
            caseReferenceBody = string.Format(Settings.GetDoffinLinkTemplate(), consentReference);
        }
        else if (IsTedReference(consentReference))
        {
            caseReferenceBody = string.Format(Settings.GetTedLinkTemplate(), consentReference);
        }
        else
        {
            caseReferenceBody = consentReference;
        }

        return caseReferenceBody;
    }

    private static bool IsTedReference(string consentReference)
    {
        var validChars = new Regex(@"^[0-9]{1,8}-[0-9]{4}$");
        return validChars.IsMatch(consentReference);
    }

    private static bool IsDoffinReference(string consentReference)
    {
        var validChars = new Regex(@"^[0-9]{4}-[0-9]{1,8}$");
        return validChars.IsMatch(consentReference);
    }
}
