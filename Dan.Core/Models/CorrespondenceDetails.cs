namespace Dan.Core.Models;

/// <summary>
/// This class holds the details needed to create a new correspondence in Altinn.
/// </summary>
public class CorrespondenceDetails
{
    /// <summary>
    /// Gets or sets the correspondence recipient. This should be an organization number or social security number.
    /// </summary>
    public string Reportee { get; set; }

    /// <summary>
    /// Gets or sets the title of the correspondence.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the correspondence summary. 
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// Gets or sets the main body of the correspondence.
    /// </summary>
    public string Body { get; set; }

    /// <summary>
    /// Gets or sets the sender of the correspondence.
    /// </summary>
    public string Sender { get; set; }

    /// <summary>
    /// Gets or sets the details needed to create notifications.
    /// </summary>
    public NotificationDetails Notification { get; set; }
}
