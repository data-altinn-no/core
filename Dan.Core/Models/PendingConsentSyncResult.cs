namespace Dan.Core.Models;

/// <summary>
/// The outcome of a run of the pending consent sync
/// </summary>
/// <param name="Checked">Accreditations whose consent request was looked up in Altinn</param>
/// <param name="Granted">Accreditations stored as consent given</param>
/// <param name="Denied">Accreditations stored as consent denied</param>
/// <param name="Failed">Accreditations that could not be checked or stored</param>
/// <param name="NotChecked">Accreditations left for the next run, because this run was stopped after too many failures in a row</param>
public record PendingConsentSyncResult(int Checked, int Granted, int Denied, int Failed, int NotChecked = 0);
