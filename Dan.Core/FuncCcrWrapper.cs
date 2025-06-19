using Dan.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    public class FuncCcrWrapper(IHttpClientFactory httpClientFactory)
    {
        // Old? Unused?
        [Function("FuncPpeProxyCcr"), NoAuthentication]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ppeproxy/enhetsregisteret/api/{unitOrSubunit}/{orgNumber}")] HttpRequestData req,
            ILogger log,
            string unitOrSubunit,
            string orgNumber)
        {
            var httpClient = httpClientFactory.CreateClient("ppeproxyccr");
            var ccrResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"https://data.ppe.brreg.no/enhetsregisteret/api/{unitOrSubunit}/{orgNumber}"));

            var response = req.CreateResponse();
            response.StatusCode = ccrResponse.StatusCode;
            await response.WriteStringAsync(await ccrResponse.Content.ReadAsStringAsync());
            response.Headers.Add("Content-Type", "application/json");

            return response;
        }
        
        [Function("FuncCcrWrapperSingle"), NoAuthentication]
        public async Task<HttpResponseData> RunCrrWrapperSingle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccrwrapper/enhetsregisteret/api/{unitOrSubunit}/{orgNumber}")] HttpRequestData req,
            ILogger log,
            string unitOrSubunit,
            string orgNumber)
        {
            var httpClient = httpClientFactory.CreateClient("ppeproxyccr");
            var ccrResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{Config.Settings.CcrUrl}/{unitOrSubunit}/{orgNumber}"));

            var response = req.CreateResponse();
            response.StatusCode = ccrResponse.StatusCode;
            await response.WriteStringAsync(await ccrResponse.Content.ReadAsStringAsync());
            response.Headers.Add("Content-Type", "application/json");

            return response;
        }
        
        [Function("FuncCcrWrapperList"), NoAuthentication]
        public async Task<HttpResponseData> RunCrrWrapperList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ccrwrapper/enhetsregisteret/api/{unitOrSubunit}")] HttpRequestData req,
            ILogger log,
            string unitOrSubunit)
        {
            var httpClient = httpClientFactory.CreateClient("ppeproxyccr");
            var ccrResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{Config.Settings.CcrUrl}/{unitOrSubunit}"));

            var response = req.CreateResponse();
            response.StatusCode = ccrResponse.StatusCode;
            await response.WriteStringAsync(await ccrResponse.Content.ReadAsStringAsync());
            response.Headers.Add("Content-Type", "application/json");

            return response;
        }
    }
}
