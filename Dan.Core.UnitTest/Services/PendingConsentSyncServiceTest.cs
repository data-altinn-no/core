using System.Diagnostics.CodeAnalysis;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Models;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dan.Core.UnitTest.Services;

[TestClass]
[ExcludeFromCodeCoverage]
public class PendingConsentSyncServiceTest
{
    private const string Owner = "991825827";

    private readonly IAccreditationRepository _repository = A.Fake<IAccreditationRepository>();
    private readonly IAltinn3ConsentService _consentService = A.Fake<IAltinn3ConsentService>();
    private readonly List<Accreditation> _saved = new();

    [TestInitialize]
    public void Initialize()
    {
        A.CallTo(() => _repository.UpdateAccreditationAsync(A<Accreditation>._))
            .Invokes((Accreditation accreditation) => _saved.Add(accreditation))
            .Returns(Task.FromResult(true));
    }

    [TestMethod]
    public async Task StoresConsentAsGiven_WhenAltinnReportsAccepted()
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);

        var result = await CreateService().Sync();

        Assert.HasCount(1, _saved);
        Assert.AreEqual(pending.AccreditationId, _saved[0].AccreditationId);
        Assert.AreEqual("ok", _saved[0].Altinn3ConsentStatus);
        Assert.AreEqual(new PendingConsentSyncResult(Checked: 1, Granted: 1, Denied: 0, Failed: 0), result);
    }

    [TestMethod]
    [DataRow(Altinn3ConsentRequestStatus.Rejected)]
    [DataRow(Altinn3ConsentRequestStatus.Revoked)]
    public async Task StoresConsentAsDenied_WhenAltinnReportsRejectedOrRevoked(Altinn3ConsentRequestStatus status)
    {
        GivenPending(status);

        var result = await CreateService().Sync();

        Assert.HasCount(1, _saved);
        Assert.AreEqual(Altinn3ConsentService.ConsentDenied, _saved[0].Altinn3ConsentStatus);
        Assert.AreEqual(new PendingConsentSyncResult(Checked: 1, Granted: 0, Denied: 1, Failed: 0), result);
    }

    [TestMethod]
    [DataRow(Altinn3ConsentRequestStatus.Created)]
    [DataRow(Altinn3ConsentRequestStatus.Deleted)]
    [DataRow(Altinn3ConsentRequestStatus.Expired)]
    [DataRow(Altinn3ConsentRequestStatus.Unknown)]
    public async Task LeavesTheAccreditationUntouched_WhenAltinnReportsNoAnswer(Altinn3ConsentRequestStatus status)
    {
        GivenPending(status);

        var result = await CreateService().Sync();

        Assert.IsEmpty(_saved);
        Assert.AreEqual(new PendingConsentSyncResult(Checked: 1, Granted: 0, Denied: 0, Failed: 0), result);
    }

    [TestMethod]
    public async Task SetsLastChanged_WhenStoringTheAnswer()
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        pending.LastChanged = DateTime.Now.AddDays(-10);
        var before = DateTime.Now;

        await CreateService().Sync();

        Assert.IsGreaterThanOrEqualTo(before, _saved[0].LastChanged);
    }

    [TestMethod]
    public async Task SavesTheFreshlyReadAccreditation_SoChangesMadeDuringTheRunAreKept()
    {
        // The save replaces the whole document. A reminder written after the list of pending was read must survive.
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        var current = Copy(pending);
        current.Reminders.Add(new NotificationReminder { Success = true, Date = DateTime.Now });
        GivenCurrent(current);

        await CreateService().Sync();

        Assert.HasCount(1, _saved);
        Assert.HasCount(1, _saved[0].Reminders);
        Assert.AreEqual("ok", _saved[0].Altinn3ConsentStatus);
    }

    [TestMethod]
    public async Task DoesNotOverwrite_WhenTheConsentReceiptArrivedDuringTheRun()
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        var current = Copy(pending);
        current.Altinn3ConsentStatus = "OK";
        GivenCurrent(current);

        var result = await CreateService().Sync();

        Assert.IsEmpty(_saved);
        Assert.AreEqual(new PendingConsentSyncResult(Checked: 1, Granted: 0, Denied: 0, Failed: 0), result);
    }

    [TestMethod]
    public async Task DoesNotSave_WhenTheAccreditationWasDeletedDuringTheRun()
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        GivenCurrent(pending.AccreditationId, null);

        await CreateService().Sync();

        Assert.IsEmpty(_saved);
    }

    [TestMethod]
    public async Task ContinuesWithTheNextAccreditation_WhenOneFails()
    {
        var failing = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        var succeeding = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        A.CallTo(() => _consentService.GetConsentRequestStatus(failing.Altinn3ConsentId!))
            .Throws(new ServiceNotAvailableException("Altinn did not return the consent request"));

        var result = await CreateService().Sync();

        Assert.HasCount(1, _saved);
        Assert.AreEqual(succeeding.AccreditationId, _saved[0].AccreditationId);
        Assert.AreEqual(new PendingConsentSyncResult(Checked: 2, Granted: 1, Denied: 0, Failed: 1), result);
    }

    [TestMethod]
    public async Task DoesNotAskAltinn_WhenTheAccreditationHasExpired()
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        pending.ValidTo = DateTime.Now.AddMinutes(-1);

        var result = await CreateService().Sync();

        A.CallTo(() => _consentService.GetConsentRequestStatus(A<string>._)).MustNotHaveHappened();
        Assert.IsEmpty(_saved);
        Assert.AreEqual(new PendingConsentSyncResult(Checked: 0, Granted: 0, Denied: 0, Failed: 0), result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task DoesNotAskAltinn_WhenTheAccreditationHasNoConsentRequest(string consentId)
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Accepted);
        pending.Altinn3ConsentId = consentId;

        await CreateService().Sync();

        A.CallTo(() => _consentService.GetConsentRequestStatus(A<string>._)).MustNotHaveHappened();
        Assert.IsEmpty(_saved);
    }

    [TestMethod]
    [DataRow("ok")]
    [DataRow("denied")]
    public async Task DoesNotAskAltinn_WhenTheConsentIsAlreadyAnswered(string storedStatus)
    {
        var pending = GivenPending(Altinn3ConsentRequestStatus.Rejected);
        pending.Altinn3ConsentStatus = storedStatus;

        await CreateService().Sync();

        A.CallTo(() => _consentService.GetConsentRequestStatus(A<string>._)).MustNotHaveHappened();
        Assert.IsEmpty(_saved);
    }

    private PendingConsentSyncService CreateService()
    {
        A.CallTo(() => _repository.GetAccreditationsPendingConsentAsync()).Returns(Task.FromResult(_pending));
        return new PendingConsentSyncService(_repository, _consentService, new NullLoggerFactory());
    }

    private readonly List<Accreditation> _pending = new();

    /// <summary>
    /// Adds an accreditation waiting for consent, for which Altinn reports the given status. Unless
    /// <see cref="GivenCurrent(Accreditation)"/> says otherwise, reading it again returns an unchanged copy.
    /// </summary>
    private Accreditation GivenPending(Altinn3ConsentRequestStatus statusInAltinn)
    {
        var accreditation = new Accreditation
        {
            AccreditationId = Guid.NewGuid().ToString(),
            Owner = Owner,
            Subject = "910402021",
            Requestor = "958935420",
            SubjectParty = new Party { NorwegianOrganizationNumber = "910402021" },
            RequestorParty = new Party { NorwegianOrganizationNumber = "958935420" },
            ValidTo = DateTime.Now.AddDays(1),
            Altinn3ConsentId = Guid.NewGuid().ToString(),
            Altinn3ConsentStatus = null,
            ServiceContext = "eBevis",
            EvidenceCodes = new List<EvidenceCode> { new() { EvidenceCodeName = "test", ServiceContext = "eBevis" } }
        };

        _pending.Add(accreditation);
        A.CallTo(() => _consentService.GetConsentRequestStatus(accreditation.Altinn3ConsentId))
            .Returns(Task.FromResult(statusInAltinn));
        GivenCurrent(Copy(accreditation));

        return accreditation;
    }

    private void GivenCurrent(Accreditation current)
    {
        GivenCurrent(current.AccreditationId, current);
    }

    private void GivenCurrent(string accreditationId, Accreditation current)
    {
        A.CallTo(() => _repository.GetAccreditationAsync(accreditationId, Owner)).Returns(Task.FromResult(current));
    }

    private static Accreditation Copy(Accreditation accreditation)
    {
        return new Accreditation
        {
            AccreditationId = accreditation.AccreditationId,
            Owner = accreditation.Owner,
            Subject = accreditation.Subject,
            Requestor = accreditation.Requestor,
            SubjectParty = accreditation.SubjectParty,
            RequestorParty = accreditation.RequestorParty,
            ValidTo = accreditation.ValidTo,
            LastChanged = accreditation.LastChanged,
            Altinn3ConsentId = accreditation.Altinn3ConsentId,
            Altinn3ConsentStatus = accreditation.Altinn3ConsentStatus,
            ServiceContext = accreditation.ServiceContext,
            EvidenceCodes = accreditation.EvidenceCodes,
            Reminders = new List<NotificationReminder>(accreditation.Reminders)
        };
    }
}
