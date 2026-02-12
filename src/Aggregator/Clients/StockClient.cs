using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Models;

namespace Aggregator.Clients;

public class StockClient : IStockClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StockClient(HttpClient httpClient, ILogger<StockClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StockSummary> GetStockAlertsAsync(string pubId)
    {
        var url = $"/stock/alerts?pubId={pubId}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var alertCount = json.GetProperty("alertCount").GetInt32();
        var alerts = json.GetProperty("alerts");

        var criticalItems = new List<string>();
        string? estimatedShortfall = null;

        foreach (var alert in alerts.EnumerateArray())
        {
            var severity = alert.GetProperty("severity").GetString();
            if (string.Equals(severity, "CRITICAL", StringComparison.OrdinalIgnoreCase))
            {
                if (alert.TryGetProperty("productName", out var name))
                {
                    criticalItems.Add(name.GetString() ?? "Unknown");
                }
            }

            if (alert.TryGetProperty("estimatedDepletionTime", out var depletionTime)
                && depletionTime.ValueKind != JsonValueKind.Null)
            {
                var depletion = depletionTime.GetDateTime();
                var timeOnly = depletion.ToString("HH:mm");
                if (estimatedShortfall == null ||
                    string.Compare(timeOnly, estimatedShortfall, StringComparison.Ordinal) < 0)
                {
                    estimatedShortfall = timeOnly;
                }
            }
        }

        return new StockSummary(
            AlertCount: alertCount,
            CriticalItems: criticalItems,
            EstimatedShortfall: estimatedShortfall
        );
    }
}
