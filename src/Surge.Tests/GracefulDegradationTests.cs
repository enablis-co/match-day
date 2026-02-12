using Surge.Clients;
using Surge.Clients.Dtos;
using Surge.Models;
using Surge.Services;
using Surge.Tests.Helpers;
using MockFactory = Surge.Tests.Helpers.MockFactory;

namespace Surge.Tests;

public class GracefulDegradationTests
{
    [Fact]
    public async Task EventsUnavailable_WeatherAvailable_MediumConfidence()
    {
        var today = DateTime.UtcNow.Date;
        var weather = MockFactory.CreateWeatherData(today, temperature: 10.0, rainProbability: 20.0);

        var eventsClient = MockFactory.CreateEventsClient(returnsNull: true);
        var weatherClient = MockFactory.CreateWeatherClient(weather);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var result = await service.GetForecastAsync("PUB-001", 4);

        Assert.Equal(Confidence.MEDIUM, result.Confidence);
        Assert.False(result.Signals.EventsAvailable);
        Assert.True(result.Signals.WeatherAvailable);
        Assert.Equal(4, result.Forecast.Count);
    }

    [Fact]
    public async Task EventsAvailable_WeatherUnavailable_MediumConfidence()
    {
        var today = DateTime.UtcNow.Date;
        var kickoff = new DateTime(today.Year, today.Month, today.Day, 14, 0, 0, DateTimeKind.Utc);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };

        var eventsClient = MockFactory.CreateEventsClient(events, demandMultiplier: 2.0);
        var weatherClient = MockFactory.CreateWeatherClient(returnsNull: true);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var result = await service.GetForecastAsync("PUB-001", 4);

        Assert.Equal(Confidence.MEDIUM, result.Confidence);
        Assert.True(result.Signals.EventsAvailable);
        Assert.False(result.Signals.WeatherAvailable);
    }

    [Fact]
    public async Task BothUnavailable_LowConfidence_StillReturnsForecast()
    {
        var eventsClient = MockFactory.CreateEventsClient(returnsNull: true);
        var weatherClient = MockFactory.CreateWeatherClient(returnsNull: true);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var result = await service.GetForecastAsync("PUB-001", 4);

        Assert.Equal(Confidence.LOW, result.Confidence);
        Assert.False(result.Signals.EventsAvailable);
        Assert.False(result.Signals.WeatherAvailable);
        Assert.Equal(4, result.Forecast.Count);
        // Should still have time-of-day and default signals
        Assert.All(result.Forecast, f => Assert.True(f.SurgeScore >= 0));
    }

    [Fact]
    public async Task BothAvailable_HighConfidence()
    {
        var today = DateTime.UtcNow.Date;
        var kickoff = new DateTime(today.Year, today.Month, today.Day, 14, 0, 0, DateTimeKind.Utc);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };
        var weather = MockFactory.CreateWeatherData(today, temperature: 10.0);

        var eventsClient = MockFactory.CreateEventsClient(events, demandMultiplier: 2.0);
        var weatherClient = MockFactory.CreateWeatherClient(weather);

        var service = CreateService(eventsClient.Object, weatherClient.Object);

        var result = await service.GetForecastAsync("PUB-001", 4);

        Assert.Equal(Confidence.HIGH, result.Confidence);
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
