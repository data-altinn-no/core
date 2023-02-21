namespace Dan.Common.Enums;

public enum LogAction
{
    // Used once per accreditation
    AuthorizationGranted = 1,

    // Used per consent request (one accreditation may incur several consent requests, each of which may span
    // several datasets
    ConsentRequested = 2,

    // As with ConsentRequested
    ConsentGiven = 3,

    // As with ConsentRequested
    ConsentDenied = 4,

    // Used for each harvest performed, of which there may be several per accreditation
    DatasetRetrieved = 5,

    // As with ConsentRequested
    ConsentReminderSent = 6,

    // Used when a open data set harvest is performed
    OpenDatasetRetrieved = 7,

    // Used once per accreditation
    AccreditationDeleted = 8,

    // Used per data set requested for each AuthorizationGranted
    DatasetRequested = 9,

    // Used for each dataset in a consent request
    DatasetRequiringConsentRequested = 10,

    // Used for each correspondence sent related to consent requests (may be skipped, so counted seperately)
    CorrespondenceSent = 11,
}