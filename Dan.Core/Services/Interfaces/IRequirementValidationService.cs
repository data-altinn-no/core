using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IRequirementValidationService
{
    /// <summary>
    /// Create a new RequirementValidationHelper instance
    /// </summary>
    /// <returns>The list of found request validation errors</returns>
    Task<List<string>> ValidateRequirements(Dictionary<string, List<Requirement>> evidenceCodeRequirements, AuthorizationRequest? authorizationRequest = default);

    /// <summary>
    /// Returns a list of any skipped evidence codes due to soft requirements
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, Requirement> GetSkippedEvidenceCodes();
}