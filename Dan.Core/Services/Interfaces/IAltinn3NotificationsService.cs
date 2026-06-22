using Dan.Common.Models;
using Dan.Core.Models.Notifications;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Sends notifications (SMS/email) via the Altinn 3 Notifications API.
/// Replaces the legacy Altinn correspondence notification path for consent reminders.
/// </summary>
public interface IAltinn3NotificationsService
{
    /// <summary>
    /// Sends a consent reminder to the accreditation subject as a standard notification order
    /// (sent now), using both email and SMS. Mirrors the previous
    /// <c>IAltinnCorrespondenceService.SendNotification</c> signature so the result can be
    /// stored directly on the accreditation.
    /// </summary>
    /// <param name="accreditation">The related accreditation.</param>
    /// <param name="serviceContext">The current service context (holds the text templates).</param>
    /// <returns>One reminder receipt per channel (email and SMS).</returns>
    Task<List<NotificationReminder>> SendReminder(Accreditation accreditation, ServiceContext serviceContext);

    /// <summary>
    /// Creates a notification order (future/standard order) and returns the order chain receipt.
    /// </summary>
    Task<NotificationOrderChainResponse> CreateOrder(NotificationOrderChainRequest order);

    /// <summary>
    /// Creates and immediately sends an SMS notification to a single recipient.
    /// </summary>
    Task<InstantNotificationOrderResponse> CreateInstantSmsOrder(InstantSmsNotificationOrderRequest order);

    /// <summary>
    /// Creates and immediately sends an email notification to a single recipient.
    /// </summary>
    Task<InstantNotificationOrderResponse> CreateInstantEmailOrder(InstantEmailNotificationOrderRequest order);

    /// <summary>
    /// Retrieves the processing status of a previously created notification order.
    /// </summary>
    Task<NotificationOrderWithStatus?> GetOrderStatus(Guid orderId);
}
