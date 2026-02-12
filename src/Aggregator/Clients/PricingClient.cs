using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Models;

namespace Aggregator.Clients;

public class PricingClient : IPricingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PricingClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PricingClient(HttpClient httpClient, ILogger<PricingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PricingSummary> GetActiveOffersAsync(string pubId, DateTime? time)
    {
        var url = $"/offers/active?pubId={pubId}";
        if (time.HasValue)
        {
            url += $"&time={time.Value:O}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var activeOffers = json.GetProperty("activeOffers");
        var suspendedOffers = json.GetProperty("suspendedOffers");

        var activeCount = activeOffers.GetArrayLength();
        var suspendedCount = suspendedOffers.GetArrayLength();

        string? suspensionReason = null;
        if (suspendedCount > 0)
        {
            var firstSuspended = suspendedOffers[0];
            if (firstSuspended.TryGetProperty("reason", out var reasonProp))
            {
                suspensionReason = reasonProp.GetString();
            }
        }

        return new PricingSummary(
            OffersActive: activeCount,
            OffersSuspended: suspendedCount,
            SuspensionReason: suspensionReason
        );
    }
}
