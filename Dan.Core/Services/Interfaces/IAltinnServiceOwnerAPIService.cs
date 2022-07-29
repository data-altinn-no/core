using Dan.Common.Models;
using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAltinnServiceOwnerApiService
{
    /// <summary>
    /// Validate an Altinn role delegation between two parties
    /// </summary>                
    /// <param name="offeredby">
    /// The ssn/orgno of the delegator of the role
    /// </param>
    /// <param name="coveredby">
    /// The ssn/orgno of the receiver of the role
    /// </param>
    /// <param name="roleCode">
    /// The name of the Altinn role
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    Task<bool> VerifyAltinnRole(string offeredby, string coveredby, string roleCode);

    /// <summary>
    /// Verify that coveredby has rights to the service on behalf of offeredby
    /// </summary>                
    /// <param name="offeredby">
    /// The ssn/orgno of the delegator of the role
    /// </param>
    /// <param name="coveredby">
    /// The ssn/orgno of the receiver of the role
    /// </param>
    /// <param name="serviceCode">
    /// The service code identifier of an Altinn service
    /// </param>
    /// <param name="serviceEdition">
    /// The service edition identifier of an Altinn service
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    Task<bool> VerifyAltinnRight(string offeredby, string coveredby, string serviceCode, string serviceEdition);

    /// <summary>
    /// Makes sure a SRR right exists for the reportee and handledby for all required services
    /// </summary>
    /// <param name="requestor">The reportee in SRR</param>
    /// <param name="validTo">How long the rule should be valid</param>
    /// <returns></returns>
    Task EnsureSrrRights(string requestor, DateTime validTo, IEnumerable<EvidenceCode> evidenceCodesRequiringConsent);

    /// <summary>
    /// Looks up a organization by its organization number
    /// </summary>
    /// <param name="orgNumber">Organization number</param>
    /// <returns>The organization</returns>
    Task<Organization> GetOrganization(string orgNumber);
}