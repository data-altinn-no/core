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

        // A macro without a value is not added here, and is left as it is in the text
        var replacements = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(acc.Requestor)) replacements[TextMacros.Requestor] = Value(acc.Requestor);
        if (!string.IsNullOrEmpty(requestorName)) replacements[TextMacros.RequestorName] = Value(requestorName);
        if (!string.IsNullOrEmpty(acc.Subject)) replacements[TextMacros.Subject] = Value(acc.Subject);
        if (!string.IsNullOrEmpty(subjectName)) replacements[TextMacros.SubjectName] = Value(subjectName);
        if (!string.IsNullOrEmpty(serviceContextName)) replacements[TextMacros.ServiceContextName] = Value(serviceContextName);
        if (!string.IsNullOrEmpty(acc.ExternalReference)) replacements[TextMacros.ExternalReference] = Reference(acc.ExternalReference);

        if (!string.IsNullOrEmpty(acc.ConsentReference))
        {
            var encodedReference = Reference(acc.ConsentReference);
            replacements[TextMacros.ConsentReference] = encodedReference;
            replacements[TextMacros.EbevisReference] = isAltinn3
                ? GetEbevisRefMd(acc.ConsentReference, encodedReference)
                : GetEbevisRef(acc.ConsentReference, encodedReference);
        }

        if (!string.IsNullOrEmpty(buttonText))
        {
            replacements[TextMacros.Button] = isAltinn3 ? GetConsentButtonMd(buttonText, consentUrl) : GetConsentButton(buttonText, consentUrl);
        }

        replacements[TextMacros.ConsentOrExternalReference] = Reference(acc.ConsentReference != "" ? acc.ConsentReference : acc.ExternalReference);
        replacements[TextMacros.ConsentAndExternalReference] = acc.ConsentReference + acc.ExternalReference != "" ? ", " + Reference(acc.ExternalReference) : "";

        // The template is scanned once, so text that looks like a macro inside a value is never expanded
        return MacroPattern.Replace(input, match => replacements.TryGetValue(match.Value, out var replacement) ? replacement ?? string.Empty : match.Value);
    }

    /// <summary>
    /// Matches the macros that <see cref="ProcessMacros"/> replaces
    /// </summary>
    private static readonly Regex MacroPattern = new(
        string.Join("|", new[]
        {
            TextMacros.Requestor,
            TextMacros.RequestorName,
            TextMacros.Subject,
            TextMacros.SubjectName,
            TextMacros.ServiceContextName,
            TextMacros.ConsentReference,
            TextMacros.ExternalReference,
            TextMacros.Button,
            TextMacros.EbevisReference,
            TextMacros.ConsentOrExternalReference,
            TextMacros.ConsentAndExternalReference
        }.Select(Regex.Escape)),
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
