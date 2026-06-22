using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Interface for Token Requester
/// </summary>
public interface ITokenRequesterService
{
    Task<string> GetMaskinportenToken(string scopes, string? consumerOrgNo = null);
    Task<string> GetMaskinportenConsentToken(string consentId, string offeredby, EvidenceCode evidenceCodes);

    /// <summary>
    /// Requests a Maskinporten token for the given scopes and exchanges it for an Altinn token,
    /// as required by Altinn platform APIs (e.g. the Notifications API). Returns the same
    /// <c>{ "access_token": ..., "expires_in": ... }</c> JSON shape as <see cref="GetMaskinportenToken"/>,
    /// where <c>access_token</c> is the exchanged Altinn token.
    /// </summary>
    Task<string> GetAltinnExchangedToken(string scopes, string? consumerOrgNo = null);
}
