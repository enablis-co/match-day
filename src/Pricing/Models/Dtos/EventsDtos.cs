using System.Text.Json.Serialization;

namespace Pricing.Models.Dtos;

public class EventsActiveResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("activeEvents")]
    public List<ActiveEvent> ActiveEvents { get; set; } = [];

    [JsonPropertyName("inMatchWindow")]
    public bool InMatchWindow { get; set; }
}

public class ActiveEvent
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("minutesRemaining")]
    public int MinutesRemaining { get; set; }

    [JsonPropertyName("demandMultiplier")]
    public double DemandMultiplier { get; set; }
}

public class DemandMultiplierResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}
