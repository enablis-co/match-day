namespace Aggregator.Models;

public record SurgeSummary(
    string PeakHour,
    double SurgeScore,
    string Intensity,
    string Label,
    string Confidence
);
