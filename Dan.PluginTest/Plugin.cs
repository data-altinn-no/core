using Dan.Common.Exceptions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Services;
using Dan.Common.Util;
using Dan.PluginTest.Config;
using Dan.PluginTest.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dan.PluginTest;

// ReSharper disable once ClassNeverInstantiated.Global
public class Plugin(
    ILoggerFactory loggerFactory,
    IEvidenceSourceMetadata evidenceSourceMetadata,
    IDanPluginClientService danPluginClientService,
    ICcrClientService ccrClientService,
    IOptions<Settings> settings)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Plugin>();
    private readonly Settings _settings = settings.Value;
    
    // Used to test aliases
    [Function(PluginConstants.DatasetOne)]
    public async Task<HttpResponseData> DatasetOne(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        EvidenceHarvesterRequest? evidenceHarvesterRequest;
        try
        {
            evidenceHarvesterRequest = await req.ReadFromJsonAsync<EvidenceHarvesterRequest>();
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Exception while attempting to parse request into EvidenceHarvesterRequest: {exceptionType}: {exceptionMessage}",
                e.GetType().Name, e.Message);
            throw new EvidenceSourcePermanentClientException(PluginConstants.ErrorInvalidInput,
                "Unable to parse request", e);
        }

        return await EvidenceSourceResponse.CreateResponse(req,
            () => GetEvidenceValuesDatasetOne(evidenceHarvesterRequest));
    }
    
    // Used to test aliases
    [Function(PluginConstants.DatasetTwo)]
    public async Task<HttpResponseData> DatasetTwo(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        EvidenceHarvesterRequest? evidenceHarvesterRequest;
        try
        {
            evidenceHarvesterRequest = await req.ReadFromJsonAsync<EvidenceHarvesterRequest>();
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Exception while attempting to parse request into EvidenceHarvesterRequest: {exceptionType}: {exceptionMessage}",
                e.GetType().Name, e.Message);
            throw new EvidenceSourcePermanentClientException(PluginConstants.ErrorInvalidInput,
                "Unable to parse request", e);
        }

        return await EvidenceSourceResponse.CreateResponse(req,
            () => GetEvidenceValuesDatasetTwo(evidenceHarvesterRequest));
    }
    
    // Used to test IDanPluginClientService for plugin to plugic calls
    [Function(PluginConstants.PluginForward)]
    public async Task<HttpResponseData> PluginForward(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        EvidenceHarvesterRequest? evidenceHarvesterRequest;
        try
        {
            evidenceHarvesterRequest = await req.ReadFromJsonAsync<EvidenceHarvesterRequest>();
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Exception while attempting to parse request into EvidenceHarvesterRequest: {exceptionType}: {exceptionMessage}",
                e.GetType().Name, e.Message);
            throw new EvidenceSourcePermanentClientException(PluginConstants.ErrorInvalidInput,
                "Unable to parse request", e);
        }

        return await EvidenceSourceResponse.CreateResponse(req,
            () => FetchPluginValue(evidenceHarvesterRequest));
    }
    
    // Used to test app settings - particularly getting settings from keyvault
    [Function(PluginConstants.PluginSettingsTest)]
    public async Task<HttpResponseData> PluginSettingsTest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        EvidenceHarvesterRequest? evidenceHarvesterRequest;
        try
        {
            evidenceHarvesterRequest = await req.ReadFromJsonAsync<EvidenceHarvesterRequest>();
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Exception while attempting to parse request into EvidenceHarvesterRequest: {exceptionType}: {exceptionMessage}",
                e.GetType().Name, e.Message);
            throw new EvidenceSourcePermanentClientException(PluginConstants.ErrorInvalidInput,
                "Unable to parse request", e);
        }

        return await EvidenceSourceResponse.CreateResponse(req,
            () => EvidenceValuesPluginSettings(evidenceHarvesterRequest));
    }
    
    [Function(PluginConstants.PluginGenericTest)]
    public async Task<HttpResponseData> PluginGenericTest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        EvidenceHarvesterRequest? evidenceHarvesterRequest;
        try
        {
            evidenceHarvesterRequest = await req.ReadFromJsonAsync<EvidenceHarvesterRequest>();
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Exception while attempting to parse request into EvidenceHarvesterRequest: {exceptionType}: {exceptionMessage}",
                e.GetType().Name, e.Message);
            throw new EvidenceSourcePermanentClientException(PluginConstants.ErrorInvalidInput,
                "Unable to parse request", e);
        }

        var unit = await ccrClientService.IsPublic("923609016", "local");
        
        return await EvidenceSourceResponse.CreateResponse(req,
            () => GetEvidenceValuesDatasetOne(evidenceHarvesterRequest));
    }
    
    private async Task<List<EvidenceValue>> GetEvidenceValuesDatasetOne(
        EvidenceHarvesterRequest? evidenceHarvesterRequest)
    {
        var response = new DatasetResponse{Test = "DatasetOne"};
        var ecb = new EvidenceBuilder(evidenceSourceMetadata, PluginConstants.DatasetOne);
        ecb.AddEvidenceValue("default", response, PluginConstants.Source);

        await Task.CompletedTask;
        return ecb.GetEvidenceValues();
    }
    
    private async Task<List<EvidenceValue>> GetEvidenceValuesDatasetTwo(
        EvidenceHarvesterRequest? evidenceHarvesterRequest)
    {
        var response = new DatasetResponse{Test = "DatasetTwo"};
        var ecb = new EvidenceBuilder(evidenceSourceMetadata, PluginConstants.DatasetTwo);
        ecb.AddEvidenceValue("default", response, PluginConstants.Source);

        await Task.CompletedTask;
        return ecb.GetEvidenceValues();
    }
    
    private async Task<List<EvidenceValue>> FetchPluginValue(
        EvidenceHarvesterRequest? evidenceHarvesterRequest)
    {
        var enovaRequest = new EvidenceHarvesterRequest
        {
            EvidenceCodeName = "OffentligEnergiData",
            OrganizationNumber = "931001434"
        };
        
        var response = await danPluginClientService.GetPluginDataSetAsync<dynamic>(
            request: enovaRequest,
            code: _settings.PluginCode,
            env: "dev",
            isDefaultJson: true,
            source: "enova");
        var ecb = new EvidenceBuilder(evidenceSourceMetadata, PluginConstants.DatasetOne);
        ecb.AddEvidenceValue("default", response, PluginConstants.Source);

        return ecb.GetEvidenceValues();
    }
    
    private async Task<List<EvidenceValue>> EvidenceValuesPluginSettings(
        EvidenceHarvesterRequest? evidenceHarvesterRequest)
    {
        var ecb = new EvidenceBuilder(evidenceSourceMetadata, PluginConstants.PluginSettingsTest);
        var certFetched = !string.IsNullOrWhiteSpace(_settings.Certificate);
        ecb.AddEvidenceValue("certSuccessfullyFetched", certFetched, PluginConstants.Source);

        await Task.CompletedTask;
        return ecb.GetEvidenceValues();
    }
}