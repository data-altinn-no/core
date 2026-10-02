using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Models.Events;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Services;

/// <inheritdoc />
public class ConsentEventPublisher : IConsentEventPublisher
{
    /// <summary>
    /// Subject URN form Altinn Events uses for organizations in generic events and in subscription subjectFilter
    /// </summary>
    public const string OrganizationSubjectUrnPrefix = "urn:altinn:organization:identifier-no:";

    private const string ResourceUrnPrefix = "urn:altinn:resource:";

    private readonly IAltinnEventsService _altinnEventsService;
    private readonly ILogger<ConsentEventPublisher> _logger;

    public ConsentEventPublisher(IAltinnEventsService altinnEventsService, ILogger<ConsentEventPublisher> logger)
    {
        _altinnEventsService = altinnEventsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => Settings.ConsentEventsEnabled;

    /// <inheritdoc />
    public bool Enqueue(Accreditation accreditation, string eventType)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (accreditation.ConsentEvents.Any(e => e.Type == eventType))
        {
            return false;
        }

        accreditation.ConsentEvents.Add(new ConsentEventRecord
        {
            EventId = ConsentEventRecord.CreateEventId(accreditation.AccreditationId, eventType),
            Type = eventType,
            Created = DateTime.UtcNow
        });

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryPublishPending(Accreditation accreditation)
    {
        if (!IsEnabled)
        {
            return false;
        }

        var maxAttempts = Settings.ConsentEventsMaxAttempts;
        var pending = accreditation.ConsentEvents.Where(e => !e.Published && e.Attempts < maxAttempts).ToList();
        if (pending.Count == 0)
        {
            return false;
        }

        foreach (var record in pending)
        {
            record.Attempts++;
            record.LastAttempt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(accreditation.Owner))
            {
                record.LastError = "Accreditation has no owner, cannot determine event subject";
                _logger.LogWarning("Cannot publish consent event without owner aid={accreditationId} type={type}",
                    accreditation.AccreditationId, record.Type);
                continue;
            }

            try
            {
                var cloudEvent = BuildEvent(accreditation, record);
                await _altinnEventsService.Publish(cloudEvent, record.EventId);

                record.Published = true;
                record.PublishedAt = DateTime.UtcNow;
                record.LastError = null;

                _logger.LogInformation("Consent event published aid={accreditationId} type={type} eventId={eventId} attempt={attempt}",
                    accreditation.AccreditationId, record.Type, record.EventId, record.Attempts);
                _logger.DanLog(accreditation, LogAction.ConsentEventPublished);
            }
            catch (Exception ex)
            {
                record.LastError = ex.Message;
                _logger.LogWarning(ex, "Consent event publish failed aid={accreditationId} type={type} eventId={eventId} attempt={attempt}",
                    accreditation.AccreditationId, record.Type, record.EventId, record.Attempts);
                _logger.DanLog(accreditation, LogAction.ConsentEventPublishFailed);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public AltinnCloudEvent BuildEvent(Accreditation accreditation, ConsentEventRecord record)
    {
        return new AltinnCloudEvent
        {
            Id = record.EventId,
            Type = record.Type,
            Source = Settings.ConsentEventsSource,
            Subject = OrganizationSubjectUrnPrefix + accreditation.Owner,
            Resource = ResourceUrnPrefix + Settings.ConsentEventsResourceId,
            ResourceInstance = accreditation.AccreditationId,
            Time = record.Created.ToUniversalTime(),
            Data = new ConsentEventData
            {
                AccreditationId = accreditation.AccreditationId,
                Status = GetStatus(record.Type),
                ServiceContext = accreditation.ServiceContext,
                ConsentReference = accreditation.ConsentReference,
                ExternalReference = accreditation.ExternalReference,
                ValidTo = accreditation.ValidTo
            }
        };
    }

    /// <summary>
    /// The last segment of the event type is the consent status: granted, denied or expired
    /// </summary>
    private static string GetStatus(string eventType)
    {
        return eventType[(eventType.LastIndexOf('.') + 1)..];
    }
}
