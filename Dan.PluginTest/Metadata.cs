using System.Net;
using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.PluginTest.Config;
using Dan.PluginTest.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NJsonSchema;

namespace Dan.PluginTest;

public class Metadata : IEvidenceSourceMetadata
{
    private const string DanTest = "DAN-test";
    private const string AltinnStudioApps = "Altinn Studio-apps";
    private readonly List<string> _serviceContexts = [DanTest,AltinnStudioApps];

    [Function(Constants.EvidenceSourceMetadataFunctionName)]
    public async Task<HttpResponseData> GetMetadataAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(GetEvidenceCodes());
        return response;
    }

    public List<EvidenceCode> GetEvidenceCodes()
    {
        return
        [
            new EvidenceCode
            {
                EvidenceCodeName = PluginConstants.DatasetOne,
                EvidenceSource = PluginConstants.Source,
                BelongsToServiceContexts = _serviceContexts,
                DatasetAliases = new List<DatasetAlias>()
                {
                    new (){ ServiceContext = DanTest, DatasetAliasName = "AliasOne" },
                    new (){ ServiceContext = AltinnStudioApps, DatasetAliasName = "AliasTwo" },
                },
                Values =
                [
                    new EvidenceValue
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        JsonSchemaDefintion = JsonSchema
                            .FromType<DatasetResponse>()
                            .ToJson(Newtonsoft.Json.Formatting.Indented)
                    }
                ]
            },
            new EvidenceCode
            {
                EvidenceCodeName = PluginConstants.DatasetTwo,
                EvidenceSource = PluginConstants.Source,
                ServiceContext = DanTest,
                Values =
                [
                    new EvidenceValue
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        JsonSchemaDefintion = JsonSchema
                            .FromType<DatasetResponse>()
                            .ToJson(Newtonsoft.Json.Formatting.Indented)
                    }
                ]
            },
            new EvidenceCode
            {
                EvidenceCodeName = PluginConstants.PluginForward,
                EvidenceSource = PluginConstants.Source,
                ServiceContext = DanTest,
                Values =
                [
                    new EvidenceValue
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema
                    }
                ]
            }
        ];
    }
}