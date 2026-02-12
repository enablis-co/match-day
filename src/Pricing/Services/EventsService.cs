using System.Net.Http.Json;
using Pricing.Models.Dtos;

namespace Pricing.Services;

public class EventsService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EventsService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<EventsActiveResponse?> GetActiveEventsAsync(DateTime? time = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("EventsService");
            var url = time.HasValue
                ? $"/events/active?time={time.Value:O}"
                : "/events/active";
            return await client.GetFromJsonAsync<EventsActiveResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DemandMultiplierResponse?> GetDemandMultiplierAsync(DateTime? time = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("EventsService");
            var url = time.HasValue
                ? $"/events/demand-multiplier?time={time.Value:O}"
                : "/events/demand-multiplier";
            return await client.GetFromJsonAsync<DemandMultiplierResponse>(url);
        }
        catch
        {
            return null;
        }
    }
}
