using Dan.Common.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core.Services.Interfaces;

public interface IRequestContextService
{
    string AuthenticatedOrgNumber { get; set; }
    string SubscriptionKey { get; set; }
    List<string>? Scopes { get; set; }
    ServiceContext ServiceContext { get; set; }
    HttpRequestData Request { get; set; }
    Task BuildRequestContext(HttpRequestData request);
}