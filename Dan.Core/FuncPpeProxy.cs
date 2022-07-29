using Dan.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    public class FuncPpeProxy
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FuncPpeProxy(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Function("FuncPpeProxyCcr"), NoAuthentication]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ppeproxy/enhetsregisteret/api/{unitOrSubunit}/{orgNumber}")] HttpRequestData req,
            ILogger log,
            string unitOrSubunit,
            string orgNumber)
        {
            var httpClient = _httpClientFactory.CreateClient("ppeproxyccr");
            var ccrResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"https://data.ppe.brreg.no/enhetsregisteret/api/{unitOrSubunit}/{orgNumber}"));

            var response = req.CreateResponse();
            response.StatusCode = ccrResponse.StatusCode;
            await response.WriteStringAsync(await ccrResponse.Content.ReadAsStringAsync());
            response.Headers.Add("Content-Type", "application/json");

            return response;
        }
    }
}
