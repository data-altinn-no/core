using System.Collections.Concurrent;
using Dan.Common.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core.Services.Interfaces;

public interface IRequestContextService
{
    string AuthenticatedOrgNumber { get; set; }
    string SubscriptionKey { get; set; }
    List<string>? Scopes { get; set; }
    ServiceContext ServiceContext { get; set; }
    HttpRequestData? Request { get; set; }
    ConcurrentDictionary<string, string> CustomResponseHeaders { get; set; }
    public Task BuildRequestContext(HttpRequestData request);
    EvidenceHarvesterOptions GetEvidenceHarvesterOptionsFromRequest();
}