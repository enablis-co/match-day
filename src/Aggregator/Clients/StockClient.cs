using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Models;

namespace Aggregator.Clients;

public class StockClient : IStockClient
{
    private static readonly ActivitySource ActivitySource = new("Aggregator.Clients.StockClient");

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
        using var activity = ActivitySource.StartActivity("StockClient.GetStockAlerts");
        activity?.SetTag("pub.id", pubId);
        activity?.SetTag("http.url", url);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Calling Stock service: {Url} for pub {PubId}", url, pubId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url);
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            _logger.LogError(ex,
                "Stock service request failed for pub {PubId} after {ElapsedMs}ms: {ErrorMessage}",
                pubId, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }

        activity?.SetTag("http.status_code", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            sw.Stop();
            var responseBody = await response.Content.ReadAsStringAsync();
            activity?.SetTag("error", true);
            activity?.SetTag("http.response_body", responseBody);
            _logger.LogError(
                "Stock service returned {StatusCode} for pub {PubId} after {ElapsedMs}ms. Response: {ResponseBody}",
                (int)response.StatusCode, pubId, sw.ElapsedMilliseconds, responseBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogDebug("Stock service returned {StatusCode} for pub {PubId} after {ElapsedMs}ms",
            (int)response.StatusCode, pubId, sw.ElapsedMilliseconds);

        JsonElement json;
        try
        {
            json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        }
        catch (JsonException ex)
        {
            sw.Stop();
            var rawBody = await response.Content.ReadAsStringAsync();
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", "JsonDeserializationFailed");
            _logger.LogError(ex,
                "Failed to deserialise Stock service response for pub {PubId}. Raw body: {ResponseBody}",
                pubId, rawBody);
            throw;
        }

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

        sw.Stop();
        activity?.SetTag("stock.alert_count", alertCount);
        activity?.SetTag("stock.critical_item_count", criticalItems.Count);
        _logger.LogInformation(
            "Stock service call completed for pub {PubId} in {ElapsedMs}ms — {AlertCount} alerts, {CriticalItemCount} critical items",
            pubId, sw.ElapsedMilliseconds, alertCount, criticalItems.Count);

        return new StockSummary(
            AlertCount: alertCount,
            CriticalItems: criticalItems,
            EstimatedShortfall: estimatedShortfall
        );
    }
}
