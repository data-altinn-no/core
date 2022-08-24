namespace Dan.Common.Helpers.Util;

public class PartyParser
{
    public const string SchemeIso6523ActorIdUpis = "iso6523-actorid-upis";
    public const string SchemeNorwegianSsn = "nor-freg";
    public const string NorwegianIcd = "0192";

    public static readonly string[] Schemes =
    {
        SchemeIso6523ActorIdUpis,
        SchemeNorwegianSsn
    };


    public static Party? GetPartyFromIdentifier(string? partyIdentifier, out string? error)
    {
        error = null;
        if (partyIdentifier == null)
        {
            error = "Party identifier was null";
            return null;
        }

        if (!partyIdentifier.Contains("::"))
        {
            var party = ParseNativeNorwegianOrgNoOrSsn(partyIdentifier);
            if (party == null)
                error = "Supplied non-structured identifier failed to validate as a norwegian organization number or SSN";

            return party;
        }

        var schemeParts = partyIdentifier.Split("::");

        if (!Schemes.Contains(schemeParts[0]))
        {
            error =
                $"Unknown scheme supplied for structured identifier. Expected one of: {string.Join(", ", Schemes)}";
            return null;
        }

        return schemeParts[0] switch
        {
            SchemeIso6523ActorIdUpis => GetPartyFromIso6523ActorIdUpis(schemeParts[1], out error),
            SchemeNorwegianSsn => GetPartyFromNorwegianSsn(schemeParts[1], out error),
            // Shouldn't get here, if we do we're not handling all the defined schemes
            _ => null
        };
    }


    private static Party? GetPartyFromIso6523ActorIdUpis(string id, out string? error)
    {
        var idParts = id.Split(":");
        if (idParts.Length != 2)
        {
            error = "Unable to parse ISO5623 identifier, expected \"<icd>:<national identifier>\"";
            return null;
        }

        if (idParts[0] == NorwegianIcd)
        {
            return GetPartyFromNorwegianOrganizationNumber(idParts[1], out error);
        }

        error = null;
        return new Party
        {
            Scheme = SchemeIso6523ActorIdUpis,
            Id = id
        };
    }

    private static Party? ParseNativeNorwegianOrgNoOrSsn(string partyIdentifier)
    {
        var party = GetPartyFromNorwegianOrganizationNumber(partyIdentifier, out _);
        return party ?? GetPartyFromNorwegianSsn(partyIdentifier, out _);
    }

    private static Party? GetPartyFromNorwegianOrganizationNumber(string organizationNumber, out string? error)
    {
        if (OrganizationNumberValidator.IsWellFormed(organizationNumber))
        {
            error = null;
            return new Party
            {
                Scheme = SchemeIso6523ActorIdUpis,
                Id = NorwegianIcd + ":" + organizationNumber,
                NorwegianOrganizationNumber = organizationNumber
            };
        }

        error = "Not a well-formed norwegian organization number";
        return null;
    }

    private static Party? GetPartyFromNorwegianSsn(string ssn, out string? error)
    {
        if (SSNValidator.ValidateSSN(ssn))
        {
            error = null;
            return new Party
            {
                Scheme = SchemeNorwegianSsn,
                Id = ssn,
                NorwegianSocialSecurityNumber = ssn
            };
        }

        error = "Not a well-formed norwegian SSN";
        return null;
    }
}