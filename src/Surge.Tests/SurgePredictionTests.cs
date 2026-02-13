using Surge.Clients;
using Surge.Clients.Dtos;
using Surge.Models;
using Surge.Services;
using Surge.Tests.Helpers;
using MockFactory = Surge.Tests.Helpers.MockFactory;

namespace Surge.Tests;

public class SurgePredictionTests
{
    [Fact]
    public async Task KnownScenario_Arsenal14Kickoff_ProducesExpectedForecast()
    {
        // Arsenal kickoff at 14:00, 8°C, 15% rain
        var today = DateTime.UtcNow.Date;
        var kickoff = new DateTime(today.Year, today.Month, today.Day, 14, 0, 0, DateTimeKind.Utc);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff, demandMultiplier: 2.0) };
        var weather = MockFactory.CreateWeatherData(today, temperature: 8.5, rainProbability: 15.0);

        var eventsClient = MockFactory.CreateEventsClient(events, demandMultiplier: 2.0);
        var weatherClient = MockFactory.CreateWeatherClient(weather);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var result = await service.GetForecastAsync("PUB-001", 8);

        Assert.Equal("PUB-001", result.PubId);
        Assert.Equal(Confidence.HIGH, result.Confidence);
        Assert.True(result.Signals.EventsAvailable);
        Assert.True(result.Signals.WeatherAvailable);
        Assert.Equal(8, result.Forecast.Count);
    }

    [Fact]
    public async Task Peak_ReturnsSingleHighestHour()
    {
        var today = DateTime.UtcNow.Date;
        var kickoff = new DateTime(today.Year, today.Month, today.Day, 14, 0, 0, DateTimeKind.Utc);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff, demandMultiplier: 2.0) };
        var weather = MockFactory.CreateWeatherData(today, temperature: 8.5, rainProbability: 15.0);

        var eventsClient = MockFactory.CreateEventsClient(events, demandMultiplier: 2.0);
        var weatherClient = MockFactory.CreateWeatherClient(weather);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var peak = await service.GetPeakAsync("PUB-001");

        Assert.Equal("PUB-001", peak.PubId);
        Assert.True(peak.SurgeScore > 0);
        Assert.NotEmpty(peak.PeakHour);
        Assert.Equal(Confidence.HIGH, peak.Confidence);
    }

    [Fact]
    public async Task IntensityLabels_MatchThresholds()
    {
        var today = DateTime.UtcNow.Date;
        var events = new List<EventDto>();
        var weather = MockFactory.CreateWeatherData(today, temperature: 8.5, rainProbability: 15.0);

        var eventsClient = MockFactory.CreateEventsClient(events, demandMultiplier: 1.0);
        var weatherClient = MockFactory.CreateWeatherClient(weather);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var result = await service.GetForecastAsync("PUB-001", 24);

        // With no events, all scores should be moderate or below
        foreach (var hour in result.Forecast)
        {
            Assert.True(hour.SurgeScore is >= 0 and <= 10.0);
            var expectedIntensity = hour.SurgeScore switch
            {
                <= 2.0 => SurgeIntensity.QUIET,
                <= 4.0 => SurgeIntensity.MODERATE,
                <= 6.0 => SurgeIntensity.BUSY,
                <= 8.0 => SurgeIntensity.HIGH,
                _ => SurgeIntensity.CRITICAL
            };
            Assert.Equal(expectedIntensity, hour.Intensity);
        }
    }

    private static SurgePredictionService CreateService(
        IEventsClient eventsClient,
        IWeatherClient weatherClient)
    {
        return new SurgePredictionService(
            eventsClient,
            weatherClient,
            new EventSignalCalculator(),
            new WeatherSignalCalculator(),
            new TimeOfDaySignalCalculator(),
            MockFactory.CreateLogger<SurgePredictionService>());
    }
}
