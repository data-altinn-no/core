using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Services;

/// <summary>
/// Helper for performing authorization request validation
/// </summary>
public class AuthorizationRequestValidatorService : IAuthorizationRequestValidatorService
{
    private readonly ILogger<AuthorizationRequestValidatorService> _log;
    private readonly IEntityRegistryService _entityRegistryService;
    private readonly IAvailableEvidenceCodesService _availableEvidenceCodesService;
    private readonly IRequirementValidationService _requirementValidationService;
    private readonly IRequestContextService _requestContextService;
    private AuthorizationRequest _authRequest = new();
    private List<EvidenceCode> _registeredEvidenceCodes = new();
    private List<EvidenceCode> _evidenceCodesFromRequest = new();

    /// <summary>
    /// Takes a authorization request that should be validated as well as a HTTP client for performing external lookups
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="entityRegistryService">The injected entity registry service</param>
    /// <param name="availableEvidenceCodesService">The injected service for available evidence codes</param>
    /// <param name="requirementValidationService">The injected service for authorization requirement validation</param>
    /// <param name="requestContextService">The injected service for getting request context information</param>
    public AuthorizationRequestValidatorService(
        ILoggerFactory loggerFactory,
        IEntityRegistryService entityRegistryService,
        IAvailableEvidenceCodesService availableEvidenceCodesService,
        IRequirementValidationService requirementValidationService,
        IRequestContextService requestContextService)
    {
        _log = loggerFactory.CreateLogger<AuthorizationRequestValidatorService>();
        _entityRegistryService = entityRegistryService;
        _availableEvidenceCodesService = availableEvidenceCodesService;
        _requirementValidationService = requirementValidationService;
        _requestContextService = requestContextService;

        _entityRegistryService.UseCoreProxy = false;
        _entityRegistryService.AllowTestCcrLookup = !Settings.IsProductionEnvironment;
    }

    /// <summary>
    /// Performs validation of the authorization request. First performs simple validations of parties, 
    /// then all requirements specified on the evidencecodes in the authorization request
    /// </summary>
    /// <returns>Async task</returns>
    public async Task Validate(AuthorizationRequest? authorizationRequest)
    {
        _authRequest = authorizationRequest ?? throw new InvalidAuthorizationRequestException();
        _registeredEvidenceCodes = await _availableEvidenceCodesService.GetAvailableEvidenceCodes();
        _evidenceCodesFromRequest = _registeredEvidenceCodes.Where(r => _authRequest.EvidenceRequests.Any(x => x.EvidenceCodeName == r.EvidenceCodeName)).ToList();

        ValidateAndPopulateRequestor();
        ValidateAndPopulateSubject();
        ValidateLegalBasisWellFormed();
        ValidateEvidenceRequestWellFormed();
        ValidateEvidenceCodesAreAvailableForServiceContext();
        ValidateEvidenceRequestParameters();
        ValidateValidToDate();

        if (_authRequest.SubjectParty.NorwegianOrganizationNumber != null)
        {
            await ValidateSubjectHasValidEntryInEntityRegister();
        }
        
        if (_authRequest.RequestorParty.NorwegianOrganizationNumber != null)
        {
            await ValidateRequestorHasValidEntryInEntityRegister();
        }

        ValidateLanguageCodes();

        var requirements = _evidenceCodesFromRequest.ToDictionary(es => es.EvidenceCodeName, es => es.AuthorizationRequirements);
        if (authorizationRequest.FromEvidenceHarvester)
        {
            foreach (var requirement in requirements.Values)
            {
                requirement.RemoveAll(x => x.RequiredOnEvidenceHarvester == false);
            }
        }

        var authorizationErrors = await _requirementValidationService.ValidateRequirements(requirements, _authRequest);
        if (authorizationErrors.Count > 0)
        {
            throw new AuthorizationFailedException(string.Join(", ", authorizationErrors));
        }

        foreach (var (evidenceCodeName, _) in _requirementValidationService.GetSkippedEvidenceCodes())
        {
            _authRequest.EvidenceRequests.RemoveAll(x => x.EvidenceCodeName == evidenceCodeName);
        }

        if (_authRequest.EvidenceRequests.Count == 0)
        {
            throw new AuthorizationFailedException(
                $"No evidence requests could be satisfied. The following evidence codes were skipped due to unmet authorization requirements: {string.Join(", ", _requirementValidationService.GetSkippedEvidenceCodes().Keys)}");
        }
    }

    /// <summary>
    /// Get the validated authorization request
    /// </summary>
    /// <returns>An authorization request</returns>
    public AuthorizationRequest GetAuthorizationRequest()
    {
        return _authRequest;
    }

    /// <summary>
    /// Get all the connected evidence codes with any supplied parameters
    /// </summary>
    /// <returns>A list of evidence codes</returns>
    public List<EvidenceCode> GetEvidenceCodes()
    {
        var evidenceCodes = _registeredEvidenceCodes.Where(r => _authRequest.EvidenceRequests.Any(x => x.EvidenceCodeName == r.EvidenceCodeName)).ToList();
        var evidenceCodesFromRequest = evidenceCodes.DeepCopy();
        foreach (var evidenceCode in evidenceCodesFromRequest)
        {
            evidenceCode.Parameters = _authRequest.EvidenceRequests.Find(x => x.EvidenceCodeName == evidenceCode.EvidenceCodeName)?.Parameters ?? new List<EvidenceParameter>();
        }

        return evidenceCodesFromRequest;
    }

    public IDictionary<string, Requirement> GetSkippedEvidenceCodes()
    {
        return _requirementValidationService.GetSkippedEvidenceCodes();
    }

    /// <summary>
    /// Get all evidence code names that has verified legal basis.
    /// FIXME! This whole deal should be removed.
    /// </summary>
    /// <returns>A list of evidence code names</returns>
    public List<string> GetEvidenceCodeNamesWithVerifiedLegalBasis()
    {
        return _authRequest.EvidenceRequests.Where(x => x.LegalBasisId != null).Select(x => x.EvidenceCodeName).ToList();
    }

    /// <summary>
    /// Returns the effective valid to date taking into account limitations in evidence codes. Assumes validation has been performed.
    /// </summary>
    /// <returns>The date and time to which the accreditation is valid</returns>
    public DateTime GetValidTo()
    {
        if (_authRequest.ValidTo.HasValue)
        {
            return _authRequest.ValidTo.Value;
        }

        var validToMinDays = GetLowestValidToDays();
        return DateTime.Now.AddDays(validToMinDays ?? Settings.AccreditationDefaultValidDays);
    }

    /// <summary>
    /// Uses PartyParser on the supplied requestor, and populates RequestorParty with it. Overwrites Requestor with norwegian identifier if applicable, else set to null
    /// </summary>
    /// <exception cref="InvalidRequestorException"></exception>
    private void ValidateAndPopulateRequestor()
    {
        if (_authRequest.Requestor == null)
        {
            throw new InvalidRequestorException($"Invalid requestor supplied (was null)");
        }

        Party? party = PartyParser.GetPartyFromIdentifier(_authRequest.Requestor, out string? error);
        if (party == null)
        {
            throw new InvalidRequestorException($"Invalid requestor supplied: {error}");
        }

        _authRequest.Requestor = party.NorwegianOrganizationNumber ?? party.NorwegianSocialSecurityNumber;
        _authRequest.RequestorParty = party;
    }

    /// <summary>
    /// Uses PartyParser on the supplied subject, and populates SubjectParty with it. Overwrites Requestor with norwegian identifier if applicable, else set to null
    /// </summary>
    /// <exception cref="InvalidSubjectException"></exception>
    private void ValidateAndPopulateSubject()
    {

        if (_authRequest.Subject == null)
        {
            return;
        }

        Party? party = PartyParser.GetPartyFromIdentifier(_authRequest.Subject, out string? error);
        if (party == null)
        {
            throw new InvalidSubjectException($"Invalid subject supplied: {error}");
        }

        _authRequest.Subject = party.NorwegianOrganizationNumber ?? party.NorwegianSocialSecurityNumber;
        _authRequest.SubjectParty = party;
    }

    private void ValidateLegalBasisWellFormed()
    {
        if (_authRequest.LegalBasisList == null)
        {
            return;
        }

        foreach (var legalBasis in _authRequest.LegalBasisList)
        {
            if (string.IsNullOrEmpty(legalBasis.Id))
            {
                throw new InvalidLegalBasisException("Missing id in legal basis");
            }

            if (legalBasis.Type == null)
            {
                throw new InvalidLegalBasisException("Invalid legal basis type");
            }

            if (string.IsNullOrEmpty(legalBasis.Content))
            {
                throw new InvalidLegalBasisException("Invalid legal basis content");
            }

            if (_authRequest.EvidenceRequests.Find(x => x.LegalBasisId == legalBasis.Id) == null)
            {
                throw new InvalidLegalBasisException($"Unreferenced legal basis: {legalBasis.Id}");
            }
        }
    }

    private void ValidateLanguageCodes()
    {
        if (string.IsNullOrEmpty(_authRequest.LanguageCode)) return;
        var validLanguages = _requestContextService.ServiceContext.ValidLanguages;
        if (validLanguages != null)
        {
            if (validLanguages.Contains(_authRequest.LanguageCode)) return;
            var languageCodeList = string.Join(", ", validLanguages);
            if (validLanguages.Count > 1)
            {
                languageCodeList = "one of " + languageCodeList;
            }
            throw new InvalidAuthorizationRequestException($"Invalid language code for {_requestContextService.ServiceContext.Id}: {_authRequest.LanguageCode} - must be {languageCodeList}");
        }
        var acceptedLanguages = new List<string>
        {
            Constants.LANGUAGE_CODE_NORWEGIAN_NB,
            Constants.LANGUAGE_CODE_NORWEGIAN_NN,
            Constants.LANGUAGE_CODE_ENGLISH
        };
        if (!acceptedLanguages.Contains(_authRequest.LanguageCode))
        {
            throw new InvalidAuthorizationRequestException($"Invalid language code for {_requestContextService.ServiceContext.Id}: {_authRequest.LanguageCode} - must be one of {string.Join(",", acceptedLanguages)}");
        }
    }

    private int? GetLowestValidToDays()
    {
        return _registeredEvidenceCodes.Where(x => x.MaxValidDays.HasValue).OrderBy(x => x.MaxValidDays).FirstOrDefault()?.MaxValidDays;
    }

    private void ValidateValidToDate()
    {
        if (!_authRequest.ValidTo.HasValue)
        {
            return;
        }

        if (_authRequest.ValidTo.Value < DateTime.Now.AddDays(Settings.AccreditationMinimumValidDays))
        {
            throw new InvalidValidToDateTimeException($"The accreditation must be valid for at least {Settings.AccreditationMinimumValidDays} days");
        }

        if (_authRequest.ValidTo.Value > DateTime.Now.AddDays(Settings.AccreditationMaximumValidDays))
        {
            throw new InvalidValidToDateTimeException($"The accreditation can be valid for at most {Settings.AccreditationMaximumValidDays} days");
        }

        var lowestValidToDays = GetLowestValidToDays();
        if (lowestValidToDays.HasValue && _authRequest.ValidTo.Value > DateTime.Now.AddDays(lowestValidToDays.Value))
        {
            throw new InvalidValidToDateTimeException($"The accreditation can be valid for at most {lowestValidToDays.Value} days due to limitations defined in one or more of the requested evidence codes");
        }
    }

    private async Task ValidateSubjectHasValidEntryInEntityRegister()
    {
        if (_authRequest.Subject == null)
        {
            throw new InvalidSubjectException("Subject was not set");
        }

        var entity = await _entityRegistryService.Get(_authRequest.Subject);

        if (entity == null)
        {
            throw new InvalidSubjectException("Subject (" + _authRequest.Subject + ") was not found in the Central Coordinating Register for Legal Entities");
        }

        if (entity.IsDeleted)
        {
            throw new InvalidSubjectException("Subject (" + _authRequest.Subject + ") is deleted from the Central Coordinating Register for Legal Entities");
        }
    }
    
    private async Task ValidateRequestorHasValidEntryInEntityRegister()
    {
        if (_authRequest.Requestor == null)
        {
            throw new InvalidSubjectException("Requestor was not set");
        }

        var entity = await _entityRegistryService.Get(_authRequest.Requestor);

        if (entity == null)
        {
            throw new InvalidSubjectException("Requestor (" + _authRequest.Requestor + ") was not found in the Central Coordinating Register for Legal Entities");
        }

        if (entity.IsDeleted)
        {
            throw new InvalidSubjectException("Requestor (" + _authRequest.Requestor + ") is deleted from the Central Coordinating Register for Legal Entities");
        }
    }

    private void ValidateEvidenceRequestWellFormed()
    {
        if (_authRequest.EvidenceRequests == null || _authRequest.EvidenceRequests.Count == 0)
        {
            throw new InvalidEvidenceRequestException("No evidence request was supplied");
        }

        if (_authRequest.EvidenceRequests.GroupBy(x => x.EvidenceCodeName).Any(g => g.Count() > 1))
        {
            throw new InvalidEvidenceRequestException("Multiple requests for the same evidence code is not supported");
        }
    }

    private void ValidateEvidenceCodesAreAvailableForServiceContext()
    {
        foreach (var evidenceRequest in _authRequest.EvidenceRequests)
        {
            var registeredEvidenceCode = _registeredEvidenceCodes.Find(x => x.EvidenceCodeName == evidenceRequest.EvidenceCodeName);

            if (registeredEvidenceCode == null)
            {
                throw new UnknownEvidenceCodeException($"Unknown evidence code: {evidenceRequest.EvidenceCodeName}");
            }

            if (!registeredEvidenceCode.IsValidServiceContext(_requestContextService.ServiceContext))
            {
                _log.LogWarning("Request for '{evidenceCode}' from '{authenticatedOrg}' with subscription key '{subscriptionKey}' for product '{productid}', expected one of: {availableForProducts}",
                    registeredEvidenceCode.EvidenceCodeName, _requestContextService.AuthenticatedOrgNumber, _requestContextService.SubscriptionKey, _requestContextService.ServiceContext.Name, string.Join(", ", registeredEvidenceCode.GetBelongsToServiceContexts()));

                throw new InvalidEvidenceRequestException(
                    $"The evidence code: {evidenceRequest.EvidenceCodeName} is not available for the supplied subscription key product '{_requestContextService.ServiceContext.Name}', expected one of: {string.Join(", ", registeredEvidenceCode.GetBelongsToServiceContexts())}.");
            }
        }
    }

    private void ValidateEvidenceRequestParameters()
    {
        foreach (var evidenceRequest in _authRequest.EvidenceRequests)
        {
            var registeredEvidenceCode = _registeredEvidenceCodes.Find(x => x.EvidenceCodeName == evidenceRequest.EvidenceCodeName);

            if (registeredEvidenceCode == null)
            {
                continue;
            }

            // Normalize for simpler handling
            registeredEvidenceCode.Parameters ??= new List<EvidenceParameter>();
            evidenceRequest.Parameters ??= new List<EvidenceParameter>();

            if (registeredEvidenceCode.Parameters.Count == 0 && evidenceRequest.Parameters.Count > 0)
            {
                throw new InvalidEvidenceRequestParameterException($"The evidence code '{evidenceRequest.EvidenceCodeName}' was requested with parameters, but none were expected");
            }

            if (evidenceRequest.Parameters.Count > registeredEvidenceCode.Parameters.Count || evidenceRequest.Parameters.Count < registeredEvidenceCode.Parameters.FindAll(x => x.Required.HasValue && x.Required.Value).Count)
            {
                throw new InvalidEvidenceRequestParameterException($"The evidence code '{evidenceRequest.EvidenceCodeName}' was requested with an invalid amount of parameters");
            }

            foreach (var requiredParam in registeredEvidenceCode.Parameters.FindAll(x => x.Required.HasValue && x.Required.Value))
            {
                if (evidenceRequest.Parameters.Find(x => x.EvidenceParamName == requiredParam.EvidenceParamName) == null)
                {
                    throw new InvalidEvidenceRequestParameterException($"The required evidence code parameter '{requiredParam.EvidenceParamName}' was not supplied");
                }
            }

            foreach (var evidenceRequestParameter in evidenceRequest.Parameters)
            {
                var registeredEvidenceParameter = registeredEvidenceCode.Parameters.Find(x => x.EvidenceParamName == evidenceRequestParameter.EvidenceParamName);
                if (registeredEvidenceParameter == null || registeredEvidenceParameter.ParamType == null)
                {
                    throw new InvalidEvidenceRequestParameterException($"Unknown evidence code parameter '{evidenceRequestParameter.EvidenceParamName}'");
                }

                switch (registeredEvidenceParameter.ParamType.Value)
                {
                    case EvidenceParamType.Attachment:
                    case EvidenceParamType.String:
                        // We allow anything as attachment or string
                        break;
                    case EvidenceParamType.Boolean:
                        if (!bool.TryParse(evidenceRequestParameter.Value?.ToString(), out _))
                        {
                            throw new InvalidEvidenceRequestParameterException($"Invalid evidence code parameter value given for parameter '{evidenceRequestParameter.EvidenceParamName}', expected 'true' or 'false'");
                        }

                        break;
                    case EvidenceParamType.DateTime:
                        if (!DateTime.TryParse(evidenceRequestParameter.Value?.ToString(), out _))
                        {
                            throw new InvalidEvidenceRequestParameterException($"Invalid evidence code parameter value given for parameter '{evidenceRequestParameter.EvidenceParamName}', expected ISO 8601 datetime");
                        }

                        break;
                    case EvidenceParamType.Number:
                        if (!decimal.TryParse(evidenceRequestParameter.Value?.ToString(), out _))
                        {
                            throw new InvalidEvidenceRequestParameterException($"Invalid evidence code parameter value given for parameter '{evidenceRequestParameter.EvidenceParamName}', expected numeric value");
                        }

                        break;
                }
            }
        }
    }
}
