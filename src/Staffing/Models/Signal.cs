using System.Text.Json.Serialization;

namespace Staffing.Models;

public class Signal
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SignalSource Source { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public object? RawValue { get; set; }
}
