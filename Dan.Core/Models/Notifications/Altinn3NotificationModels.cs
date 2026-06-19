using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dan.Core.Models.Notifications;

/// <summary>
/// Available notification channels in the Altinn Notifications API.
/// Serialized by name (e.g. "EmailAndSms").
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum NotificationChannel
{
    Email,
    Sms,
    EmailPreferred,
    SmsPreferred,
    EmailAndSms
}

/// <summary>
/// Content type for an email body.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EmailContentType
{
    Plain,
    Html
}

/// <summary>
/// Policy governing when a message may be delivered.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum SendingTimePolicy
{
    Anytime,
    Daytime
}

/// <summary>
/// Email-specific settings used when the channel scheme includes email.
/// </summary>
public class EmailSendingOptions
{
    [JsonProperty("senderEmailAddress", NullValueHandling = NullValueHandling.Ignore)]
    public string? SenderEmailAddress { get; set; }

    [JsonProperty("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;

    [JsonProperty("contentType")]
    public EmailContentType ContentType { get; set; } = EmailContentType.Plain;

    [JsonProperty("sendingTimePolicy", NullValueHandling = NullValueHandling.Ignore)]
    public SendingTimePolicy? SendingTimePolicy { get; set; }
}

/// <summary>
/// SMS-specific settings used when the channel scheme includes SMS.
/// </summary>
public class SmsSendingOptions
{
    [JsonProperty("sender", NullValueHandling = NullValueHandling.Ignore)]
    public string? Sender { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;

    [JsonProperty("sendingTimePolicy", NullValueHandling = NullValueHandling.Ignore)]
    public SendingTimePolicy? SendingTimePolicy { get; set; }
}

/// <summary>
/// Recipient targeted by national identity number; contact info is resolved from KRR.
/// </summary>
public class RecipientPerson
{
    [JsonProperty("nationalIdentityNumber")]
    public string NationalIdentityNumber { get; set; } = string.Empty;

    [JsonProperty("channelSchema")]
    public NotificationChannel ChannelSchema { get; set; }

    [JsonProperty("emailSettings", NullValueHandling = NullValueHandling.Ignore)]
    public EmailSendingOptions? EmailSettings { get; set; }

    [JsonProperty("smsSettings", NullValueHandling = NullValueHandling.Ignore)]
    public SmsSendingOptions? SmsSettings { get; set; }

    [JsonProperty("ignoreReservation")]
    public bool IgnoreReservation { get; set; }
}

/// <summary>
/// Recipient targeted by organization number; contact info is resolved from Enhetsregisteret.
/// </summary>
public class RecipientOrganization
{
    [JsonProperty("orgNumber")]
    public string OrgNumber { get; set; } = string.Empty;

    [JsonProperty("channelSchema")]
    public NotificationChannel ChannelSchema { get; set; }

    [JsonProperty("emailSettings", NullValueHandling = NullValueHandling.Ignore)]
    public EmailSendingOptions? EmailSettings { get; set; }

    [JsonProperty("smsSettings", NullValueHandling = NullValueHandling.Ignore)]
    public SmsSendingOptions? SmsSettings { get; set; }
}

/// <summary>
/// Container selecting exactly one recipient type for a notification order.
/// </summary>
public class NotificationRecipient
{
    [JsonProperty("recipientPerson", NullValueHandling = NullValueHandling.Ignore)]
    public RecipientPerson? RecipientPerson { get; set; }

    [JsonProperty("recipientOrganization", NullValueHandling = NullValueHandling.Ignore)]
    public RecipientOrganization? RecipientOrganization { get; set; }
}

/// <summary>
/// Request body for POST notifications/api/v1/future/orders.
/// </summary>
public class NotificationOrderChainRequest
{
    [JsonProperty("idempotencyId")]
    public string IdempotencyId { get; set; } = string.Empty;

    [JsonProperty("recipient")]
    public NotificationRecipient Recipient { get; set; } = new();

    [JsonProperty("sendersReference", NullValueHandling = NullValueHandling.Ignore)]
    public string? SendersReference { get; set; }

    [JsonProperty("requestedSendTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? RequestedSendTime { get; set; }

    [JsonProperty("conditionEndpoint", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? ConditionEndpoint { get; set; }
}

/// <summary>
/// Shipment tracking information returned for an order or reminder.
/// </summary>
public class NotificationOrderChainShipment
{
    [JsonProperty("shipmentId")]
    public Guid ShipmentId { get; set; }

    [JsonProperty("sendersReference")]
    public string? SendersReference { get; set; }
}

/// <summary>
/// Receipt for a created order chain, including any reminder shipments.
/// </summary>
public class NotificationOrderChainReceipt : NotificationOrderChainShipment
{
    [JsonProperty("reminders")]
    public List<NotificationOrderChainShipment>? Reminders { get; set; }
}

/// <summary>
/// Response from POST notifications/api/v1/future/orders.
/// </summary>
public class NotificationOrderChainResponse
{
    [JsonProperty("notificationOrderId")]
    public Guid OrderChainId { get; set; }

    [JsonProperty("notification")]
    public NotificationOrderChainReceipt? OrderChainReceipt { get; set; }
}

/// <summary>
/// Content and sender for an instant SMS.
/// </summary>
public class ShortMessageContent
{
    [JsonProperty("sender", NullValueHandling = NullValueHandling.Ignore)]
    public string? Sender { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Delivery details for an instant SMS.
/// </summary>
public class ShortMessageDeliveryDetails
{
    [JsonProperty("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonProperty("timeToLiveInSeconds")]
    public int TimeToLiveInSeconds { get; set; }

    [JsonProperty("smsSettings")]
    public ShortMessageContent ShortMessageContent { get; set; } = new();
}

/// <summary>
/// Request body for POST notifications/api/v1/future/orders/instant/sms.
/// </summary>
public class InstantSmsNotificationOrderRequest
{
    [JsonProperty("idempotencyId")]
    public string IdempotencyId { get; set; } = string.Empty;

    [JsonProperty("sendersReference", NullValueHandling = NullValueHandling.Ignore)]
    public string? SendersReference { get; set; }

    [JsonProperty("recipientSms")]
    public ShortMessageDeliveryDetails RecipientSms { get; set; } = new();
}

/// <summary>
/// Content and sender for an instant email.
/// </summary>
public class InstantEmailContent
{
    [JsonProperty("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;

    [JsonProperty("senderEmailAddress", NullValueHandling = NullValueHandling.Ignore)]
    public string? SenderEmailAddress { get; set; }

    [JsonProperty("contentType")]
    public EmailContentType ContentType { get; set; } = EmailContentType.Plain;
}

/// <summary>
/// Recipient and content for an instant email.
/// </summary>
public class InstantEmailDetails
{
    [JsonProperty("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;

    [JsonProperty("emailSettings")]
    public InstantEmailContent EmailSettings { get; set; } = new();
}

/// <summary>
/// Request body for POST notifications/api/v1/future/orders/instant/email.
/// </summary>
public class InstantEmailNotificationOrderRequest
{
    [JsonProperty("idempotencyId")]
    public string IdempotencyId { get; set; } = string.Empty;

    [JsonProperty("sendersReference", NullValueHandling = NullValueHandling.Ignore)]
    public string? SendersReference { get; set; }

    [JsonProperty("recipientEmail")]
    public InstantEmailDetails RecipientEmail { get; set; } = new();
}

/// <summary>
/// Response from the instant order endpoints.
/// </summary>
public class InstantNotificationOrderResponse
{
    [JsonProperty("notificationOrderId")]
    public Guid OrderChainId { get; set; }

    [JsonProperty("notification")]
    public NotificationOrderChainShipment? Notification { get; set; }
}

/// <summary>
/// Status summary for a single notification channel.
/// </summary>
public class NotificationChannelStatus
{
    [JsonProperty("generated")]
    public int Generated { get; set; }

    [JsonProperty("succeeded")]
    public int Succeeded { get; set; }
}

/// <summary>
/// Summary of per-channel notification statuses.
/// </summary>
public class NotificationsStatusSummary
{
    [JsonProperty("email")]
    public NotificationChannelStatus? Email { get; set; }

    [JsonProperty("sms")]
    public NotificationChannelStatus? Sms { get; set; }
}

/// <summary>
/// Processing status of a notification order.
/// </summary>
public class OrderProcessingStatus
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? StatusDescription { get; set; }

    [JsonProperty("lastUpdate")]
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Response from GET notifications/api/v1/orders/{id}/status.
/// </summary>
public class NotificationOrderWithStatus
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("sendersReference")]
    public string? SendersReference { get; set; }

    [JsonProperty("requestedSendTime")]
    public DateTime RequestedSendTime { get; set; }

    [JsonProperty("creator")]
    public string Creator { get; set; } = string.Empty;

    [JsonProperty("created")]
    public DateTime Created { get; set; }

    [JsonProperty("notificationChannel")]
    public NotificationChannel NotificationChannel { get; set; }

    [JsonProperty("processingStatus")]
    public OrderProcessingStatus ProcessingStatus { get; set; } = new();

    [JsonProperty("notificationsStatusSummary")]
    public NotificationsStatusSummary? NotificationsStatusSummary { get; set; }
}
