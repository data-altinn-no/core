namespace Dan.Common.Models;

/// <summary>
/// The response returned from reminder
/// </summary>
[DataContract]
public class NotificationReminder
{
    /// <summary>
    /// Type of notification reminder
    /// </summary>
    [DataMember(Name = "notificationType")]
    public string? NotificationType { get; set; }

    /// <summary>
    /// True if everything went ok with sending of notification
    /// </summary>
    [DataMember(Name = "success")]
    public bool Success { get; set; }

    /// <summary>
    /// Notification description
    /// </summary>
    [DataMember(Name = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Amount of recipients
    /// </summary>
    [DataMember(Name = "recipientCount")]
    public int RecipientCount { get; set; }

    /// <summary>
    /// When notification was sent
    /// </summary>
    [DataMember(Name = "date")]
    public DateTime Date { get; set; }
}