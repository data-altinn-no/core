using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Dan.Core.Services;

/// <summary>
/// Helper class for validation of requrements when creating accreditations or validating evidence requests
/// </summary>
public class RequirementValidationService : IRequirementValidationService
{
    private readonly IAltinnServiceOwnerApiService _altinnServiceOwnerApiService;
    private readonly IEntityRegistryService _entityRegistryService;
    private readonly IRequestContextService _requestContextService;
    private AuthorizationRequest _authRequest;
    private string _owner;
    private List<string>? _maskinportenScopes;
    private readonly ConcurrentBag<string> _errors;
    private readonly ConcurrentDictionary<string, Requirement> _skippedEvidenceCodes;

    /// <summary>
    /// Create a new RequirementValidationHelper instance
    /// </summary>                
    /// <param name="altinnServiceOwnerApiService"></param>
    /// <param name="entityRegistryService"></param>
    /// <param name="requestContextService"></param>
    public RequirementValidationService(IAltinnServiceOwnerApiService altinnServiceOwnerApiService, IEntityRegistryService entityRegistryService, IRequestContextService requestContextService)
    {
        _altinnServiceOwnerApiService = altinnServiceOwnerApiService;
        _entityRegistryService = entityRegistryService;
        _requestContextService = requestContextService;
        _owner = string.Empty;
        _authRequest = new AuthorizationRequest();
        _errors = new ConcurrentBag<string>();
        _skippedEvidenceCodes = new ConcurrentDictionary<string, Requirement>();
    }

    /// <summary>
    /// Create a new RequirementValidationHelper instance
    /// </summary>
    /// <returns>The list of found request validation errors</returns>
    public async Task<List<string>> ValidateRequirements(Dictionary<string, List<Requirement>> evidenceCodeRequirements, AuthorizationRequest? authorizationRequest)
    {
        if (authorizationRequest == null)
        {
            throw new InvalidAuthorizationRequestException();
        }

        _owner = _requestContextService.AuthenticatedOrgNumber;
        _maskinportenScopes = _requestContextService.Scopes;
        _authRequest = authorizationRequest;

        var taskList = new List<Task>();
        foreach (var er in _authRequest.EvidenceRequests)
        {

            if (!evidenceCodeRequirements.ContainsKey(er.EvidenceCodeName))
            {
                throw new InternalServerErrorException($"Invalid state detected: evidence code requirements for {er.EvidenceCodeName} is null");
            }

            foreach (var req in evidenceCodeRequirements[er.EvidenceCodeName])
            {
                if (req.AppliesToServiceContext.Count > 0 && !req.AppliesToServiceContext.Contains(_requestContextService.ServiceContext.Name)) continue;

                taskList.Add(Task.Run(() => ValidateSingleRequirement(req, er.EvidenceCodeName)));
            }
        }

        await Task.WhenAll(taskList.ToArray());

        return _errors.ToList();
    }

    /// <summary>
    /// Returns any skipped evidence codes due to soft requirements
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, Requirement> GetSkippedEvidenceCodes()
    {
        return _skippedEvidenceCodes;
    }

    private async Task<bool> ValidateSingleRequirement(Requirement req, string evidenceCodeName)
    {
        var ret = req switch
        {
            WhiteListFromConfigRequirement r => ValidateWhiteListWithConfig(r, _authRequest, _owner, evidenceCodeName),
            WhiteListRequirement r => ValidateWhitelist(r, _authRequest, _owner, evidenceCodeName),
            ConsentRequirement r => ValidateConsent(r, _authRequest, evidenceCodeName),
            AltinnRoleRequirement r => await ValidateAltinnRole(r, _owner, _authRequest.Subject, _authRequest.Requestor, evidenceCodeName),
            AltinnRightsRequirement r => await ValidateAltinnRights(r, _owner, _authRequest.Subject, _authRequest.Requestor, evidenceCodeName),
            MaskinportenScopeRequirement r => ValidateMaskinportScopes(r, _maskinportenScopes, evidenceCodeName),
            SrrRequirement r => ValidateSRR(r, _authRequest, _owner, evidenceCodeName),
            LegalBasisRequirement r => ValidateLegalBasis(r, _authRequest, evidenceCodeName),
            PartyTypeRequirement r => await ValidatePartyTypes(r, _authRequest.Subject, _authRequest.Requestor, _owner, evidenceCodeName),
            AccreditationPartyRequirement r => ValidateAccreditationPartyRequirement(r, _authRequest.Subject, _authRequest.Requestor, _owner, evidenceCodeName),
            ReferenceRequirement r => ValidateReferenceRequirement(r, _authRequest, evidenceCodeName),
            ProvideOwnTokenRequirement r => ValidateProvideOwnTokenRequirement(r, evidenceCodeName),
            _ => false
        };

        if (ret || req.FailureAction != FailureAction.Skip) return ret;
        _skippedEvidenceCodes[evidenceCodeName] = req;
        return true;
    }

    private bool ValidateWhiteListWithConfig(WhiteListFromConfigRequirement req, AuthorizationRequest authRequest, string owner, string evidenceCodeName)
    {
        bool subjectResult;
        bool requestorResult;
        bool ownerResult;

        string[] subjectReqs = Array.Empty<string>();
        string[] requestorReqs = Array.Empty<string>();
        string[] ownerReqs = Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(req.SubjectConfigKey))
        {
            subjectReqs = Settings.GetWhiteList(req.SubjectConfigKey).Split(",");
        }

        if (!string.IsNullOrWhiteSpace(req.RequestorConfigKey))
        {
            requestorReqs = Settings.GetWhiteList(req.RequestorConfigKey).Split(",");
        }

        if (!string.IsNullOrWhiteSpace(req.OwnerConfigKey))
        {
            ownerReqs = Settings.GetWhiteList(req.OwnerConfigKey).Split(",");
        }

        if (subjectReqs.Any())
        {
            if (subjectReqs.Contains(authRequest.SubjectParty.ToString()))
            {
                subjectResult = true;
            }
            else
            {
                AddError(req, $"Subject '{authRequest.SubjectParty}' is not whitelisted for this evidence code", evidenceCodeName);
                subjectResult = false;
            }
        }
        else
        {
            subjectResult = true;
        }

        if (requestorReqs.Any())
        {
            if (requestorReqs.Contains(authRequest.RequestorParty.ToString()))
            {
                requestorResult = true;
            }
            else
            {
                AddError(req, $"Requestor {authRequest.RequestorParty} is not whitelisted for this evidence code", evidenceCodeName);
                requestorResult = false;
            }
        }
        else
        {
            requestorResult = true;
        }

        if (ownerReqs.Any())
        {
            if (ownerReqs.Contains(owner.Trim()))
            {
                ownerResult = true;
            }
            else
            {
                AddError(req, $"Owner {owner} is not whitelisted for this evidence code", evidenceCodeName);
                ownerResult = false;
            }
        }
        else
        {
            ownerResult = true;
        }

        return (subjectResult && requestorResult && ownerResult);
    }

    private bool ValidateAccreditationPartyRequirement(AccreditationPartyRequirement req, string? subject, string? requestor, string owner, string evidenceCodeName)
    {
        bool result = true;

        foreach (AccreditationPartyRequirementType a in req.PartyRequirements)
        {
            string partyError = "";
            bool singleResult = false;
            switch (a)
            {
                case AccreditationPartyRequirementType.RequestorAndOwnerAreEqual:
                    singleResult = (requestor == owner);
                    if (!singleResult) partyError = $"requestor and owner must be identical  - {requestor} and {owner}";
                    break;
                case AccreditationPartyRequirementType.RequestorAndOwnerAreNotEqual:
                    singleResult = (requestor != owner);
                    if (!singleResult) partyError = $"requestor and owner cannot be identical - {requestor} and {owner}";
                    break;
                case AccreditationPartyRequirementType.RequestorAndSubjectAreEqual:
                    singleResult = (requestor == subject);
                    if (!singleResult) partyError = $"requestor and subject must be identical - {requestor} and {subject}";
                    break;
                case AccreditationPartyRequirementType.RequestorAndSubjectAreNotEqual:
                    singleResult = (requestor != subject);
                    if (!singleResult) partyError = $"requestor and subject cannot be identical - {requestor} and {subject}";
                    break;
                case AccreditationPartyRequirementType.SubjectAndOwnerAreEqual:
                    singleResult = (subject == owner);
                    if (!singleResult) partyError = $"owner and subject must be identical - {subject} and {owner}";
                    break;

            }
            if (!singleResult)
            {
                AddError(req, "Accreditation parties are not compliant with authorization requirements: " + partyError, evidenceCodeName);
                result = false;
            }
        }

        return result;
    }

    private async Task<bool> ValidatePartyTypes(PartyTypeRequirement req, string? subject, string? requestor, string owner, string evidenceCodeName)
    {
        bool subjectResult;
        bool ownerResult;
        bool requestorResult;

        if (req.AllowedPartyTypes.Count < 1)
        {
            return true;
        }

        var subjectKeyPair = new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject, await GetPartyType(subject));
        var ownerKeyPair = new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Owner, await GetPartyType(owner));
        var requestorKeyPair = new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor, await GetPartyType(requestor));

        if (req.AllowedPartyTypes.Any(x => x.Key == AccreditationPartyTypes.Subject) && !req.AllowedPartyTypes.Any(z => z.Value == subjectKeyPair.Value && z.Key == AccreditationPartyTypes.Subject))
        {
            AddError(req, $"Invalid subject party type ({subject}), must be {string.Join(", ", req.AllowedPartyTypes.Where(h => h.Key == AccreditationPartyTypes.Subject).Select(v => v.Value.ToString()))}", evidenceCodeName);
            subjectResult = false;
        }
        else
        {
            subjectResult = true;
        }

        if (req.AllowedPartyTypes.Any(x => x.Key == AccreditationPartyTypes.Requestor) && !req.AllowedPartyTypes.Any(z => z.Value == requestorKeyPair.Value && z.Key == AccreditationPartyTypes.Requestor))
        {
            AddError(req, $"Invalid requestor party type ({requestor}), must be {string.Join(", ", req.AllowedPartyTypes.Where(h => h.Key == AccreditationPartyTypes.Requestor).Select(v => v.Value.ToString()))}", evidenceCodeName);
            requestorResult = false;
        }
        else
        {
            requestorResult = true;
        }

        if (req.AllowedPartyTypes.Any(x => x.Key == AccreditationPartyTypes.Owner) && !req.AllowedPartyTypes.Any(z => z.Value == ownerKeyPair.Value && z.Key == AccreditationPartyTypes.Owner))
        {
            AddError(req, $"Invalid owner party type ({owner}), must be {string.Join(", ", req.AllowedPartyTypes.Where(h => h.Key == AccreditationPartyTypes.Owner).Select(v => v.Value.ToString()))}", evidenceCodeName);
            ownerResult = false;
        }
        else
        {
            ownerResult = true;
        }

        return (subjectResult && requestorResult && ownerResult);
    }

    private bool ValidateLegalBasis(LegalBasisRequirement req, AuthorizationRequest? authRequest, string evidenceCodeName)
    {
        LegalBasis? selectedlegalBasis = authRequest?.LegalBasisList?.FirstOrDefault(legalBasis => legalBasis.Type.HasValue && req.ValidLegalBasisTypes.HasFlag(legalBasis.Type));

        if (selectedlegalBasis == null)
        {
            AddError(req, "A valid legal basis was not supplied", evidenceCodeName);
            return false;
        }

        var validator = LegalBasisValidatorFactory.Create(selectedlegalBasis, authRequest);
        if (validator.IsLegalBasisValid()) return true;
        AddError(req, "Failed to validated legal basis", evidenceCodeName);
        return false;
    }

    
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private bool ValidateSRR(Requirement req, AuthorizationRequest authRequest, string owner, string evidenceCodeName)
    {
        throw new NotImplementedException();
    }

    private bool ValidateMaskinportScopes(MaskinportenScopeRequirement req, IEnumerable<string>? maskinportenScopes, string evidenceCodeName)
    {
        var result = maskinportenScopes?.Intersect(req.RequiredScopes).Count() == req.RequiredScopes.Count();

        if (!result)
        {
            AddError(req, $"Missing required maskinport scopes {string.Join(", ", req.RequiredScopes.ToArray())}", evidenceCodeName);
        }

        return result;
    }

    private async Task<bool> ValidateAltinnRights(AltinnRightsRequirement req, string owner, string? subject, string? requestor, string evidenceCodeName)
    {
        if (subject == null)
        {
            AddError(req, "Altinn service delegations can only be checked beween norwegian parties; supplied subject is not", evidenceCodeName);
            return false;
        }

        if (requestor == null)
        {
            AddError(req, "Altinn service delegations can only be checked beween norwegian parties; supplied requestor is not", evidenceCodeName);
            return false;
        }

        if (req.ServiceCode == null || req.ServiceEdition == null)
        {
            AddError(req, "Cannot validate Altinn rights, missing service code / service edition", evidenceCodeName);
            return false;
        }

        GetOfferedByAndCoveredBy(req.OfferedBy, req.CoveredBy, owner, subject, requestor, out var offeredby, out var coveredby);

        var result = await _altinnServiceOwnerApiService.VerifyAltinnRight(offeredby, coveredby, req.ServiceCode, req.ServiceEdition);

        if (!result)
        {
            AddError(req, $"Missing required Altinn rights for service code {req.ServiceCode} and service edition {req.ServiceEdition}", evidenceCodeName);
        }

        return result;
    }

    private void GetOfferedByAndCoveredBy(AccreditationPartyTypes offerer, AccreditationPartyTypes coverer, string owner, string subject, string requestor, out string offeredby, out string coveredby)
    {
        offeredby = string.Empty;
        coveredby = string.Empty;

        switch (coverer)
        {
            case AccreditationPartyTypes.Subject:
                coveredby = subject;
                break;
            case AccreditationPartyTypes.Requestor:
                coveredby = requestor;
                break;
            case AccreditationPartyTypes.Owner:
                coveredby = owner;
                break;
        }

        switch (offerer)
        {
            case AccreditationPartyTypes.Subject:
                offeredby = subject;
                break;
            case AccreditationPartyTypes.Requestor:
                offeredby = requestor;
                break;
            case AccreditationPartyTypes.Owner:
                offeredby = owner;
                break;
        }
    }

    private async Task<bool> ValidateAltinnRole(AltinnRoleRequirement req, string owner, string? subject, string? requestor, string evidenceCodeName)
    {
        if (subject == null)
        {
            AddError(req, "Altinn role delegations can only be checked beween norwegian parties; supplied subject is not", evidenceCodeName);
            return false;
        }

        if (requestor == null)
        {
            AddError(req, "Altinn role delegations can only be checked beween norwegian parties; supplied requestor is not", evidenceCodeName);
            return false;
        }

        if (req.RoleCode == null)
        {
            AddError(req, "Cannot validate Altinn role, role code not set", evidenceCodeName);
            return false;
        }

        GetOfferedByAndCoveredBy(req.OfferedBy, req.CoveredBy, owner, subject, requestor, out var offeredby, out var coveredby);

        var result = await _altinnServiceOwnerApiService.VerifyAltinnRole(offeredby, coveredby, req.RoleCode);
        if (!result)
        {
            AddError(req, $"Missing Altinn role {req.RoleCode} from {offeredby} to {coveredby}", evidenceCodeName);
        }

        return result;
    }

    private bool ValidateConsent(Requirement req, AuthorizationRequest authRequest, string evidenceCodeName)
    {
        var result = true;

        if (authRequest.FromEvidenceHarvester)
        {
            return result;
        }

        if (authRequest.EvidenceRequests.Find(x => x.EvidenceCodeName == evidenceCodeName)?.RequestConsent != true)
        {
            AddError(req, "This dataset requires consent; you need to supply \"requestConsent\": true", evidenceCodeName);
            result = false;
        }

        if (string.IsNullOrEmpty(authRequest.ConsentReference))
        {
            AddError(req, "This dataset requires consent; the field 'consentReference' must be supplied", evidenceCodeName);
            result = false;
        }

        if (authRequest.Subject == null)
        {
            AddError(req, $"Consent based evidence can only be requested from norwegian parties; subject '{authRequest.SubjectParty}' is not", evidenceCodeName);
            result = false;
        }

        if (authRequest.Requestor == null)
        {
            AddError(req, $"Consent based evidence can only be requested by norwegian parties; requestor '{authRequest.RequestorParty}' is not", evidenceCodeName);
            result = false;
        }

        return result;
    }

    private bool ValidateWhitelist(WhiteListRequirement req, AuthorizationRequest authRequest, string owner, string evidenceCodeName)
    {
        var subjectResult = true;
        var requestorResult = true;
        var ownerResult = true;

        var hasSubjectReqs = req.AllowedParties.Any(pair => pair.Key == AccreditationPartyTypes.Subject);
        var hasRequestorReqs = req.AllowedParties.Any(pair => pair.Key == AccreditationPartyTypes.Requestor);
        var hasOwnerReqs = req.AllowedParties.Any(pair => pair.Key == AccreditationPartyTypes.Owner);

        if (hasSubjectReqs)
        {
            if (!req.AllowedParties.Any(pair => pair.Key == AccreditationPartyTypes.Subject && pair.Value.Equals(authRequest.SubjectParty.ToString())))
            {
                AddError(req, $"Subject {authRequest.SubjectParty} is not whitelisted for this evidence code", evidenceCodeName);
                subjectResult = false;
            }
        }

        if (hasRequestorReqs)
        {
            if (!req.AllowedParties.Any(pair => pair.Key == AccreditationPartyTypes.Requestor && pair.Value.Equals(authRequest.RequestorParty.ToString())))
            {
                AddError(req, $"Requestor {authRequest.RequestorParty} is not whitelisted for this evidence code", evidenceCodeName);
                requestorResult = false;
            }
        }

        if (hasOwnerReqs)
        {
            if (!req.AllowedParties.Any(pair => pair.Key == AccreditationPartyTypes.Owner && pair.Value.Equals(owner)))
            {
                AddError(req, $"Owner {owner} is not whitelisted for this evidence code", evidenceCodeName);
                ownerResult = false;
            }
        }

        return (subjectResult && requestorResult && ownerResult);
    }

    private bool ValidateReferenceRequirement(ReferenceRequirement req, AuthorizationRequest authRequest, string evidenceCodeName)
    {
        string referenceType;
        string? referenceValue;

        if (req.ReferenceType == ReferenceType.ConsentReference)
        {
            referenceType = "consent reference";
            referenceValue = authRequest.ConsentReference;
        }
        else if (req.ReferenceType == ReferenceType.ExternalReference)
        {
            referenceType = "external reference";
            referenceValue = authRequest.ExternalReference;
        }
        else
        {
            throw new InternalServerErrorException("Reference requirement definition is invalid; Unknown or missing reference type");
        }

        // Check if the reference in the authorization request is set
        if (string.IsNullOrEmpty(referenceValue))
        {
            AddError(req, $"The request requires a valid {referenceType} but none is provided", evidenceCodeName);
            return false;
        }

        // Check if the reference exceedes the maximum length allowed for a reference
        if (referenceValue.Length > Settings.MaxReferenceLength)
        {
            AddError(req, $"The {referenceType} exceedes the maximum reference length of {Settings.MaxReferenceLength} characters ", evidenceCodeName);
        }

        // Check that an accepted format is provided in the reference requirement, and that it is a valid regular expression
        Regex validReference;
        try
        {
            validReference = new Regex(req.AcceptedFormat);
        }
        catch (Exception e)
        {
            if (e is ArgumentNullException)
            {
                AddError(req, $"A valid reference is required but no accepted format is provided.", evidenceCodeName);
            }

            if (e is ArgumentException)
            {
                AddError(req, $"The accepted format is invalid; '{req.AcceptedFormat}' is not a valid regular expression.", evidenceCodeName);
            }
            return false;
        }


        // Validate that the value of the provided reference conforms to the accepted format defined in the requirements
        if (validReference.IsMatch(referenceValue))
        {
            return true;
        }
        else
        {
            AddError(req, $"The provided {referenceType} is invalid; does not match the regular expression '{req.AcceptedFormat}'", evidenceCodeName);
            return false;
        }
    }

    private bool ValidateProvideOwnTokenRequirement(Requirement req, string evidenceCodeName)
    {
        var evidenceHarvesterOptions = _requestContextService.GetEvidenceHarvesterOptionsFromRequest();

        if (evidenceHarvesterOptions.OverriddenAccessToken != null || evidenceHarvesterOptions.ReuseClientAccessToken || evidenceHarvesterOptions.FetchSupplierAccessTokenOnBehalfOfOwner)
        {
            return true;
        }

        AddError(req, $"The dataset {evidenceCodeName} requires that the client provides a bearer token or delegates access to Digitaliseringsdirektoratet", evidenceCodeName);
        return false;
    }

    private async Task<PartyTypeConstraint> GetPartyType(string? identifier)
    {
        if (identifier == null) return PartyTypeConstraint.Foreign; 

        var result = PartyTypeConstraint.Invalid;
        if (identifier.Length == 9)
        {
            if (OrganizationNumberValidator.IsWellFormed(identifier))
            {
                result = await _entityRegistryService.IsOrganizationPublicAgency(identifier) ? PartyTypeConstraint.PublicAgency : PartyTypeConstraint.PrivateEnterprise;
            }
        }
        else if (identifier.Length == 11)
        {
            if (SSNValidator.ValidateSSN(identifier))
            {
                result = PartyTypeConstraint.PrivatePerson;
            }
        }

        return result;
    }

    private void AddError(Requirement req, string error, string evidenceCodeName)
    {
        if (req.FailureAction != FailureAction.Skip) _errors.Add($"{evidenceCodeName}: {error}");
    }
}
