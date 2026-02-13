using Surge.Clients.Dtos;

namespace Surge.Clients;

public class EventsClient : IEventsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventsClient> _logger;

    public EventsClient(HttpClient httpClient, ILogger<EventsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TodayEventsResponse?> GetTodayEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/events/today");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TodayEventsResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch today's events from Events Service");
            return null;
        }
    }

    public async Task<DemandMultiplierResponse?> GetDemandMultiplierAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/events/demand-multiplier");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<DemandMultiplierResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch demand multiplier from Events Service");
            return null;
        }
    }
}
