using Dan.Common.Models;

namespace Dan.Core.Helpers.LegalBasisValidators;

/// <summary>
/// Validator class for ESPD (European Single Procurement Document) XML documents.
/// </summary>
public class InvalidLegalBasisValidator : LegalBasisValidator
{
    /// <inheritdoc />
    public InvalidLegalBasisValidator(AuthorizationRequest? authorizationRequest, LegalBasis? legalBasis) : base(authorizationRequest, legalBasis)
    {
    }

    /// <summary>
    /// Returns whether or not 
    /// </summary>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public override bool IsLegalBasisValid()
    {
        return false;
    }
}