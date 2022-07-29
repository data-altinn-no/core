using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAuthorizationRequestValidatorService
{
    /// <summary>
    /// Performs validation of the authorization request. First performs simple validations of parties, 
    /// then all requirements specified on the evidencecodes in the authrequest
    /// </summary>
    /// <returns>Async task</returns>
    Task Validate(AuthorizationRequest? authorizationRequest);

    /// <summary>
    /// Get the validated authorization request
    /// </summary>
    /// <returns>An authorization request</returns>
    AuthorizationRequest? GetAuthorizationRequest();

    /// <summary>
    /// Get all the connected evidence codes with any supplied parameters
    /// </summary>
    /// <returns>A list of evidence codes</returns>
    List<EvidenceCode> GetEvidenceCodes();

    /// <summary>
    /// Returns a list of any skipped evidence codes due to soft requirements
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, Requirement> GetSkippedEvidenceCodes();

    /// <summary>
    /// Returns the effective valid to date taking into account limitations in evidence codes. Assumes validation has been performed.
    /// </summary>
    /// <returns>The date and time to which the accreditation is valid</returns>
    DateTime GetValidTo();
}