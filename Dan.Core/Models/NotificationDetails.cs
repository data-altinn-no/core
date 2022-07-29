namespace Dan.Core.Models;

/// <summary>
/// This class holds details for creating notifications for a correspondence.
/// </summary>
public class NotificationDetails
{
    /// <summary>
    /// Gets or sets the text to use in an SMS.
    /// </summary>
    public string SmsText { get; set; }

    /// <summary>
    ///  Gets or sets the text to use in the subject of an email.
    /// </summary>
    public string EmailSubject { get; set; }

    /// <summary>
    ///  Gets or sets the text to use in the body of an email.
    /// </summary>
    public string EmailBody { get; set; }
}
