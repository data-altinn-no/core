using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Helpers.LegalBasisValidators;

namespace Dan.Core.Helpers;

/// <summary>
/// The legal basis validator factory.
/// </summary>
public static class LegalBasisValidatorFactory
{
    /// <summary>
    /// Returns a LegalBasisValidator instance based on the provided authorization object
    /// </summary>
    /// <param name="legalBasis">The legal basis</param>
    /// <param name="authorization">
    /// The authorization request.
    /// </param>
    /// <returns>
    /// The <see cref="LegalBasisValidator"/>.
    /// </returns>
    public static LegalBasisValidator Create(LegalBasis? legalBasis, AuthorizationRequest? authorization)
    {
        if (legalBasis == null)
        {
            return new InvalidLegalBasisValidator(authorization, null);
        }

        switch (legalBasis.Type)
        {
            case LegalBasisType.Espd:
                return new Espd(authorization, legalBasis);

            case LegalBasisType.Cpv:
                return new Cpv(authorization, legalBasis);

            default:
                return new InvalidLegalBasisValidator(authorization, legalBasis);
        }
    }
}
