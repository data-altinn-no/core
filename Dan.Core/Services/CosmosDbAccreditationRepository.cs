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
    private readonly IEvidenceStatusService _evidenceStatusService;
    private readonly PartitionKey _accreditationsPartitionKey;
    private readonly Container _container;

    public CosmosDbAccreditationRepository(CosmosClient cosmosClient, IEvidenceStatusService evidenceStatusService)
    {
        _evidenceStatusService = evidenceStatusService;
        _accreditationsPartitionKey = new PartitionKey(Settings.CosmosbDbAccreditationsPartitionKey);
        _container = cosmosClient.GetContainer(Settings.CosmosDbDatabase, Settings.CosmosDbAccreditations);
    }

    public async Task<Accreditation?> GetAccreditationAsync(string accreditationId, IRequestContextService requestContextService, bool allowExpired = false)
    {
        var queryDefinition =
            new QueryDefinition("SELECT * FROM c WHERE c.Owner = @owner AND c.AccreditationId = @aid AND c.serviceContext = @serviceContext")
                .WithParameter("@owner", requestContextService.AuthenticatedOrgNumber)
                .WithParameter("@aid", accreditationId)
                .WithParameter("@serviceContext", requestContextService.ServiceContext.Name);

        using (var feedIterator = _container.GetItemQueryIterator<Accreditation>(queryDefinition, null,
                   new QueryRequestOptions { PartitionKey = _accreditationsPartitionKey }))
        {
            while (feedIterator.HasMoreResults)
            {
                foreach (var accreditation in await feedIterator.ReadNextAsync())
                {
                    if (!allowExpired && accreditation.ValidTo < DateTime.Now)
                    {
                        throw new ExpiredAccreditationException();
                    }

                    accreditation.PopulateParties();
                    return accreditation;
                }
            }
        }

        return null;
    }

    public async Task<Accreditation?> GetAccreditationAsync(string accreditationId, bool allowExpired = false)
    {
        Accreditation? accreditation = null;
        try
        {
            accreditation =
                await _container.ReadItemAsync<Accreditation>(accreditationId, _accreditationsPartitionKey);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

        if (accreditation == null) return null;

        if (!allowExpired && accreditation.ValidTo < DateTime.Now)
        {
            throw new ExpiredAccreditationException();
        }

        accreditation.PopulateParties();
        return accreditation;
    }

    public async Task<List<Accreditation>> QueryAccreditationsAsync(AccreditationsQuery accreditationsQuery, IRequestContextService requestContextService)
    {
        var queryText = "SELECT * FROM c WHERE c.Owner = @owner AND c.validTo > NOW() AND serviceContext = @serviceContext";
        var parameters = new Dictionary<string, string>
            {
                { "@owner", accreditationsQuery.Owner ?? requestContextService.AuthenticatedOrgNumber },
                { "@serviceContext", accreditationsQuery.ServiceContext ?? requestContextService.ServiceContext.Name }
            };

        if (accreditationsQuery.ChangedAfter.HasValue)
        {
            queryText += " AND lastChanged > @changedAfter";
            parameters.Add("@changedAfter", accreditationsQuery.ChangedAfter.Value.ToString("O"));
        }

        if (accreditationsQuery.Requestor != null)
        {
            queryText += " AND requestor = @requestor";
            parameters.Add("@requestor", accreditationsQuery.Requestor);
        }

        var queryDefinition = new QueryDefinition(queryText);
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.Key, param.Value);
        }

        List<Accreditation> accreditations = new List<Accreditation>();
        using (var feedIterator = _container.GetItemQueryIterator<Accreditation>(queryDefinition, null,
                   new QueryRequestOptions { PartitionKey = _accreditationsPartitionKey }))
        {
            while (feedIterator.HasMoreResults)
            {
                foreach (var accreditation in await feedIterator.ReadNextAsync())
                {
                    accreditations.Add(accreditation);
                }
            }
        }

        await DetermineAggregateStatus(accreditations);

        if (accreditationsQuery.OnlyAvailableForHarvest)
        {
            accreditations = accreditations.Where(x => x.AggregateStatus.Code == EvidenceStatusCode.Available.Code).ToList();
        }

        accreditations.ForEach(x =>
        {
            x.EvidenceCodes = null;
            x.PopulateParties();
        });

        return accreditations;
    }

    public async Task<Accreditation> CreateAccreditationAsync(Accreditation accreditation)
    {
        var result = await _container.CreateItemAsync(accreditation, _accreditationsPartitionKey);
        if (result.StatusCode != HttpStatusCode.Created)
        {
            throw new AccreditationRepositoryException();
        }

        var savedAccreditation = (Accreditation)result;
        savedAccreditation.PopulateParties();
        return savedAccreditation;
    }

    public async Task<bool> UpdateAccreditationAsync(Accreditation accreditation)
    {
        var result = await _container.ReplaceItemAsync(accreditation, accreditation.AccreditationId, _accreditationsPartitionKey);
        return result.StatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> DeleteAccreditationAsync(string accreditationId)
    {
        var result = await _container.DeleteItemAsync<Accreditation>(accreditationId, _accreditationsPartitionKey);
        return result.StatusCode == HttpStatusCode.NoContent;
    }

    /// <summary>
    /// Populates the Aggregate Status property for the accreditations
    /// </summary>
    /// <param name="accreditations">A list of accreditations</param>
    /// <param name="onlyLocalChecks">Whether or not to only do local checks without hitting the network</param>
    /// <returns>An async task</returns>
    private async Task DetermineAggregateStatus(List<Accreditation> accreditations, bool onlyLocalChecks = true)
    {
        foreach (var accreditation in accreditations)
        {
            await DetermineAggregateStatus(accreditation, onlyLocalChecks);
        }
    }

    /// <summary>
    /// Populates the Aggregate Status property for the accreditation
    /// </summary>
    /// <param name="accreditation">A accreditation</param>
    /// <param name="onlyLocalChecks">Whether or not to only do local checks without hitting the network</param>
    /// <returns>An async task</returns>
    private async Task DetermineAggregateStatus(Accreditation accreditation, bool onlyLocalChecks = false)
    {
        foreach (var evidenceCode in accreditation.EvidenceCodes)
        {
            var evidenceStatus = await _evidenceStatusService.GetEvidenceStatusAsync(accreditation, evidenceCode, onlyLocalChecks);
            if (evidenceStatus.Status.Code == EvidenceStatusCode.Available.Code) continue;
            accreditation.AggregateStatus = evidenceStatus.Status;
            return;
        }

        accreditation.AggregateStatus = EvidenceStatusCode.Available;
    }

}