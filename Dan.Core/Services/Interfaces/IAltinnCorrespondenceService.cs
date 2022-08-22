using Dan.Common.Models;
using Dan.Core.Helpers.Correspondence;
using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Represents a simple client used then communicating with the Altinn correspondence service.
/// </summary>
public interface IAltinnCorrespondenceService
{
    /// <summary>
    /// Create a new correspondence element.
    /// </summary>
    /// <param name="correspondence">The correspondence subject.</param>
    /// <returns>A receipt indicating whether the correspondence was successfully created.</returns>
    Task<ReceiptExternal> SendCorrespondence(CorrespondenceDetails correspondence);

    /// <summary>
    /// Create a new notification element.
    /// </summary>
    /// <param name="accreditation">The related accreditation.</param>
    /// <param name="serviceContext">The current service context.</param>
    /// <returns>A response indicating whether the notification was successfully sent.</returns>
    Task<List<NotificationReminder>> SendNotification(Accreditation accreditation, ServiceContext serviceContext);
}