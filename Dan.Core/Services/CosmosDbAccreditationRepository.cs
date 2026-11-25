using System.Net;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Cosmos;

namespace Dan.Core.Services;
public class CosmosDbAccreditationRepository : IAccreditationRepository
{
    private readonly Container _container;

    public CosmosDbAccreditationRepository(CosmosClient cosmosClient)
    {
        _container = cosmosClient.GetContainer(Settings.CosmosDbDatabase, Settings.CosmosDbAccreditations);
    }

    public async Task<Accreditation?> GetAccreditationAsync(string accreditationId, string? partitionKeyValue)
    {
        Accreditation? accreditation = null;

        // We assume using ReadItemAsync is faster than a query iterator when we have a partition key value.
        // When null, call QueryAccreditationsAsync which uses a GetItemQueryIterator that supports cross-partition queries
        if (partitionKeyValue != null)
        {
            try
            {
                accreditation =
                    await _container.ReadItemAsync<Accreditation>(accreditationId, new PartitionKey(partitionKeyValue));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
        }
        else
        {
            var accreditations =
                await QueryAccreditationsAsync(new AccreditationsQuery { AccreditationId = accreditationId }, partitionKeyValue);

            if (accreditations.Count == 0) return null;
            accreditation = accreditations[0];
        }

        if (accreditation == null) return null;

        accreditation.PopulateParties();
        return accreditation;
    }

    public async Task<List<Accreditation>> QueryAccreditationsAsync(AccreditationsQuery accreditationsQuery, string? partitionKeyValue)
    {
        var queryText = "SELECT * FROM c WHERE 1=1 ";
        var parameters = new Dictionary<string, string>();

        if (accreditationsQuery.AccreditationId != null)
        {
            queryText += " AND c.id = @accreditationId";
            parameters.Add("@accreditationId", accreditationsQuery.AccreditationId);
        }

        if (accreditationsQuery.ServiceContext != null)
        {
            queryText += " AND c.serviceContext = @serviceContext";
            parameters.Add("@serviceContext", accreditationsQuery.ServiceContext);
        }

        if (accreditationsQuery.ChangedAfter.HasValue)
        {
            queryText += " AND c.lastChanged > @changedAfter";
            parameters.Add("@changedAfter", accreditationsQuery.ChangedAfter.Value.ToString("O"));
        }

        if (accreditationsQuery.Requestor != null)
        {
            queryText += " AND c.requestor = @requestor";
            parameters.Add("@requestor", accreditationsQuery.Requestor);
        }

        var queryDefinition = new QueryDefinition(queryText);
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.Key, param.Value);
        }

        var queryOptions = partitionKeyValue != null
            ? new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKeyValue) }
            : null;

        return await ExecuteQueryAsync(queryDefinition, queryOptions);
    }

    public async Task<List<Accreditation>> GetAccreditationsPendingConsentAsync()
    {
        // "Pending" is not a stored status; it means the consent request has no stored answer. The stored timestamps do
        // not all carry the same UTC offset, and are compared as strings, hence the margin of a day on validTo.
        var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.validTo > @validAfter" +
                " AND IS_STRING(c.altinn3ConsentId) AND c.altinn3ConsentId != ''" +
                " AND (NOT IS_DEFINED(c.altinn3ConsentStatus) OR IS_NULL(c.altinn3ConsentStatus) OR c.altinn3ConsentStatus = '')")
            .WithParameter("@validAfter", DateTime.UtcNow.AddDays(-1).ToString("O"));

        // No partition key, as this spans all owners. This will cause a cross partition query.
        return await ExecuteQueryAsync(queryDefinition, null);
    }

    private async Task<List<Accreditation>> ExecuteQueryAsync(QueryDefinition queryDefinition, QueryRequestOptions? queryOptions)
    {
        var accreditations = new List<Accreditation>();
        using (var feedIterator = _container.GetItemQueryIterator<Accreditation>(queryDefinition, null, queryOptions))
        {
            while (feedIterator.HasMoreResults)
            {
                foreach (var accreditation in await feedIterator.ReadNextAsync())
                {
                    accreditations.Add(accreditation);
                }
            }
        }

        // TODO! This is only requires for legacy accreditations. Should be removed when all legacy accreditations have expired from the database.
        accreditations.ForEach(x =>
        {
            x.PopulateParties();
        });

        return accreditations;
    }

    public async Task<Accreditation> CreateAccreditationAsync(Accreditation accreditation)
    {
        // Remove requirements before saving, these need to be repopulated
        var accreditationToSave = GetAccredidationWithoutRequirements(accreditation);

        var result = await _container.CreateItemAsync(accreditationToSave, new PartitionKey(accreditationToSave.Owner));
        var savedAccreditation = (Accreditation)result;

        // TODO! This is only requires for legacy accreditations. Should be removed when all legacy accreditations have expired from the database.
        savedAccreditation.PopulateParties();
        
        return savedAccreditation;
    }

    public async Task<bool> UpdateAccreditationAsync(Accreditation accreditation)
    {
        // Remove requirements before saving, these need to be repopulated
        var accreditationToSave = GetAccredidationWithoutRequirements(accreditation);

        var result = await _container.ReplaceItemAsync(accreditationToSave, accreditationToSave.AccreditationId, new PartitionKey(accreditationToSave.Owner));
        return result.StatusCode == HttpStatusCode.OK;
    }

    public async Task<VersionedAccreditation?> GetAccreditationWithVersionAsync(string accreditationId, string partitionKeyValue)
    {
        try
        {
            var response = await _container.ReadItemAsync<Accreditation>(accreditationId, new PartitionKey(partitionKeyValue));
            var accreditation = response.Resource;
            accreditation.PopulateParties();

            return new VersionedAccreditation(accreditation, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> TryUpdateAccreditationAsync(Accreditation accreditation, string etag)
    {
        // Remove requirements before saving, these need to be repopulated
        var accreditationToSave = GetAccredidationWithoutRequirements(accreditation);

        try
        {
            await _container.ReplaceItemAsync(accreditationToSave, accreditationToSave.AccreditationId,
                new PartitionKey(accreditationToSave.Owner), new ItemRequestOptions { IfMatchEtag = etag });
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.NotFound)
        {
            // Changed (412) or deleted (404) by someone else since it was read
            return false;
        }
    }

    public async Task<bool> DeleteAccreditationAsync(Accreditation accreditation)
    {
        var result = await _container.DeleteItemAsync<Accreditation>(accreditation.AccreditationId, new PartitionKey(accreditation.Owner));
        return result.StatusCode == HttpStatusCode.NoContent;
    }

    private Accreditation GetAccredidationWithoutRequirements(Accreditation accreditation)
    {
        var newAccreditation = accreditation.DeepCopy();
        foreach (var evidenceCode in newAccreditation.EvidenceCodes)
        {
            evidenceCode.AuthorizationRequirements = new List<Requirement>();
        }

        return newAccreditation;
    }
}