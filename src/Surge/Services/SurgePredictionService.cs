using Surge.Clients;
using Surge.Clients.Dtos;
using Surge.Models;

namespace Surge.Services;

public class SurgePredictionService : ISurgePredictionService
{
    private readonly IEventsClient _eventsClient;
    private readonly IWeatherClient _weatherClient;
    private readonly IEventSignalCalculator _eventSignalCalculator;
    private readonly IWeatherSignalCalculator _weatherSignalCalculator;
    private readonly ITimeOfDaySignalCalculator _timeOfDaySignalCalculator;
    private readonly ILogger<SurgePredictionService> _logger;

    private const double EventWeight = 0.40;
    private const double DemandWeight = 0.25;
    private const double WeatherWeight = 0.20;
    private const double TimeWeight = 0.15;

    public SurgePredictionService(
        IEventsClient eventsClient,
        IWeatherClient weatherClient,
        IEventSignalCalculator eventSignalCalculator,
        IWeatherSignalCalculator weatherSignalCalculator,
        ITimeOfDaySignalCalculator timeOfDaySignalCalculator,
        ILogger<SurgePredictionService> logger)
    {
        _eventsClient = eventsClient;
        _weatherClient = weatherClient;
        _eventSignalCalculator = eventSignalCalculator;
        _weatherSignalCalculator = weatherSignalCalculator;
        _timeOfDaySignalCalculator = timeOfDaySignalCalculator;
        _logger = logger;
    }

    public async Task<ForecastResponse> GetForecastAsync(string pubId, int hours)
    {
        // Fetch upstream data in parallel
        var eventsTask = _eventsClient.GetTodayEventsAsync();
        var demandTask = _eventsClient.GetDemandMultiplierAsync();
        var weatherTask = _weatherClient.GetHourlyWeatherAsync();

        await Task.WhenAll(eventsTask, demandTask, weatherTask);

        var todayEvents = eventsTask.Result;
        var demandMultiplier = demandTask.Result;
        var weatherData = weatherTask.Result;

        var eventsAvailable = todayEvents is not null;
        var weatherAvailable = weatherData is not null;

        var events = todayEvents?.Events ?? [];
        var multiplier = demandMultiplier?.Multiplier ?? 1.0;

        var confidence = DetermineConfidence(eventsAvailable, weatherAvailable);

        // Calculate demand signal: map multiplier to 0–10
        var demandSignal = Math.Min(multiplier * 2.5, 10.0);

        // Get current temperature and rain for the signal summary
        var now = DateTime.UtcNow;
        var currentTemp = GetCurrentTemperature(now, weatherData);
        var currentRain = GetCurrentRainProbability(now, weatherData);

        var forecastHours = new List<HourlyForecast>();
        var startHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < hours; i++)
        {
            var forecastTime = startHour.AddHours(i);
            var forecast = CalculateHourlyForecast(forecastTime, events, demandSignal, weatherData);
            forecastHours.Add(forecast);
        }

        return new ForecastResponse
        {
            PubId = pubId,
            GeneratedAt = now,
            Confidence = confidence,
            Signals = new SignalSummary
            {
                EventsAvailable = eventsAvailable,
                WeatherAvailable = weatherAvailable,
                ActiveEvents = events.Count,
                Temperature = currentTemp,
                RainProbability = currentRain
            },
            Forecast = forecastHours
        };
    }

    public async Task<PeakResponse> GetPeakAsync(string pubId, int hours = 8)
    {
        var forecast = await GetForecastAsync(pubId, hours);

        var peak = forecast.Forecast
            .OrderByDescending(f => f.SurgeScore)
            .FirstOrDefault();

        if (peak is null)
        {
            return new PeakResponse
            {
                PubId = pubId,
                PeakHour = "N/A",
                SurgeScore = 0,
                Intensity = SurgeIntensity.QUIET,
                Label = "No forecast data available",
                Confidence = forecast.Confidence
            };
        }

        return new PeakResponse
        {
            PubId = pubId,
            PeakHour = peak.Hour,
            SurgeScore = peak.SurgeScore,
            Intensity = peak.Intensity,
            Label = peak.Label,
            Confidence = forecast.Confidence
        };
    }

    private HourlyForecast CalculateHourlyForecast(
        DateTime forecastHour,
        List<EventDto> events,
        double demandSignal,
        WeatherData? weatherData)
    {
        var eventSignal = _eventSignalCalculator.Calculate(forecastHour, events);
        var weatherSignal = _weatherSignalCalculator.Calculate(forecastHour, weatherData);
        var timeSignal = _timeOfDaySignalCalculator.Calculate(forecastHour);

        var surgeScore = Math.Round(
            (eventSignal * EventWeight)
            + (demandSignal * DemandWeight)
            + (weatherSignal * WeatherWeight)
            + (timeSignal * TimeWeight),
            1);

        surgeScore = Math.Clamp(surgeScore, 0.0, 10.0);

        var intensity = ClassifyIntensity(surgeScore);
        var label = GenerateLabel(surgeScore, eventSignal, forecastHour, events);

        return new HourlyForecast
        {
            Hour = forecastHour.ToString("HH:00"),
            SurgeScore = surgeScore,
            Intensity = intensity,
            Label = label,
            Breakdown = new SurgeBreakdown
            {
                Event = Math.Round(eventSignal, 2),
                Demand = Math.Round(demandSignal, 2),
                Weather = Math.Round(weatherSignal, 2),
                TimeOfDay = Math.Round(timeSignal, 2)
            }
        };
    }

    private static SurgeIntensity ClassifyIntensity(double score) => score switch
    {
        <= 2.0 => SurgeIntensity.QUIET,
        <= 4.0 => SurgeIntensity.MODERATE,
        <= 6.0 => SurgeIntensity.BUSY,
        <= 8.0 => SurgeIntensity.HIGH,
        _ => SurgeIntensity.CRITICAL
    };

    private static string GenerateLabel(double surgeScore, double eventSignal, DateTime hour, List<EventDto> events)
    {
        foreach (var evt in events)
        {
            var minutesSinceKickoff = (hour - evt.Kickoff).TotalMinutes;
            var minutesToKickoff = (evt.Kickoff - hour).TotalMinutes;
            var minutesSinceEnd = (hour - evt.ExpectedEnd).TotalMinutes;

            // Half-time window
            if (minutesSinceKickoff >= 45 && minutesSinceKickoff <= 60)
                return "Half-time surge — all hands on deck";

            // During match (not half-time)
            if (minutesSinceKickoff >= 0 && hour <= evt.ExpectedEnd)
            {
                if (minutesSinceKickoff < 45)
                    return "First half — expect rush at the bar";
                return "Second half underway";
            }

            // Pre-match rush: 0–60 mins before
            if (minutesToKickoff is > 0 and <= 60)
                return "Kickoff approaching — rush imminent";

            // Pre-match build: 60–120 mins before
            if (minutesToKickoff is > 60 and <= 120)
                return "Pre-match build-up starting";

            // Early arrivals: 2+ hours before
            if (minutesToKickoff > 120)
                return "Early arrivals expected";

            // Post-match: 0–30 mins after end
            if (minutesSinceEnd is >= 0 and <= 30)
                return "Post-match surge";

            // Post-match wind-down: 30–90 mins after end
            if (minutesSinceEnd is > 30 and <= 90)
                return "Post-match wind-down";
        }

        return surgeScore switch
        {
            <= 2.0 => "Normal trading expected",
            <= 4.0 => "Slightly above baseline",
            <= 6.0 => "Noticeable demand increase",
            <= 8.0 => "Significant demand — prepare accordingly",
            _ => "Peak demand — all hands on deck"
        };
    }

    private static Confidence DetermineConfidence(bool eventsAvailable, bool weatherAvailable) =>
        (eventsAvailable, weatherAvailable) switch
        {
            (true, true) => Confidence.HIGH,
            (true, false) => Confidence.MEDIUM,
            (false, true) => Confidence.MEDIUM,
            (false, false) => Confidence.LOW
        };

    private static double? GetCurrentTemperature(DateTime now, WeatherData? weatherData)
    {
        if (weatherData is null) return null;

        var hourStr = now.ToString("yyyy-MM-ddTHH:00");
        var index = weatherData.Times.IndexOf(hourStr);
        return index >= 0 && index < weatherData.Temperatures.Count
            ? weatherData.Temperatures[index]
            : null;
    }

    private static double? GetCurrentRainProbability(DateTime now, WeatherData? weatherData)
    {
        if (weatherData is null) return null;

        var hourStr = now.ToString("yyyy-MM-ddTHH:00");
        var index = weatherData.Times.IndexOf(hourStr);
        return index >= 0 && index < weatherData.RainProbabilities.Count
            ? weatherData.RainProbabilities[index] / 100.0
            : null;
    }
}
