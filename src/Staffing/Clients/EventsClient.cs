using Staffing.Clients.Dtos;

namespace Staffing.Clients;

public interface IEventsClient
{
    Task<ActiveEventsResponse?> GetActiveEventsAsync(DateTime? time = null);
    Task<DemandMultiplierResponse?> GetDemandMultiplierAsync(DateTime? time = null);
}

public class EventsClient : IEventsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventsClient> _logger;

    public EventsClient(HttpClient httpClient, ILogger<EventsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ActiveEventsResponse?> GetActiveEventsAsync(DateTime? time = null)
    {
        try
        {
            var url = "/events/active";
            if (time.HasValue)
                url += $"?time={time.Value:O}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ActiveEventsResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch active events from Events Service");
            return null;
        }
    }

    public async Task<DemandMultiplierResponse?> GetDemandMultiplierAsync(DateTime? time = null)
    {
        try
        {
            var url = "/events/demand-multiplier";
            if (time.HasValue)
                url += $"?time={time.Value:O}";

            var response = await _httpClient.GetAsync(url);
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
