using System.Net;
using Dan.Common.Interfaces;
using Dan.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core;

public class FuncTestingTemp
{
    private readonly IEntityRegistryService _registryService;

    public FuncTestingTemp(IEntityRegistryService registryService)
    {
        _registryService = registryService;
        _registryService.UseCoreProxy = false;
    }
    
    [Function("TestingSubUnits"), NoAuthentication]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "testing/api/subunits/{orgNumber}")]
        HttpRequestData req,
        ILogger log,
        string orgNumber)
    {
        var x = await _registryService.GetSubunitList(orgNumber);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(x);
        return response;
    }
}