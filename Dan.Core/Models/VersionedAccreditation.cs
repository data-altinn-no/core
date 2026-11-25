using Dan.Common.Models;

namespace Dan.Core.Models;

/// <summary>
/// An accreditation together with the version (ETag) it had when it was read, for saving it only if nobody else has
/// changed it in the meantime
/// </summary>
public record VersionedAccreditation(Accreditation Accreditation, string ETag);
