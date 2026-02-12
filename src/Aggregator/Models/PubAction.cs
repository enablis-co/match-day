using System.Text.Json.Serialization;

namespace Aggregator.Models;

public record PubAction(
    int Priority,
    string Action,
    string Reason,
    DateTime? Deadline,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ActionSource Source
);
