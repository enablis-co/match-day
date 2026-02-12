using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Models;

namespace Aggregator.Clients;

public class StaffingClient : IStaffingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StaffingClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StaffingClient(HttpClient httpClient, ILogger<StaffingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StaffingSummary> GetRecommendationAsync(string pubId, DateTime? time)
    {
        var url = $"/staffing/recommendation?pubId={pubId}";
        if (time.HasValue)
        {
            url += $"&time={time.Value:O}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var recommendation = json.GetProperty("recommendation");
        var action = recommendation.GetProperty("action").GetString() ?? "MAINTAIN";
        var additionalStaff = recommendation.GetProperty("additionalStaff").GetInt32();
        var urgency = recommendation.GetProperty("urgency").GetString() ?? "LOW";

        var confidence = json.GetProperty("confidence").GetString() ?? "LOW";

        return new StaffingSummary(
            Recommendation: action,
            AdditionalRequired: additionalStaff,
            Urgency: urgency,
            Confidence: confidence
        );
    }
}
