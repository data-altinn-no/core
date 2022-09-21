using System.Net;
using System.Security.Cryptography;
using Dan.Core.Attributes;
using Dan.Core.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core;

public class FuncMetadataSigningCertificate
{
    [Function("MetadataSigningCertificate"), NoAuthentication]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Settings.SigningCertificateEndpoint)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        var pemChars = PemEncoding.Write("CERTIFICATE", Settings.AltinnCertificate.GetRawCertData());
        await response.WriteStringAsync(new string(pemChars));

        return response;
    }
}