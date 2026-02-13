namespace Surge.Models;

public class HourlyForecast
{
    public string Hour { get; set; } = string.Empty;

    public double SurgeScore { get; set; }

    public SurgeIntensity Intensity { get; set; }

    public string Label { get; set; } = string.Empty;

    public SurgeBreakdown Breakdown { get; set; } = new();
}
