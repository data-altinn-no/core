using Dan.Common;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Models.Events;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Dan.Core.Services;

/// <summary>
/// Publishes CloudEvents to the Altinn 3 Events API (generic/external events). Authenticates with an
/// Altinn-exchanged Maskinporten token exactly like <see cref="Altinn3NotificationsService"/>.
/// See https://docs.altinn.studio/en/events/publish-events/developer-guides/publish-events/
/// </summary>
public class AltinnEventsService : IAltinnEventsService
{
    private const string CloudEventsContentType = "application/cloudevents+json";
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenRequesterService _tokenRequesterService;
    private readonly ILogger<AltinnEventsService> _logger;

    public AltinnEventsService(
        IHttpClientFactory httpClientFactory,
        ITokenRequesterService tokenRequesterService,
        ILogger<AltinnEventsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenRequesterService = tokenRequesterService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Publish(AltinnCloudEvent cloudEvent, string idempotencyKey)
    {
        var client = _httpClientFactory.CreateClient(Constants.AltinnEventsHttpClient);

        var baseUrl = Settings.EventsApiBaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/events");

        // The Events API requires an Altinn token, so exchange the Maskinporten token first.
        var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            await _tokenRequesterService.GetAltinnExchangedToken(Settings.EventsPublishScope));
        if (tokenResponse == null || !tokenResponse.TryGetValue(Constants.ACCESS_TOKEN, out var accessToken))
        {
            throw new ServiceNotAvailableException("Temporarily unable to retrieve authentication token for the Altinn Events API");
        }

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation(IdempotencyKeyHeader, idempotencyKey);
        request.Content = new StringContent(JsonConvert.SerializeObject(cloudEvent), Encoding.UTF8, CloudEventsContentType);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Altinn Events publish failed eventId={eventId} type={type} statusCode={statusCode} reasonPhrase={reasonPhrase}",
                cloudEvent.Id, cloudEvent.Type, response.StatusCode, response.ReasonPhrase);
            throw new ServiceNotAvailableException($"The Altinn Events API returned {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        _logger.LogInformation("Published event to Altinn Events eventId={eventId} type={type} resourceInstance={resourceInstance}",
            cloudEvent.Id, cloudEvent.Type, cloudEvent.ResourceInstance);
    }
}
