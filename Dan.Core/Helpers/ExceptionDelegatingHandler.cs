using System.Net;

namespace Dan.Core.Helpers;

class ExceptionDelegatingHandler : DelegatingHandler
{
    public const string ALLOWEDSTATUSCODES = "AllowedStatusCodes";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {

        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var allowedStatusCodes = new List<HttpStatusCode>();

        if (request.Properties.ContainsKey(ALLOWEDSTATUSCODES))
        {
            allowedStatusCodes = request.Properties[ALLOWEDSTATUSCODES] as List<HttpStatusCode>;
        }

        if (allowedStatusCodes != null && allowedStatusCodes.Contains(response.StatusCode))
        {
            return response;
        }
        else
        {
            var message = response.Content == null ? response.ReasonPhrase : await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Server returned error status code: {(int)response.StatusCode}, {message}");
        }
    }
}
