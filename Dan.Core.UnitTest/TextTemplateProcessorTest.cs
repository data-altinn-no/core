using System.Diagnostics.CodeAnalysis;
using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Helpers;
using Dan.Core.ServiceContextTexts;

namespace Dan.Core.UnitTest;

[TestClass]
[ExcludeFromCodeCoverage]
public class TextTemplateProcessorTest
{
    private const string HtmlPayload = "<img src=x onerror=alert(document.domain)>";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReceiptTexts_HtmlEncodeConsentReference_EbevisReferenceMacro(bool useAltinn3)
    {
        var texts = Render(new EBevisServiceContextTextTemplate(), consentReference: HtmlPayload, useAltinn3: useAltinn3);

        AssertHtmlPayloadIsEncoded(texts.ConsentGivenReceiptText);
        AssertHtmlPayloadIsEncoded(texts.ConsentDeniedReceiptText);
    }

    [TestMethod]
    public void ReceiptTexts_HtmlEncodeConsentReference_ConsentReferenceMacro()
    {
        var texts = Render(new DrosjeloyveServiceContextTextTemplate(), consentReference: HtmlPayload);

        AssertHtmlPayloadIsEncoded(texts.ConsentGivenReceiptText);
        AssertHtmlPayloadIsEncoded(texts.ConsentDeniedReceiptText);
    }

    [TestMethod]
    public void ReceiptTexts_HtmlEncodePartyNames()
    {
        var texts = Render(new EBevisServiceContextTextTemplate(), requestorName: "Hansen & <b>Sønn</b> AS");

        Assert.Contains("Hansen &amp; &lt;b&gt;Sønn&lt;/b&gt; AS", texts.ConsentGivenReceiptText);
        Assert.DoesNotContain("<b>", texts.ConsentGivenReceiptText);
    }

    [TestMethod]
    public void ReceiptTexts_KeepMarkupFromTemplate()
    {
        var texts = Render(new EBevisServiceContextTextTemplate(), consentReference: HtmlPayload);

        Assert.Contains("</br>", texts.ConsentGivenReceiptText);
    }

    [TestMethod]
    public void CorrespondenceBodyA3_EscapesMarkdownInConsentReference()
    {
        var texts = Render(new EBevisServiceContextTextTemplate(), consentReference: "[Klikk her](https://evil.example) <script>");

        Assert.DoesNotContain("[Klikk her](https://evil.example)", texts.CorrespondenceBodyA3);
        Assert.Contains(@"\[Klikk her\](https://evil.example)", texts.CorrespondenceBodyA3);
        Assert.DoesNotContain("<script>", texts.CorrespondenceBodyA3);
    }

    [TestMethod]
    public void CorrespondenceBodyA3_KeepsOrdinaryReferenceUnchanged()
    {
        var texts = Render(new EBevisServiceContextTextTemplate(), consentReference: "Sak 24/1234 - Rådhuset (del 2)");

        Assert.Contains("Referanse: Sak 24/1234 - Rådhuset (del 2) ", texts.CorrespondenceBodyA3);
    }

    [TestMethod]
    public void PlainTexts_AreNotEncoded()
    {
        var texts = Render(new EBevisServiceContextTextTemplate(), consentReference: "Bygg & anlegg");

        Assert.Contains("Bygg & anlegg", texts.CorrespondenceTitle);
        Assert.Contains("Bygg & anlegg", texts.EmailNotificationContent);
    }

    private static IServiceContextTextTemplate<string> Render(
        IServiceContextTextTemplate<LocalizedString> template,
        string consentReference = "2019-123456",
        string requestorName = "REQUESTOR_NAME",
        bool useAltinn3 = true)
    {
        var context = new ServiceContext { Id = "test", Name = "Test", ServiceContextTextTemplate = template };
        var accreditation = new Accreditation
        {
            AccreditationId = Guid.NewGuid().ToString(),
            Subject = "910402021",
            Requestor = "958935420",
            ConsentReference = consentReference,
            ExternalReference = "externalreference",
            LanguageCode = Constants.LANGUAGE_CODE_NORWEGIAN_NB
        };

        return TextTemplateProcessor.GetRenderedTexts(context, accreditation, requestorName, "SUBJECT_NAME", "https://altinn.example/consent", useAltinn3);
    }

    private static void AssertHtmlPayloadIsEncoded(string text)
    {
        Assert.DoesNotContain("<img", text);
        Assert.Contains("&lt;img src=x onerror=alert(document.domain)&gt;", text);
    }
}
