using Dan.Common.Models;

namespace Dan.Core.Helpers.LegalBasisValidators;

public class Cpv : LegalBasisValidator
{
    private static readonly string[] CpvCodesForRiskyAquirements =
    {
        "45000000-7", // Bygge- og anleggsvirksomhet
        "45100000-8", // Forberedende anleggsarbeid
        "45200000-9", // Deler av eller komplette byggekonstruksjoner samt anleggsarbeider
        "45300000-0", // Byggningsinnstallasjonsarbeid
        "45400000-1", // Ferdigstillende bygningsarbeid
        "45500000-2", // Utleie av entreprenørmateriell og -utstyr med operatør
        "50000000-5", // Reperasjons- og vedlikeholdstjenester
        "51000000-9", // Installasjonstjenester (bortsett fra programvare)
        "55000000-0", // Tjenester relatert til Hotell- og restaurantvirksomhet, Detaljhandel
        "55100000-1", // Hotelltjenester
        "55200000-2", // Campingplasser og annen losji unntatt hotell
        "55300000-3", // Restaurant og serveringstjenester
        "55400000-4", // Servering av drikkevarer
        "55500000-5", // Kantine- og cateringvirksomhet
        "55900000-9", // Detaljhandel
        "60000000-8", // Transporttjenester (bortsett fra avfallstransport)
        "60100000-9", // Veitransporttjenester
        "60112000-6", // Transport på offentlig vei
        "90000000-7", // Avløp, søppel, sanitære og miljømessige tjenester
        "90610000-6", // Renholds- og feiingstjenester for gater
        "90900000-6", // Renholds- og saniteringstjenester
        "90911000-6", // Renholdstjenester for lokaler, bygninger og vinduer
        "90913000-0", // Renholdstjenester for tank og reservoar
        "90919000-2", // Renholdstjenester for kontor, skole og kontorutstyr
    };

    public Cpv(AuthorizationRequest? authorizationRequest, LegalBasis? legalBasis) : base(authorizationRequest, legalBasis)
    {
    }

    /// <summary>
    /// Returns whether or not the supplied comma separated CPV-IDs in legal basis is in the static list of CPV-codes
    /// This requirements is used in conjunction with evidencecodes for "utvidet skatteattest" with a soft-requirement
    /// </summary>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public override bool IsLegalBasisValid()
    {
        if (LegalBasis?.Content == null) return false;

        // Using both comma and space as separators allows for whitespace before/after the comma due to RemoveEmptyEntries,
        // but assumes that no valid CPV-code will ever contain a space
        var suppliedCpvIds = LegalBasis.Content.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return CpvCodesForRiskyAquirements.Intersect(suppliedCpvIds).Any();
    }
}
