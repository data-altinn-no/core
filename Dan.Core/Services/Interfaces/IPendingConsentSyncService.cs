using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

public interface IPendingConsentSyncService
{
    /// <summary>
    /// Asks Altinn for the status of every consent request we are still waiting for, and stores the answer on the
    /// accreditation when the request has been accepted, rejected or revoked. This picks up answers given in Altinn where the
    /// subject never returned to the consent receipt.
    /// </summary>
    /// <returns>How many accreditations were checked, and what happened to them</returns>
    Task<PendingConsentSyncResult> Sync();
}
