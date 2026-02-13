namespace Aggregator.Models;

public record SurgeForecast(
    string PubId,
    DateTime GeneratedAt,
    string Confidence,
    SurgeSignals Signals,
    List<SurgeHourlyEntry> Forecast
);

public record SurgeSignals(
    bool EventsAvailable,
    bool WeatherAvailable,
    int ActiveEvents,
    double? Temperature,
    double? RainProbability
);

public record SurgeHourlyEntry(
    string Hour,
    double SurgeScore,
    string Intensity,
    string Label
);
