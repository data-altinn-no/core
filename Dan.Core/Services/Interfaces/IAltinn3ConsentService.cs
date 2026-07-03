using Dan.Common.Enums;
using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAltinn3ConsentService
{
    /// <summary>
    /// Initiating a consent based on the passed (validated) accreditation
    /// </summary>
    /// <param name="accreditation">
    /// The accreditation.
    /// </param>
    /// <param name="skipAltinnNotification"></param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    Task Initiate(Accreditation accreditation, bool skipAltinnNotification);

    /// <summary>
    /// The check.
    /// </summary>
    /// <param name="accreditation">
    /// The accreditation.
    /// </param>
    /// <param name="onlyLocalCheck">
    /// Whether or not to skip the call to Altinn API to get the token to check for revocation/expiration
    /// </param>
    /// <returns>
    /// The <see cref="ConsentStatus"/>.
    /// </returns>
    Task<ConsentStatus> Check(Accreditation accreditation, bool onlyLocalCheck = false);

    /// <summary>
    /// Uses Maskinporten to get a consent token (JWT) for the consent in the accreditation
    /// </summary>
    /// <param name="accreditation">Accreditation containing a consent id</param>
    /// <param name="evidenceCode">Evidence code requiring consent</param>
    /// <returns>A JWT</returns>
    Task<string> GetJwt(Accreditation accreditation, EvidenceCode evidenceCode);

    /// <summary>
    /// Logs the use of a consent
    /// </summary>
    /// <param name="accreditation">Accreditation containing a consent id</param>
    /// <param name="evidence">The evidenceCode</param>
    /// <param name="dateTime">The dateTime when usage was done</param>
    /// <returns>Success status</returns>
    Task<bool> LogUse(Accreditation accreditation, EvidenceCode evidence, DateTime? dateTime = null);

    /// <summary>
    /// Whether the suplied evidenceCode requires consent in active service context
    /// </summary>
    /// <param name="evidenceCode"></param>
    /// <returns></returns>
    bool EvidenceCodeRequiresConsent(EvidenceCode evidenceCode);

    /// <summary>
    /// Returns a list of evidence codes requiring consent for given accreditation
    /// </summary>
    /// <param name="accreditation"></param>
    /// <returns></returns>
    List<EvidenceCode> GetEvidenceCodesRequiringConsentForActiveContext(Accreditation accreditation);
}
