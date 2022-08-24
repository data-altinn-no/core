using Dan.Core.Extensions;

namespace Dan.Core.Helpers;

class ExceptionDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        if (request.Options.TryGetValue(HttpExtensions.AllowedStatusCodes, out var allowedStatusCodes) && allowedStatusCodes.Contains(response.StatusCode))
        {
            return response;
        }
        
        var message = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Server returned error status code: {(int)response.StatusCode}, {message}");
    }
}
