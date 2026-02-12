using System.Text.Json.Serialization;

namespace Staffing.Models;

public class StaffingRecommendation
{
    public string PubId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public RecommendationDetail Recommendation { get; set; } = new();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Confidence Confidence { get; set; }

    public List<Signal> Signals { get; set; } = [];
}

public class RecommendationDetail
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StaffingAction Action { get; set; }

    public int AdditionalStaff { get; set; }

    public List<string> Roles { get; set; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Urgency Urgency { get; set; }

    public DateTime WindowStart { get; set; }

    public DateTime WindowEnd { get; set; }
}
