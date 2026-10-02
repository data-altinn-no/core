using Dan.Common;
using Dan.Common.Models;
using Dan.Core.Services.Interfaces;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Dan.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class FuncConsentEventsTest
    {
        private readonly IAccreditationRepository _mockRepository = A.Fake<IAccreditationRepository>();
        private readonly IConsentEventPublisher _mockPublisher = A.Fake<IConsentEventPublisher>();
        private readonly ILoggerFactory _mockLoggerFactory = A.Fake<ILoggerFactory>();

        private FuncConsentEvents CreateFunction() => new(_mockRepository, _mockPublisher, _mockLoggerFactory);

        private static Accreditation GetAccreditation() => new()
        {
            AccreditationId = Guid.NewGuid().ToString(),
            Owner = "910402021",
            Altinn3ConsentId = Guid.NewGuid().ToString(),
            ValidTo = DateTime.Now.AddDays(-1)
        };

        [TestInitialize]
        public void Initialize()
        {
            A.CallTo(() => _mockPublisher.IsEnabled).Returns(true);
            A.CallTo(() => _mockRepository.GetAccreditationsWithUnpublishedConsentEventsAsync(A<DateTime>._, A<int>._))
                .Returns(Task.FromResult(new List<Accreditation>()));
            A.CallTo(() => _mockRepository.GetAccreditationsWithExpiredPendingConsentAsync(A<DateTime>._, A<DateTime>._, A<string>._))
                .Returns(Task.FromResult(new List<Accreditation>()));
        }

        [TestMethod]
        public async Task Process_WhenDisabled_DoesNothing()
        {
            A.CallTo(() => _mockPublisher.IsEnabled).Returns(false);

            var updated = await CreateFunction().ProcessAsync();

            Assert.AreEqual(0, updated);
            A.CallTo(() => _mockRepository.GetAccreditationsWithUnpublishedConsentEventsAsync(A<DateTime>._, A<int>._)).MustNotHaveHappened();
            A.CallTo(() => _mockRepository.GetAccreditationsWithExpiredPendingConsentAsync(A<DateTime>._, A<DateTime>._, A<string>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task Process_RetriesUnpublished_PersistsOnlyChanged()
        {
            var changedAccreditation = GetAccreditation();
            var unchangedAccreditation = GetAccreditation();
            A.CallTo(() => _mockRepository.GetAccreditationsWithUnpublishedConsentEventsAsync(A<DateTime>._, A<int>._))
                .Returns(Task.FromResult(new List<Accreditation> { changedAccreditation, unchangedAccreditation }));
            A.CallTo(() => _mockPublisher.TryPublishPending(changedAccreditation)).Returns(Task.FromResult(true));
            A.CallTo(() => _mockPublisher.TryPublishPending(unchangedAccreditation)).Returns(Task.FromResult(false));

            var updated = await CreateFunction().ProcessAsync();

            Assert.AreEqual(1, updated);
            A.CallTo(() => _mockRepository.UpdateAccreditationAsync(changedAccreditation)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockRepository.UpdateAccreditationAsync(unchangedAccreditation)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task Process_ExpiredPendingConsent_EnqueuesExpiredEventAndPublishes()
        {
            var expired = GetAccreditation();
            A.CallTo(() => _mockRepository.GetAccreditationsWithExpiredPendingConsentAsync(A<DateTime>._, A<DateTime>._, ConsentEventTypes.Expired))
                .Returns(Task.FromResult(new List<Accreditation> { expired }));
            A.CallTo(() => _mockPublisher.Enqueue(expired, ConsentEventTypes.Expired)).Returns(true);
            A.CallTo(() => _mockPublisher.TryPublishPending(expired)).Returns(Task.FromResult(true));

            var updated = await CreateFunction().ProcessAsync();

            Assert.AreEqual(1, updated);
            A.CallTo(() => _mockPublisher.Enqueue(expired, ConsentEventTypes.Expired)).MustHaveHappenedOnceExactly();
            // Once to persist the outbox record before publishing, once after the publish attempt changed it
            A.CallTo(() => _mockRepository.UpdateAccreditationAsync(expired)).MustHaveHappenedTwiceExactly();
        }

        [TestMethod]
        public async Task Process_ExpiredAlreadyRecorded_IsSkipped()
        {
            var expired = GetAccreditation();
            A.CallTo(() => _mockRepository.GetAccreditationsWithExpiredPendingConsentAsync(A<DateTime>._, A<DateTime>._, A<string>._))
                .Returns(Task.FromResult(new List<Accreditation> { expired }));
            A.CallTo(() => _mockPublisher.Enqueue(expired, ConsentEventTypes.Expired)).Returns(false);

            var updated = await CreateFunction().ProcessAsync();

            Assert.AreEqual(0, updated);
            A.CallTo(() => _mockRepository.UpdateAccreditationAsync(A<Accreditation>._)).MustNotHaveHappened();
            A.CallTo(() => _mockPublisher.TryPublishPending(A<Accreditation>._)).MustNotHaveHappened();
        }
    }
}
