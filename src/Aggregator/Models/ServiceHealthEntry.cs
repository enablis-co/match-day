using System.Text.Json.Serialization;

namespace Aggregator.Models;

public record ServiceHealthEntry(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ServiceStatus Status,
    long? Latency
);
