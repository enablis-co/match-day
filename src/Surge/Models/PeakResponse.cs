namespace Surge.Models;

public class PeakResponse
{
    public string PubId { get; set; } = string.Empty;

    public string PeakHour { get; set; } = string.Empty;

    public double SurgeScore { get; set; }

    public SurgeIntensity Intensity { get; set; }

    public string Label { get; set; } = string.Empty;

    public Confidence Confidence { get; set; }
}
