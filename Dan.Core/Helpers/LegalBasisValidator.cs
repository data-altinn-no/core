using Dan.Common.Models;

namespace Dan.Core.Helpers;

/// <summary>
/// Abstract class defining the interface for legal basis validators.
/// </summary>
public abstract class LegalBasisValidator
{
    /// <summary>
    /// The authorization request
    /// </summary>
    protected readonly AuthorizationRequest? AuthorizationRequest;
    protected readonly LegalBasis? LegalBasis;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalBasisValidator"/> class.
    /// </summary>
    /// <param name="authorizationRequest">
    /// The authorization request.
    /// </param>
    protected LegalBasisValidator(AuthorizationRequest? authorizationRequest, LegalBasis? legalBasis)
    {
        AuthorizationRequest = authorizationRequest;
        LegalBasis = legalBasis;
    }

    /// <summary>
    /// Returns whether or not 
    /// </summary>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public virtual bool IsLegalBasisValid()
    {
        return false;
    }
}