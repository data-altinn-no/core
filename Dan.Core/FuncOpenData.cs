using System.Net;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Services;
using Dan.Core.Attributes;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    /// <summary>
    /// The Azure Function being the proxy of calls to the various evidence sources
    /// </summary>
    public class FuncOpenData
    {
        private readonly IEvidenceHarvesterService _evidenceHarvesterService;
        private readonly IEntityRegistryService _entityRegistryService;
        private readonly IAvailableEvidenceCodesService _availableEvidenceCodesService;
        private readonly ILogger<FuncOpenData> _logger;

        public FuncOpenData(IAvailableEvidenceCodesService availableEvidenceCodesService, IEvidenceHarvesterService evidenceHarvesterService, IEntityRegistryService entityRegistryService, ILoggerFactory loggerFactory)
        {
            _availableEvidenceCodesService = availableEvidenceCodesService;
            _evidenceHarvesterService = evidenceHarvesterService;
            _entityRegistryService = entityRegistryService;

            _logger = loggerFactory.CreateLogger<FuncOpenData>();
        }

        [Function("FuncOpenData"), NoAuthentication]
        public async Task<HttpResponseData> RunOpenDataset(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "opendata/{datasetname}")] HttpRequestData req, string datasetName)
        {
            return await GetOpenData(datasetName, req);
        }

        [Function("FuncOpenDataIdentifier"), NoAuthentication]
        public async Task<HttpResponseData> RunOpenDatasetIdentifier(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "opendata/{datasetname}/{identifier}")] HttpRequestData req, string datasetName, string identifier)
        {
            if (IsCcrProxyRequest(datasetName))
            {
                return await HandleCcrProxyRequest(req, datasetName, identifier);
            }

            return await GetOpenData(datasetName, req, identifier);
        }

        private async Task<HttpResponseData> GetOpenData(string datasetName, HttpRequestData req, string identifier = "")
        {
            var evidencecode = await ValidateDatasetName(datasetName);
            var evidence = await _evidenceHarvesterService.HarvestOpenData(evidencecode, identifier);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.SetUnenvelopedEvidenceValuesAsync(evidence.EvidenceValues, req.GetQueryParam(JmesPathTransfomer.QueryParameter));

            _logger.DanLog(identifier, datasetName, "OpenData", LogAction.OpenDatasetRetrieved);

            return response;
        }

        private async Task<EvidenceCode> ValidateDatasetName(string datasetName)
        {
            var evidenceCodes = await _availableEvidenceCodesService.GetAvailableEvidenceCodes();
            var evidenceCode = evidenceCodes.FirstOrDefault(x => x.EvidenceCodeName.Equals(datasetName, StringComparison.InvariantCultureIgnoreCase) && x.IsPublic);

            if (evidenceCode == null)
                throw new InvalidEvidenceRequestException("Dataset is not openly available or does not exist");

            return evidenceCode;
        }

        private async Task<HttpResponseData> HandleCcrProxyRequest(HttpRequestData req, string unitTypeDatasetName, string organizationNumber)
        {
            EntityRegistryUnit? unit;
            if (unitTypeDatasetName == EntityRegistryService.CcrProxyMainUnitDatasetName)
            {
                unit = await _entityRegistryService.GetFull(organizationNumber, attemptSubUnitLookupIfNotFound: false);
            }
            else
            {
                unit = await _entityRegistryService.GetFull(organizationNumber, subUnitOnly: true);
            }

            if (unit == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(unit);

            return response;
        }

        private bool IsCcrProxyRequest(string datasetName)
        {
            return datasetName is EntityRegistryService.CcrProxyMainUnitDatasetName or EntityRegistryService.CcrProxySubUnitDatasetName;
        }
    }
}
