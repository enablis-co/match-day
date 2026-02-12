using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Models;

namespace Aggregator.Clients;

public class EventsClient : IEventsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventsClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EventsClient(HttpClient httpClient, ILogger<EventsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EventSummary> GetActiveEventsAsync(DateTime? time)
    {
        var url = time.HasValue
            ? $"/events/active?time={time.Value:O}"
            : "/events/active";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var activeEvents = json.GetProperty("activeEvents");
        var inMatchWindow = json.GetProperty("inMatchWindow").GetBoolean();

        if (!inMatchWindow || activeEvents.GetArrayLength() == 0)
        {
            return new EventSummary(
                Active: false,
                Current: null,
                DemandMultiplier: 1.0,
                EndsAt: null
            );
        }

        var first = activeEvents[0];
        var description = first.GetProperty("description").GetString();
        var demandMultiplier = first.GetProperty("demandMultiplier").GetDouble();
        var minutesRemaining = first.GetProperty("minutesRemaining").GetInt32();

        var endsAt = time.HasValue
            ? time.Value.AddMinutes(minutesRemaining)
            : DateTime.UtcNow.AddMinutes(minutesRemaining);

        return new EventSummary(
            Active: true,
            Current: description,
            DemandMultiplier: demandMultiplier,
            EndsAt: endsAt
        );
    }
}
