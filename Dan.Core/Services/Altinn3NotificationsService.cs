using Dan.Common;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Models.Notifications;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dan.Core.Services;

/// <summary>
/// Sends notifications via the Altinn 3 Notifications API. Authenticates with a raw
/// Maskinporten token (reusing <see cref="ITokenRequesterService"/>), exactly like
/// <see cref="Altinn3ConsentService"/> does for the consent API.
/// </summary>
public class Altinn3NotificationsService : IAltinn3NotificationsService
{
    private const string FromAddress = "no-reply@altinn.no";
    private const string ResourceUrnPrefix = "urn:altinn:resource:";
    private const string ResourceAction = "read";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenRequesterService _tokenRequesterService;
    private readonly Interfaces.IEntityRegistryService _entityRegistryService;
    private readonly ILogger<Altinn3NotificationsService> _logger;

    public Altinn3NotificationsService(
        IHttpClientFactory httpClientFactory,
        ITokenRequesterService tokenRequesterService,
        Interfaces.IEntityRegistryService entityRegistryService,
        ILogger<Altinn3NotificationsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenRequesterService = tokenRequesterService;
        _entityRegistryService = entityRegistryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<NotificationReminder>> SendReminder(Accreditation accreditation, ServiceContext serviceContext)
    {
        var order = await BuildReminderOrder(accreditation, serviceContext);

        try
        {
            var response = await CreateOrder(order);
            _logger.LogInformation(
                "Sent consent reminder notification order aid={accreditationId} notificationOrderId={notificationOrderId}",
                accreditation.AccreditationId, response.OrderChainId);         

            return new List<NotificationReminder>
            {
                CreateReminderReceipt("Sent", true, "Email"),
                CreateReminderReceipt("Sent", true, "SMS")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to send consent reminder notification order aid={accreditationId} exception={exception}",
                accreditation.AccreditationId, ex.Message);

            return new List<NotificationReminder>
            {
                CreateReminderReceipt("Failed", false, "Email"),
                CreateReminderReceipt("Failed", false, "SMS")
            };
        }
    }

    /// <inheritdoc />
    public async Task<NotificationOrderChainResponse> CreateOrder(NotificationOrderChainRequest order)
    {
        return await Send<NotificationOrderChainResponse>(HttpMethod.Post, "future/orders", order);
    }

    /// <inheritdoc />
    public async Task<InstantNotificationOrderResponse> CreateInstantSmsOrder(InstantSmsNotificationOrderRequest order)
    {
        return await Send<InstantNotificationOrderResponse>(HttpMethod.Post, "future/orders/instant/sms", order);
    }

    /// <inheritdoc />
    public async Task<InstantNotificationOrderResponse> CreateInstantEmailOrder(InstantEmailNotificationOrderRequest order)
    {
        return await Send<InstantNotificationOrderResponse>(HttpMethod.Post, "future/orders/instant/email", order);
    }

    /// <inheritdoc />
    public async Task<NotificationOrderWithStatus?> GetOrderStatus(Guid orderId)
    {
        return await Send<NotificationOrderWithStatus?>(HttpMethod.Get, $"orders/{orderId}/status", null, HttpStatusCode.NotFound);
    }

    private async Task<NotificationOrderChainRequest> BuildReminderOrder(Accreditation accreditation, ServiceContext serviceContext)
    {
        if (accreditation.RequestorParty == null)
        {
            throw new InvalidRequestorException("Accreditation missing requestor party");
        }

        if (accreditation.SubjectParty == null)
        {
            throw new InvalidSubjectException("Accreditation missing subject party");
        }

        var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);
        var subjectName = await GetPartyDisplayName(accreditation.SubjectParty);

        var renderedTexts = TextTemplateProcessor.GetRenderedTexts(serviceContext, accreditation, requestorName, subjectName, null);

        var emailSettings = new EmailSendingOptions
        {
            //defaults to noreply@altinn.no
            //SenderEmailAddress = FromAddress,
            Subject = renderedTexts.EmailNotificationSubject,
            Body = renderedTexts.EmailNotificationContent,
            ContentType = EmailContentType.Plain
        };

        var smsSettings = new SmsSendingOptions
        {
            Body = renderedTexts.SMSNotificationContent
        };

        var resourceId = GetNotificationResourceUrn(accreditation);

        var recipient = BuildRecipient(accreditation.SubjectParty, emailSettings, smsSettings, resourceId);

        return new NotificationOrderChainRequest
        {
            // Idempotency id must be unique per send; reminders may be sent repeatedly (>7 days apart).
            IdempotencyId = $"dan-reminder-{accreditation.AccreditationId}-{Guid.NewGuid():N}",
            SendersReference = accreditation.AccreditationId,
            //RequestedSendTime = DateTime.UtcNow.AddMinutes(1),
            Recipient = recipient
        };
    }

    private static NotificationRecipient BuildRecipient(Party subjectParty, EmailSendingOptions emailSettings, SmsSendingOptions smsSettings, string resourceId)
    {
        if (!string.IsNullOrWhiteSpace(subjectParty.NorwegianOrganizationNumber))
        {
            return new NotificationRecipient
            {
                RecipientOrganization = new RecipientOrganization
                {
                    OrgNumber = subjectParty.NorwegianOrganizationNumber,
                    ChannelSchema = NotificationChannel.EmailAndSms,
                    EmailSettings = emailSettings,
                    SmsSettings = smsSettings,
                    ResourceId = resourceId,
                    ResourceAction = ResourceAction
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(subjectParty.NorwegianSocialSecurityNumber))
        {
            return new NotificationRecipient
            {
                RecipientPerson = new RecipientPerson
                {
                    NationalIdentityNumber = subjectParty.NorwegianSocialSecurityNumber,
                    ChannelSchema = NotificationChannel.EmailAndSms,
                    EmailSettings = emailSettings,
                    SmsSettings = smsSettings,
                    ResourceId = resourceId,
                    ResourceAction = ResourceAction
                }
            };
        }

        throw new InvalidSubjectException(
            "Cannot send reminder: subject party has neither a Norwegian organization number nor a national identity number");
    }

    /// <summary>
    /// Resolves the Altinn 3 consent resource URN ("urn:altinn:resource:{id}") from the consent
    /// requirement on the accreditation's evidence codes. The Notifications API uses this to target
    /// the correct recipients, so it must be present (the A3 consent request enforces the same).
    /// </summary>
    private static string GetNotificationResourceUrn(Accreditation accreditation)
    {
        /* var resourceIdentifier = accreditation.EvidenceCodes
             .SelectMany(ec => ec.AuthorizationRequirements.OfType<ConsentRequirement>())
             .Select(cr => cr.AltinnResource)
             .FirstOrDefault(r => !string.IsNullOrEmpty(r)); 


         if (string.IsNullOrEmpty(resourceIdentifier))
         {
             throw new InternalServerErrorException(
                 "Cannot send reminder: no Altinn resource is defined on the consent requirement for the accreditation");
         } */

        return ResourceUrnPrefix + Settings.AltinnMessageResource;
    }

    private async Task<string> GetPartyDisplayName(Party party)
    {
        if (party.NorwegianOrganizationNumber == null)
        {
            // Party.ToString() masks the social security number.
            return party.ToString();
        }

        var result = await _entityRegistryService.Get(party.NorwegianOrganizationNumber);
        return result?.Name ?? party.NorwegianOrganizationNumber;
    }

    private static NotificationReminder CreateReminderReceipt(string description, bool success, string type)
    {
        return new NotificationReminder
        {
            Description = description,
            Success = success,
            NotificationType = type,
            RecipientCount = success ? 1 : 0,
            Date = DateTime.Now
        };
    }

    /// <summary>
    /// Builds an authenticated request to the Notifications API, sends it and deserializes the response.
    /// Throws <see cref="ServiceNotAvailableException"/> on unexpected (non-allowed) error responses.
    /// </summary>
    private async Task<T> Send<T>(HttpMethod method, string relativePath, object? body, params HttpStatusCode[] allowedErrorCodes)
    {
        var client = _httpClientFactory.CreateClient(Constants.Altinn3NotificationsHttpClient);

        var baseUrl = Settings.NotificationsApiBaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{baseUrl}/{relativePath}");

        // The Notifications API requires an Altinn token, so exchange the Maskinporten token first.
        var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            await _tokenRequesterService.GetAltinnExchangedToken(Settings.NotificationsCreateScope));
        if (tokenResponse == null || !tokenResponse.TryGetValue(Constants.ACCESS_TOKEN, out var accessToken))
        {
            throw new ServiceNotAvailableException("Temporarily unable to retrieve authentication token for the Notifications API");
        }

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        if (body != null)
        {
            // JsonContent forces the method to POST, so set the content directly for non-POST verbs.
            request.Content = new System.Net.Http.StringContent(
                JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
        }

        if (allowedErrorCodes.Length > 0)
        {
            request.SetAllowedErrorCodes(allowedErrorCodes);
        }

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (allowedErrorCodes.Contains(response.StatusCode))
            {
                // Caller-handled error (e.g. 404 on status lookup) — return default.
                return default!;
            }

            _logger.LogError(
                "Notifications API call failed method={method} path={path} statusCode={statusCode} reasonPhrase={reasonPhrase}",
                method, relativePath, response.StatusCode, response.ReasonPhrase);
            throw new ServiceNotAvailableException("The Altinn Notifications API returned an error. This is an internal error, please contact support");
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<T>(content);
        if (result == null)
        {
            _logger.LogError(
                "Notifications API returned success but the response could not be deserialized method={method} path={path}",
                method, relativePath);
            throw new ServiceNotAvailableException("Unexpected response from the Altinn Notifications API");
        }

        return result;
    }
}
