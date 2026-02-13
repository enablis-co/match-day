using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Models;

namespace Aggregator.Clients;

public class SurgeClient : ISurgeClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SurgeClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SurgeClient(HttpClient httpClient, ILogger<SurgeClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SurgeSummary?> GetPeakAsync(string pubId)
    {
        try
        {
            var url = $"/surge/peak?pubId={pubId}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            return new SurgeSummary(
                PeakHour: json.GetProperty("peakHour").GetString() ?? "N/A",
                SurgeScore: json.GetProperty("surgeScore").GetDouble(),
                Intensity: json.GetProperty("intensity").GetString() ?? "QUIET",
                Label: json.GetProperty("label").GetString() ?? "",
                Confidence: json.GetProperty("confidence").GetString() ?? "LOW"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Surge peak call failed");
            return null;
        }
    }

    public async Task<SurgeForecast?> GetForecastAsync(string pubId, int hours = 12)
    {
        try
        {
            var url = $"/surge/forecast?pubId={pubId}&hours={hours}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            var signals = json.GetProperty("signals");
            var forecastArray = json.GetProperty("forecast");

            var entries = new List<SurgeHourlyEntry>();
            foreach (var item in forecastArray.EnumerateArray())
            {
                entries.Add(new SurgeHourlyEntry(
                    Hour: item.GetProperty("hour").GetString() ?? "",
                    SurgeScore: item.GetProperty("surgeScore").GetDouble(),
                    Intensity: item.GetProperty("intensity").GetString() ?? "QUIET",
                    Label: item.GetProperty("label").GetString() ?? ""
                ));
            }

            return new SurgeForecast(
                PubId: json.GetProperty("pubId").GetString() ?? pubId,
                GeneratedAt: json.GetProperty("generatedAt").GetDateTime(),
                Confidence: json.GetProperty("confidence").GetString() ?? "LOW",
                Signals: new SurgeSignals(
                    EventsAvailable: signals.GetProperty("eventsAvailable").GetBoolean(),
                    WeatherAvailable: signals.GetProperty("weatherAvailable").GetBoolean(),
                    ActiveEvents: signals.GetProperty("activeEvents").GetInt32(),
                    Temperature: signals.TryGetProperty("temperature", out var temp) && temp.ValueKind != JsonValueKind.Null
                        ? temp.GetDouble() : null,
                    RainProbability: signals.TryGetProperty("rainProbability", out var rain) && rain.ValueKind != JsonValueKind.Null
                        ? rain.GetDouble() : null
                ),
                Forecast: entries
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Surge forecast call failed");
            return null;
        }
    }
}
