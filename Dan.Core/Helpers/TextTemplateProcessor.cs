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

    private static string GetConsentButtonMd(string buttonText, string consenturl)
    {
        return $"[{buttonText}]({consenturl})";
    }

    public static IServiceContextTextTemplate<string> GetRenderedTexts(ServiceContext context, Accreditation acc, string requestorName, string subjectName, string? consentUrl, bool useAltinn3 = false)
    {
        var template = context.ServiceContextTextTemplate;
        if (template == null)
        {
            throw new ServiceContextException($"The current service context, {context.Name}, does not define a ServiceContextTextTemplate");
        }

        if (acc.LanguageCode == null)
        {
            throw new ServiceContextException($"The given accreditation, {acc.AccreditationId}, does not specify a LanguageCode");
        }

        consentUrl ??= string.Empty;

        string buttonText = GetLocalisedTemplate(template.ConsentButtonText, acc.LanguageCode);

        RenderedServiceContextTexts result = new RenderedServiceContextTexts()
        {
            ConsentButtonText = buttonText,
            ConsentDelegationContexts = ProcessConsentRequestMacros(template.ConsentDelegationContexts, acc, requestorName, subjectName, context.Name),

            // The formats are documented on RenderedServiceContextTexts
            ConsentDeniedReceiptText = ProcessMacros(GetLocalisedTemplate(template.ConsentDeniedReceiptText, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3, TextFormat.Html),
            ConsentGivenReceiptText = ProcessMacros(GetLocalisedTemplate(template.ConsentGivenReceiptText, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3, TextFormat.Html),
            ConsentTitleText = ProcessMacros(GetLocalisedTemplate(template.ConsentTitleText, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3),
            CorrespondenceBody = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceBody, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3, TextFormat.Html),
            CorrespondenceBodyA3 = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceBodyA3, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3, TextFormat.Markdown),
            CorrespondenceSender = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceSender, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3),
            CorrespondenceSummary = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceSummary, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3),
            CorrespondenceTitle = ProcessMacros(GetLocalisedTemplate(template.CorrespondenceTitle, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3),
            EmailNotificationContent = ProcessMacros(GetLocalisedTemplate(template.EmailNotificationContent, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3),
            EmailNotificationSubject = ProcessMacros(GetLocalisedTemplate(template.EmailNotificationSubject, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3),
            SMSNotificationContent = ProcessMacros(GetLocalisedTemplate(template.SMSNotificationContent, acc.LanguageCode), acc, requestorName, subjectName, context.Name, consentUrl, buttonText, useAltinn3)
        };

        return result;
    }

    /// <summary>
    /// The format of a text template, which decides how the values that replace the macros are encoded
    /// </summary>
    private enum TextFormat
    {
        /// <summary>Plain text. Values are inserted as they are; whoever places the text in markup must encode it.</summary>
        Plain,

        /// <summary>The template contains HTML. All values are HTML encoded.</summary>
        Html,

        /// <summary>The template contains Markdown. The references supplied by the client are escaped.</summary>
        Markdown
    }

    private static string ProcessMacros(string? input, Accreditation acc, string requestorName = "", string subjectName = "", string serviceContextName = "", string consentUrl = "", string buttonText = "", bool isAltinn3 = false, TextFormat format = TextFormat.Plain)
    {
        if (input == null) return string.Empty;

        // Party identifiers and names come from the validated request and the entity registry
        string? Value(string? value) => format == TextFormat.Html ? OutputEncoder.Html(value) : value;

        // The references are free text supplied by the client
        string? Reference(string? value) => format switch
        {
            TextFormat.Html => OutputEncoder.Html(value),
            TextFormat.Markdown => OutputEncoder.Markdown(value),
            _ => value
        };

        input = !string.IsNullOrEmpty(acc.Requestor) ? input.Replace(TextMacros.Requestor, Value(acc.Requestor), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(requestorName) ? input.Replace(TextMacros.RequestorName, Value(requestorName), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(acc.Subject) ? input.Replace(TextMacros.Subject, Value(acc.Subject), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(subjectName) ? input.Replace(TextMacros.SubjectName, Value(subjectName), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(serviceContextName) ? input.Replace(TextMacros.ServiceContextName, Value(serviceContextName), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(acc.ConsentReference) ? input.Replace(TextMacros.ConsentReference, Reference(acc.ConsentReference), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(acc.ExternalReference) ? input.Replace(TextMacros.ExternalReference, Reference(acc.ExternalReference), StringComparison.InvariantCultureIgnoreCase) : input;
        input = !string.IsNullOrEmpty(buttonText) ? (isAltinn3 ? input.Replace(TextMacros.Button, GetConsentButtonMd(buttonText, consentUrl), StringComparison.InvariantCultureIgnoreCase) : input.Replace(TextMacros.Button, GetConsentButton(buttonText, consentUrl))) : input;
        input = !string.IsNullOrEmpty(acc.ConsentReference) ? (isAltinn3 ? input.Replace(TextMacros.EbevisReference, GetEbevisRefMd(acc.ConsentReference, Reference(acc.ConsentReference)), StringComparison.InvariantCultureIgnoreCase) : input.Replace(TextMacros.EbevisReference, GetEbevisRef(acc.ConsentReference, Reference(acc.ConsentReference)), StringComparison.InvariantCultureIgnoreCase)) : input;

        input = input.Replace(TextMacros.ConsentOrExternalReference, Reference(acc.ConsentReference != "" ? acc.ConsentReference : acc.ExternalReference));
        input = input.Replace(TextMacros.ConsentAndExternalReference, acc.ConsentReference + acc.ExternalReference != "" ? ", " + Reference(acc.ExternalReference) : "");
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
        string? res = accrediationLanguageCode switch
        {
            Constants.LANGUAGE_CODE_ENGLISH => stringTemplates.En,
            Constants.LANGUAGE_CODE_NORWEGIAN_NN => stringTemplates.NoNn,
            _ => stringTemplates.NoNb
        };

        //Indicates we have forgot to define texts for servicecontext
        if (res == null)
            throw new ServiceContextException();

        return res;
    }

    /// <summary>
    /// Gets a link to the notice if the reference is a Doffin or TED reference. Any other reference is free text,
    /// and is returned as <paramref name="encodedReference"/>, which is the reference encoded for the text it goes into.
    /// </summary>
    private static string? GetEbevisRef(string consentReference, string? encodedReference)
    {
        string? caseReferenceBody;

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
            caseReferenceBody = encodedReference;
        }

        return caseReferenceBody;
    }

    /// <summary>
    /// Markdown variant of <see cref="GetEbevisRef"/>
    /// </summary>
    private static string? GetEbevisRefMd(string consentReference, string? encodedReference)
    {
        string? result;

        if (IsDoffinReference(consentReference))
        {
            result = HtmlToMarkdown(string.Format(Settings.GetDoffinLinkTemplate(), consentReference));

           
        }
        else if (IsTedReference(consentReference))
        {
            result = HtmlToMarkdown(string.Format(Settings.GetTedLinkTemplate(), consentReference));           
        }
        else
        {
            result = encodedReference;
        }

        return result;
    }

    public static string HtmlToMarkdown(string input)
    {
        return Regex.Replace(
            input,
            @"<a\s+href\s*=\s*\\""([^\""]+)\\""\s*>(.*?)</a>",
            "[$2]($1)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );
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
