namespace Dan.Common.Enums;

/// <summary>
/// Error codes for Nadobe
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    InvalidCertificateException = 1000,

    /// <summary>
    /// Invalid Organization Exception
    /// </summary>
    InvalidRequestorException = 1001,

    /// <summary>
    /// Non-existent Accreditation Exception
    /// </summary>
    NonExistentAccreditationException = 1002,

    /// <summary>
    /// Expired Accreditation Exception
    /// </summary>
    ExpiredAccreditationException = 1003,

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    InvalidSubjectException = 1004,

    /// <summary>
    /// Invalid Organization Exception
    /// </summary>
    DeletedSubjectException = 1005,

    /// <summary>
    /// Invalid Evidence Request Exception
    /// </summary>
    InvalidEvidenceRequestException = 1006,

    /// <summary>
    /// Unknown Evidence Code Exception
    /// </summary>
    UnknownEvidenceCodeException = 1007,

    /// <summary>
    /// Invalid Legal Basis Exception
    /// </summary>
    InvalidLegalBasisException = 1008,

    /// <summary>
    /// Error In Legal Basis Reference Exception
    /// </summary>
    ErrorInLegalBasisReferenceException = 1009,

    /// <summary>
    /// Requires Consent Exception
    /// </summary>
    RequiresConsentException = 1010,

    /// <summary>
    /// Expired Consent Exception
    /// </summary>
    ExpiredConsentException = 1011,

    /// <summary>
    /// Deleted Consent Exception
    /// </summary>
    DeletedConsentException = 1012,

    /// <summary>
    /// Sender Not Available Exception
    /// </summary>
    ServiceNotAvailableException = 1013,

    /// <summary>
    /// Invalid Authorization Request Exception
    /// </summary>
    InvalidAuthorizationRequestException = 1014,

    /// <summary>
    /// Invalid Authorization Request Exception
    /// </summary>
    InternalServerErrorException = 1015,

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    AsyncEvidenceStillWaitingException = 1016,

    /// <summary>
    /// Invalid Evidence Request Parameter Exception
    /// </summary>
    InvalidEvidenceRequestParameterException = 1017,

    /// <summary>
    /// Invalid Evidence Request Parameter Exception
    /// </summary>
    InvalidValidToDateTimeException = 1018,

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    AuthorizationFailedException = 1019,

    /// <summary>
    /// Failed authorization of owner
    /// </summary>
    InvalidOwnerException = 1020,

    /// <summary>
    /// Failed attempt to remind a user about a consent that is already given/rejected
    /// </summary>
    ConsentAlreadyHandledException = 1021,

    /// <summary>
    /// Failures in communication with altinn  correspondence and notification
    /// </summary>
    AltinnServiceException = 1022,

    /// <summary>
    /// No valid authentication options present in request
    /// </summary>
    MissingAuthenticationException = 1023,

    /// <summary>
    /// Invalid access token exception
    /// </summary>
    InvalidAccessTokenException = 1024,

    /// <summary>
    /// Misconfigured servicecontext (ie texts)
    /// </summary>
    ServiceContextException = 1025,

    /// <summary>
    /// Some error occured accessing the accreditation repository
    /// </summary>
    AccreditationRepositoryException = 1026,

    /// <summary>
    /// An invalid JMES Path expression was supplied
    /// </summary>
    InvalidJmesPathExpressionException = 1027,

    ////
    //// Error codes for evidence source implementations (3xxx)
    ////

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    EvidenceSourceTransientException = 3001,

    /// <summary>
    /// Permanent evidence source client exception
    /// </summary>
    EvidenceSourcePermanentClientException = 3002,

    /// <summary>
    /// Permanent evidence source server exception
    /// </summary>
    EvidenceSourcePermanentServerException = 3003,
}