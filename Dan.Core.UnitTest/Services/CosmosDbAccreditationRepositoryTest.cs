using System.Diagnostics.CodeAnalysis;
using System.Net;
using Dan.Common.Models;
using Dan.Core.Helpers;
using Dan.Core.Services;
using FakeItEasy;
using Microsoft.Azure.Cosmos;

namespace Dan.Core.UnitTest.Services;

[TestClass]
[ExcludeFromCodeCoverage]
public class CosmosDbAccreditationRepositoryTest
{
    private const string Owner = "991825827";
    private const string ETag = "\"00000000-0000-0000-0000-000000000001\"";

    private readonly Container _container = A.Fake<Container>();
    private CosmosDbAccreditationRepository _repository;

    [TestInitialize]
    public void Initialize()
    {
        // Not part of appsettings.unittest.json; only needed for looking up the (faked) container
        ConfigurationHelper.ConfigurationRoot["CosmosDbDatabase"] = "unit-test";

        var client = A.Fake<CosmosClient>();
        A.CallTo(() => client.GetContainer(A<string>._, A<string>._)).Returns(_container);
        _repository = new CosmosDbAccreditationRepository(client);
    }

    [TestMethod]
    public async Task GetAccreditationWithVersionAsync_ReturnsTheAccreditationWithTheETagItWasReadWith()
    {
        var stored = GetAccreditation();
        var response = A.Fake<ItemResponse<Accreditation>>();
        A.CallTo(() => response.Resource).Returns(stored);
        A.CallTo(() => response.ETag).Returns(ETag);
        A.CallTo(() => _container.ReadItemAsync<Accreditation>(stored.AccreditationId, new PartitionKey(Owner), A<ItemRequestOptions>._, A<CancellationToken>._))
            .Returns(Task.FromResult(response));

        var versioned = await _repository.GetAccreditationWithVersionAsync(stored.AccreditationId, Owner);

        Assert.IsNotNull(versioned);
        Assert.AreSame(stored, versioned.Accreditation);
        Assert.AreEqual(ETag, versioned.ETag);
    }

    [TestMethod]
    public async Task GetAccreditationWithVersionAsync_ReturnsNull_WhenTheAccreditationDoesNotExist()
    {
        A.CallTo(() => _container.ReadItemAsync<Accreditation>(A<string>._, A<PartitionKey>._, A<ItemRequestOptions>._, A<CancellationToken>._))
            .Throws(CosmosError(HttpStatusCode.NotFound));

        var versioned = await _repository.GetAccreditationWithVersionAsync("missing", Owner);

        Assert.IsNull(versioned);
    }

    [TestMethod]
    public async Task TryUpdateAccreditationAsync_ReplacesOnlyIfTheETagStillMatches()
    {
        var accreditation = GetAccreditation();
        ItemRequestOptions usedOptions = null;
        A.CallTo(() => _container.ReplaceItemAsync(A<Accreditation>._, accreditation.AccreditationId, new PartitionKey(Owner), A<ItemRequestOptions>._, A<CancellationToken>._))
            .Invokes(call => usedOptions = call.GetArgument<ItemRequestOptions>(3))
            .Returns(Task.FromResult(A.Fake<ItemResponse<Accreditation>>()));

        var updated = await _repository.TryUpdateAccreditationAsync(accreditation, ETag);

        Assert.IsTrue(updated);
        Assert.IsNotNull(usedOptions);
        Assert.AreEqual(ETag, usedOptions.IfMatchEtag);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.PreconditionFailed)]
    [DataRow(HttpStatusCode.NotFound)]
    public async Task TryUpdateAccreditationAsync_ReturnsFalse_WhenTheAccreditationWasChangedOrDeletedSinceItWasRead(HttpStatusCode statusCode)
    {
        A.CallTo(() => _container.ReplaceItemAsync(A<Accreditation>._, A<string>._, A<PartitionKey?>._, A<ItemRequestOptions>._, A<CancellationToken>._))
            .Throws(CosmosError(statusCode));

        var updated = await _repository.TryUpdateAccreditationAsync(GetAccreditation(), ETag);

        Assert.IsFalse(updated);
    }

    [TestMethod]
    public async Task TryUpdateAccreditationAsync_Throws_WhenCosmosFailsForAnotherReason()
    {
        A.CallTo(() => _container.ReplaceItemAsync(A<Accreditation>._, A<string>._, A<PartitionKey?>._, A<ItemRequestOptions>._, A<CancellationToken>._))
            .Throws(CosmosError(HttpStatusCode.ServiceUnavailable));

        var exception = await Assert.ThrowsAsync<CosmosException>(() => _repository.TryUpdateAccreditationAsync(GetAccreditation(), ETag));

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    private static CosmosException CosmosError(HttpStatusCode statusCode)
    {
        return new CosmosException("error from Cosmos", statusCode, 0, "activity-id", 1);
    }

    private static Accreditation GetAccreditation()
    {
        return new Accreditation
        {
            AccreditationId = Guid.NewGuid().ToString(),
            Owner = Owner,
            Subject = "910402021",
            Requestor = "958935420",
            SubjectParty = new Party { NorwegianOrganizationNumber = "910402021" },
            RequestorParty = new Party { NorwegianOrganizationNumber = "958935420" },
            ValidTo = DateTime.Now.AddDays(1),
            EvidenceCodes = new List<EvidenceCode> { new() { EvidenceCodeName = "test", ServiceContext = "eBevis" } }
        };
    }
}
