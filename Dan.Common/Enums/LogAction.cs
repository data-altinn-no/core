namespace Dan.Common.Enums;

/// <summary>
/// Log action enum
/// </summary>
public enum LogAction
{
    /// <summary>
    /// Used once per accreditation
    /// </summary>
    AuthorizationGranted = 1,

    /// <summary>
    /// Used per consent request (one accreditation may incur several consent requests, each of which may span
    /// several datasets
    /// </summary>
    ConsentRequested = 2,

    /// <summary>
    /// As with ConsentRequested
    /// </summary>
    ConsentGiven = 3,

    /// <summary>
    /// As with ConsentRequested
    /// </summary>
    ConsentDenied = 4,

    /// <summary>
    /// Used for each harvest performed, of which there may be several per accreditation
    /// </summary>
    DatasetRetrieved = 5,

    /// <summary>
    /// As with ConsentRequested
    /// </summary>
    ConsentReminderSent = 6,

    /// <summary>
    /// Used when a open data set harvest is performed
    /// </summary>
    OpenDatasetRetrieved = 7,

    /// <summary>
    /// Used once per accreditation
    /// </summary>
    AccreditationDeleted = 8,

    /// <summary>
    /// Used per data set requested for each AuthorizationGranted
    /// </summary>
    DatasetRequested = 9,

    /// <summary>
    /// Used for each dataset in a consent request
    /// </summary>
    DatasetRequiringConsentRequested = 10,

    /// <summary>
    /// Used for each correspondence sent related to consent requests (may be skipped, so counted seperately)
    /// </summary>
    CorrespondenceSent = 11,

    /// <summary>
    /// Used when an owner requests accreditations for all or one requestor
    /// </summary>
    AccreditationsRetrieved = 11,
}