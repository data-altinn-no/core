namespace Dan.Common.Models;

/// <summary>
/// The response returned from reminder
/// </summary>
[DataContract]
public class NotificationReminder
{
    [DataMember(Name = "notificationType")]
    public string NotificationType { get; set; }

    [DataMember(Name = "success")]
    public bool Success { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "recipientCount")]
    public int RecipientCount { get; set; }

    [DataMember(Name = "date")]
    public DateTime Date { get; set; }
}