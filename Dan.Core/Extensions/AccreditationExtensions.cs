using Dan.Common.Helpers.Util;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;

namespace Dan.Core.Extensions;

/// <summary>
/// Accreditation extensions
/// </summary>
public static class AccreditationExtensions
{
    /// <summary>
    /// Get an EvidenceCode from an Accreditation
    /// </summary>
    /// <param name="accreditation">The Accreditation</param>
    /// <param name="evidenceCodeName">The name of the evidence code</param>
    /// <returns>An EvidenceCode</returns>
    public static EvidenceCode GetValidEvidenceCode(this Accreditation accreditation, string evidenceCodeName)
    {
        var evidenceCode = accreditation.EvidenceCodes.FirstOrDefault(x => string.Equals(x.EvidenceCodeName, evidenceCodeName, StringComparison.InvariantCultureIgnoreCase));

        if (evidenceCode == null)
        {
            throw new InvalidEvidenceRequestException();
        }

        return evidenceCode;
    }

    /// <summary>
    /// Get the HMAC for an accreditation
    /// </summary>
    /// <param name="accreditation">The accreditation</param>
    /// <param name="secret">Optional secret to override settings</param>
    /// <returns>The HMAC as a Base64 encoded string</returns>
    public static string GetHmac(this Accreditation accreditation, string? secret = null)
    {
        secret ??= Settings.ConsentValidationSecrets.First();

        var encoding = new System.Text.ASCIIEncoding();
        var hmac = new System.Security.Cryptography.HMACSHA256(encoding.GetBytes(secret));

        return BitConverter.ToString(hmac.ComputeHash(encoding.GetBytes(accreditation.AccreditationId))).Replace("-", String.Empty);
    }

    /// <summary>
    /// Get an url for an accreditation
    /// </summary>
    /// <param name="accreditation">The accreditation</param>
    /// <returns>The url</returns>
    public static string GetUrl(this Accreditation accreditation)
    {
        return string.Format(Settings.AccreditationCreatedLocationPattern, accreditation.AccreditationId);
    }

    /// <summary>
    /// Populates the SubjectParty and RequestorParty properties if not set (true for legacy accreditations)
    /// </summary>
    /// <param name="accreditation"></param>
    public static void PopulateParties(this Accreditation accreditation)
    {
        if (accreditation.SubjectParty.Id != null && accreditation.RequestorParty.Id != null)
        {
            return;
        }

        var subjectParty = PartyParser.GetPartyFromIdentifier(accreditation.Subject, out var error);
        if (subjectParty == null)
        {
            throw new InvalidSubjectException($"Invalid subject supplied: {error}");
        }

        var requestorParty = PartyParser.GetPartyFromIdentifier(accreditation.Requestor, out error);
        if (requestorParty == null)
        {
            throw new InvalidRequestorException($"Invalid requestor supplied: {error}");
        }


        accreditation.SubjectParty = subjectParty;
        accreditation.RequestorParty = requestorParty;
    }
}
