using Dan.Common.Models;

namespace Dan.Core.Helpers.LegalBasisValidators;

/// <summary>
/// Validator class for ESPD (European Single Procurement Document) XML documents.
/// </summary>
public class Espd : LegalBasisValidator
{
    public Espd(AuthorizationRequest? authorizationRequest, LegalBasis? legalBasis) : base(authorizationRequest, legalBasis)
    {
    }

    /// <summary>
    /// Returns whether or not the supplied legal basis is valid or not. This is a no-op, as we have no way of validating a ESPD as per now.
    /// </summary>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public override bool IsLegalBasisValid()
    {
        if (AuthorizationRequest != null)
        {
            return true;
        }

        return false;
    }
}