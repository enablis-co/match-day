namespace Surge.Models;

public class ForecastResponse
{
    public string PubId { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public Confidence Confidence { get; set; }

    public SignalSummary Signals { get; set; } = new();

    public List<HourlyForecast> Forecast { get; set; } = [];
}
