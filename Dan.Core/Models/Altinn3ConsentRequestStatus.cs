namespace Dan.Core.Models
{
    /// <summary>
    /// The status of a consent request as reported by Altinn 3
    /// </summary>
    public enum Altinn3ConsentRequestStatus
    {
        /// <summary>Altinn reported a status we do not know</summary>
        Unknown = 0,

        /// <summary>The request is created, but not yet answered</summary>
        Created = 1,

        /// <summary>The request was rejected by the subject</summary>
        Rejected = 2,

        /// <summary>The request was accepted by the subject</summary>
        Accepted = 3,

        /// <summary>The consent was given, and later withdrawn</summary>
        Revoked = 4,

        /// <summary>The request was deleted</summary>
        Deleted = 5,

        /// <summary>The request or consent has expired</summary>
        Expired = 6,

        /// <summary>Altinn has no consent request with this id. Not a status in Altinn, but its answer to the lookup</summary>
        NotFound = 7
    }
}
