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
        Created,

        /// <summary>The request was rejected by the subject</summary>
        Rejected,

        /// <summary>The request was accepted by the subject</summary>
        Accepted,

        /// <summary>The consent was given, and later withdrawn</summary>
        Revoked,

        /// <summary>The request was deleted</summary>
        Deleted,

        /// <summary>The request or consent has expired</summary>
        Expired
    }
}
