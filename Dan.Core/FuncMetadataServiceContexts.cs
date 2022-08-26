using Dan.Core.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Dan.Core.Attributes;
using Dan.Core.Helpers;
using Dan.Core.Middleware;
using Dan.Core.Services.Interfaces;

namespace Dan.Core;

public class FuncMetadataServiceContexts
{
    private readonly IServiceContextService _serviceContextService;
    private readonly ILogger<FuncMetadataServiceContexts> _logger;

    public FuncMetadataServiceContexts(IServiceContextService serviceContextService, ILoggerFactory loggerFactory)
    {
        _serviceContextService = serviceContextService;
        _logger = loggerFactory.CreateLogger<FuncMetadataServiceContexts>();
    }

    /// <summary>
    /// Entry point for the Azure function
    /// </summary>
    /// <param name="req">
    /// The HTTP request object
    /// </param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    [Function("MetadataServiceContext"), NoAuthentication]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/servicecontexts")]
        HttpRequestData req)
    {
        using (_logger.Timer("get-servicecontext-meta"))
        {
            var serviceContexts = await _serviceContextService.GetRegisteredServiceContexts();

            var response = req.CreateExternalResponse(HttpStatusCode.OK, serviceContexts.Select(a => a.Name).ToList());
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            return response;
        }
    }
}