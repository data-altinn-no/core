using Dan.Common;
using Dan.Common.Models;
using Dan.Core.Models.Events;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ConsentEventPublisherTest
    {
        private const string SubjectSsn = "08075412345";

        private readonly IAltinnEventsService _mockAltinnEventsService = A.Fake<IAltinnEventsService>();
        private readonly ILogger<ConsentEventPublisher> _mockLogger = A.Fake<ILogger<ConsentEventPublisher>>();

        private ConsentEventPublisher CreatePublisher() => new(_mockAltinnEventsService, _mockLogger);

        private static Accreditation GetAccreditation() => new()
        {
            AccreditationId = Guid.NewGuid().ToString(),
            Owner = "910402021",
            Requestor = "910402021",
            Subject = SubjectSsn,
            SubjectParty = new Party { NorwegianSocialSecurityNumber = SubjectSsn },
            RequestorParty = new Party { NorwegianOrganizationNumber = "910402021" },
            ServiceContext = "eBevis",
            ConsentReference = "ref-1",
            ExternalReference = "ext-1",
            ValidTo = DateTime.Now.AddDays(10)
        };

        [TestMethod]
        public void IsEnabled_ReflectsUnitTestSettings()
        {
            Assert.IsTrue(CreatePublisher().IsEnabled);
        }

        [TestMethod]
        public void Enqueue_AddsRecordOnceForSameType()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();

            Assert.IsTrue(publisher.Enqueue(accreditation, ConsentEventTypes.Granted));
            Assert.IsFalse(publisher.Enqueue(accreditation, ConsentEventTypes.Granted));

            Assert.AreEqual(1, accreditation.ConsentEvents.Count);
            var record = accreditation.ConsentEvents[0];
            Assert.AreEqual(ConsentEventTypes.Granted, record.Type);
            Assert.IsFalse(record.Published);
            Assert.AreEqual(0, record.Attempts);
            Assert.AreEqual(ConsentEventRecord.CreateEventId(accreditation.AccreditationId, ConsentEventTypes.Granted), record.EventId);
        }

        [TestMethod]
        public void Enqueue_DifferentTypes_AddsSeparateRecords()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();

            publisher.Enqueue(accreditation, ConsentEventTypes.Granted);
            publisher.Enqueue(accreditation, ConsentEventTypes.Expired);

            Assert.AreEqual(2, accreditation.ConsentEvents.Count);
            Assert.AreNotEqual(accreditation.ConsentEvents[0].EventId, accreditation.ConsentEvents[1].EventId);
        }

        [TestMethod]
        public void CreateEventId_IsDeterministicAndGuidFormatted()
        {
            var a = ConsentEventRecord.CreateEventId("aid-1", ConsentEventTypes.Granted);
            var b = ConsentEventRecord.CreateEventId("aid-1", ConsentEventTypes.Granted);
            var c = ConsentEventRecord.CreateEventId("aid-2", ConsentEventTypes.Granted);

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.IsTrue(Guid.TryParse(a, out _));
        }

        [TestMethod]
        public void BuildEvent_UsesOwnerAsSubject_AndContainsNoPersonalData()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();
            publisher.Enqueue(accreditation, ConsentEventTypes.Granted);
            var record = accreditation.ConsentEvents[0];

            var cloudEvent = publisher.BuildEvent(accreditation, record);

            Assert.AreEqual(record.EventId, cloudEvent.Id);
            Assert.AreEqual("1.0", cloudEvent.SpecVersion);
            Assert.AreEqual(ConsentEventTypes.Granted, cloudEvent.Type);
            Assert.AreEqual("https://test.data.altinn.no/v1/consent", cloudEvent.Source);
            Assert.AreEqual("urn:altinn:organization:identifier-no:910402021", cloudEvent.Subject);
            Assert.AreEqual("urn:altinn:resource:digdir-data-altinn-no-consent", cloudEvent.Resource);
            Assert.AreEqual(accreditation.AccreditationId, cloudEvent.ResourceInstance);
            Assert.AreEqual(DateTimeKind.Utc, cloudEvent.Time.Kind);

            var data = cloudEvent.Data as ConsentEventData;
            Assert.IsNotNull(data);
            Assert.AreEqual(accreditation.AccreditationId, data!.AccreditationId);
            Assert.AreEqual("granted", data.Status);
            Assert.AreEqual("eBevis", data.ServiceContext);
            Assert.AreEqual("ref-1", data.ConsentReference);
            Assert.AreEqual("ext-1", data.ExternalReference);
            Assert.AreEqual(accreditation.ValidTo, data.ValidTo);

            var json = JsonConvert.SerializeObject(cloudEvent);
            StringAssert.DoesNotMatch(json, new System.Text.RegularExpressions.Regex(SubjectSsn));
            StringAssert.DoesNotMatch(json, new System.Text.RegularExpressions.Regex("requestor", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }

        [TestMethod]
        public void BuildEvent_StatusFollowsEventType()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();

            var denied = publisher.BuildEvent(accreditation, new ConsentEventRecord { EventId = "x", Type = ConsentEventTypes.Denied, Created = DateTime.UtcNow });
            var expired = publisher.BuildEvent(accreditation, new ConsentEventRecord { EventId = "y", Type = ConsentEventTypes.Expired, Created = DateTime.UtcNow });

            Assert.AreEqual("denied", ((ConsentEventData)denied.Data!).Status);
            Assert.AreEqual("expired", ((ConsentEventData)expired.Data!).Status);
        }

        [TestMethod]
        public async Task TryPublishPending_Success_MarksPublished()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();
            publisher.Enqueue(accreditation, ConsentEventTypes.Granted);

            var changed = await publisher.TryPublishPending(accreditation);

            Assert.IsTrue(changed);
            var record = accreditation.ConsentEvents[0];
            Assert.IsTrue(record.Published);
            Assert.IsNotNull(record.PublishedAt);
            Assert.AreEqual(1, record.Attempts);
            Assert.IsNull(record.LastError);
            A.CallTo(() => _mockAltinnEventsService.Publish(A<AltinnCloudEvent>.That.Matches(e => e.Id == record.EventId), record.EventId))
                .MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        public async Task TryPublishPending_Failure_RecordsErrorAndDoesNotThrow()
        {
            A.CallTo(() => _mockAltinnEventsService.Publish(A<AltinnCloudEvent>._, A<string>._))
                .Throws(new Exception("events down"));
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();
            publisher.Enqueue(accreditation, ConsentEventTypes.Denied);

            var changed = await publisher.TryPublishPending(accreditation);

            Assert.IsTrue(changed);
            var record = accreditation.ConsentEvents[0];
            Assert.IsFalse(record.Published);
            Assert.AreEqual(1, record.Attempts);
            Assert.IsNotNull(record.LastAttempt);
            Assert.AreEqual("events down", record.LastError);
        }

        [TestMethod]
        public async Task TryPublishPending_SkipsPublishedRecords()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();
            accreditation.ConsentEvents.Add(new ConsentEventRecord { EventId = "x", Type = ConsentEventTypes.Granted, Published = true, Attempts = 1 });

            var changed = await publisher.TryPublishPending(accreditation);

            Assert.IsFalse(changed);
            A.CallTo(() => _mockAltinnEventsService.Publish(A<AltinnCloudEvent>._, A<string>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task TryPublishPending_SkipsRecordsAtMaxAttempts()
        {
            // ConsentEventsMaxAttempts is 3 in appsettings.unittest.json
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();
            accreditation.ConsentEvents.Add(new ConsentEventRecord { EventId = "x", Type = ConsentEventTypes.Granted, Attempts = 3 });

            var changed = await publisher.TryPublishPending(accreditation);

            Assert.IsFalse(changed);
            A.CallTo(() => _mockAltinnEventsService.Publish(A<AltinnCloudEvent>._, A<string>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task TryPublishPending_MissingOwner_SkipsWithError()
        {
            var publisher = CreatePublisher();
            var accreditation = GetAccreditation();
            accreditation.Owner = null;
            publisher.Enqueue(accreditation, ConsentEventTypes.Granted);

            var changed = await publisher.TryPublishPending(accreditation);

            Assert.IsTrue(changed);
            var record = accreditation.ConsentEvents[0];
            Assert.IsFalse(record.Published);
            Assert.AreEqual(1, record.Attempts);
            Assert.IsNotNull(record.LastError);
            A.CallTo(() => _mockAltinnEventsService.Publish(A<AltinnCloudEvent>._, A<string>._)).MustNotHaveHappened();
        }
    }
}
