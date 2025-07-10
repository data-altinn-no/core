using System.Net;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    public class FuncCcrWrapper
    {
        private readonly IEntityRegistryService entityRegistryService;

        public FuncCcrWrapper(IEntityRegistryService entityRegistryService)
        {
            this.entityRegistryService = entityRegistryService;
            this.entityRegistryService.UseCoreProxy = false;
        }
        
        // Attempts to first find main unit on orgnumber, then subunit if no main unit found
        [Function("FuncCcrOrgnumberLookup"), NoAuthentication]
        public async Task<HttpResponseData> RunCcrOrgnumberLookup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccr/{orgNumber}")] HttpRequestData req,
            ILogger log,
            string orgNumber)
        {
            var unit = await entityRegistryService.GetFull(orgNumber, attemptSubUnitLookupIfNotFound: true);
            
            if (unit == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(unit);

            return response;
        }
        
        [Function("FuncCcrSubUnits"), NoAuthentication]
        public async Task<HttpResponseData> RunCcrSubUnits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccr/{orgNumber}/subunits")] HttpRequestData req,
            ILogger log,
            string orgNumber)
        {
            var units = await entityRegistryService.GetSubunits(orgNumber);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(units);

            return response;
        }
        
        [Function("FuncCcrMainUnit"), NoAuthentication]
        public async Task<HttpResponseData> RunCcrMainUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccr/{orgNumber}/mainunit")] HttpRequestData req,
            ILogger log,
            string orgNumber)
        {
            var unit = await entityRegistryService.GetFullMainUnit(orgNumber);
            
            if (unit == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(unit);

            return response;
        }
        
        [Function("FuncCcrIsPublic"), NoAuthentication]
        public async Task<HttpResponseData> RunCcrIsPublic(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccr/{orgNumber}/ispublic")] HttpRequestData req,
            ILogger log,
            string orgNumber)
        {
            var isPublic = await entityRegistryService.IsPublicAgency(orgNumber);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(isPublic);

            return response;
        }
        
        [Function("FuncCcrHierarchy"), NoAuthentication]
        public async Task<HttpResponseData> RunCcrHierarchy(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccr/{orgNumber}/hierarchy")] HttpRequestData req,
            ILogger log,
            string orgNumber)
        {
            var hierarchy = await entityRegistryService.GetSubunitHierarchy(orgNumber);
            if (hierarchy == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(hierarchy);

            return response;
        }
    }
}
