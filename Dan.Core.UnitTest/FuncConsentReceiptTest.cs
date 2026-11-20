using System.Diagnostics.CodeAnalysis;
using System.Net;
using Dan.Common;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.ServiceContextTexts;
using Dan.Core.Services.Interfaces;
using Dan.Core.UnitTest.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dan.Core.UnitTest;

[TestClass]
[ExcludeFromCodeCoverage]
public class FuncConsentReceiptTest
{
    private const string HtmlPayload = "<img src=x onerror=alert(document.domain)>";

    private readonly IEntityRegistryService _mockEntityRegistryService = A.Fake<IEntityRegistryService>();
    private readonly IServiceContextService _mockServiceContextService = A.Fake<IServiceContextService>();
    private readonly IAccreditationRepository _mockAccreditationRepository = A.Fake<IAccreditationRepository>();

    [TestInitialize]
    public void Initialize()
    {
        // Not part of appsettings.unittest.json; only needed for the link back to Altinn on the receipt page
        ConfigurationHelper.ConfigurationRoot["AltinnPortalAddress"] = "https://altinn.example/";

        A.CallTo(() => _mockEntityRegistryService.Get(A<string>._, A<bool>._, A<bool>._))
            .Returns(Task.FromResult(new SimpleEntityRegistryUnit { Name = "Test Organization" }));

        A.CallTo(() => _mockServiceContextService.GetRegisteredServiceContexts())
            .Returns(Task.FromResult(new List<ServiceContext>
            {
                new() { Id = "ebevis-product", Name = "eBevis", ServiceContextTextTemplate = new EBevisServiceContextTextTemplate() }
            }));
    }

    [TestMethod]
    [DataRow("OK")]
    [DataRow("rejected")]
    public async Task Run_HtmlEncodesConsentReferenceInReceipt(string status)
    {
        var accreditation = GetAccreditation(consentReference: HtmlPayload);
        A.CallTo(() => _mockAccreditationRepository.GetAccreditationAsync(accreditation.AccreditationId, null))
            .Returns(Task.FromResult(accreditation));

        var func = new FuncConsentReceipt(_mockEntityRegistryService, _mockServiceContextService, _mockAccreditationRepository, new NullLoggerFactory());
        var request = TestHelpers.CreateHttpRequestData(
            $"https://localhost/api/consent/{accreditation.AccreditationId}?hmac={accreditation.GetHmac()}&status={status}");

        var response = await func.Run(request, accreditation.AccreditationId);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = TestHelpers.ReadBody(response);
        Assert.DoesNotContain("<img", body);
        Assert.Contains("&lt;img src=x onerror=alert(document.domain)&gt;", body);
    }

    [TestMethod]
    public async Task Run_KeepsMarkupFromTemplateInReceipt()
    {
        var accreditation = GetAccreditation(consentReference: "Sak 24/1234");
        A.CallTo(() => _mockAccreditationRepository.GetAccreditationAsync(accreditation.AccreditationId, null))
            .Returns(Task.FromResult(accreditation));

        var func = new FuncConsentReceipt(_mockEntityRegistryService, _mockServiceContextService, _mockAccreditationRepository, new NullLoggerFactory());
        var request = TestHelpers.CreateHttpRequestData(
            $"https://localhost/api/consent/{accreditation.AccreditationId}?hmac={accreditation.GetHmac()}&status=OK");

        var response = await func.Run(request, accreditation.AccreditationId);

        var body = TestHelpers.ReadBody(response);
        Assert.Contains("er registrert som gitt. </br>Informasjonen", body);
        Assert.Contains("i forbindelse med Sak 24/1234 på tjenesten eBevis", body);
        Assert.Contains("<a href=\"https://docs.data.altinn.no/\">docs.data.altinn.no</a>", body);
        Assert.Contains("<a href=\"https://altinn.example/ui/messagebox\" class=\"receipt-button\">Tilbake til meldingsboksen</a>", body);
    }

    [TestMethod]
    public async Task Run_ShowsDeniedReceiptWithLinkBackToAltinn()
    {
        var accreditation = GetAccreditation(consentReference: "Sak 24/1234");
        A.CallTo(() => _mockAccreditationRepository.GetAccreditationAsync(accreditation.AccreditationId, null))
            .Returns(Task.FromResult(accreditation));

        var func = new FuncConsentReceipt(_mockEntityRegistryService, _mockServiceContextService, _mockAccreditationRepository, new NullLoggerFactory());
        var request = TestHelpers.CreateHttpRequestData(
            $"https://localhost/api/consent/{accreditation.AccreditationId}?hmac={accreditation.GetHmac()}&status=rejected");

        var response = await func.Run(request, accreditation.AccreditationId);

        var body = TestHelpers.ReadBody(response);
        Assert.Contains("er registrert som avslått. </br>Dataene", body);
        Assert.Contains("<a href=\"https://altinn.example/ui/messagebox\" class=\"receipt-button\">Gå tilbake til Altinn</a>", body);
        Assert.Contains("<h1 class=\"receipt-title\">Fullmakt</h1>", body);
    }

    private static Accreditation GetAccreditation(string consentReference)
    {
        return new Accreditation
        {
            AccreditationId = Guid.NewGuid().ToString(),
            Subject = "910402021",
            Requestor = "958935420",
            SubjectParty = new Party { NorwegianOrganizationNumber = "910402021" },
            RequestorParty = new Party { NorwegianOrganizationNumber = "958935420" },
            ValidTo = DateTime.Now.AddDays(1),
            ConsentReference = consentReference,
            LanguageCode = Constants.LANGUAGE_CODE_NORWEGIAN_NB,
            Altinn3ConsentId = "test-consent-id-123",
            ServiceContext = "eBevis",
            EvidenceCodes = new List<EvidenceCode> { new() { EvidenceCodeName = "test", ServiceContext = "eBevis" } }
        };
    }
}
