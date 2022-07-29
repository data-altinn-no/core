namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Interface for Token Requester
/// </summary>
public interface ITokenRequesterService
{
    Task<string> GetMaskinportenToken(string scopes, string? consumerOrgNo = null);
}
