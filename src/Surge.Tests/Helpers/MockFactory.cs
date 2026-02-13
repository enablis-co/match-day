using Moq;
using Microsoft.Extensions.Logging;
using Surge.Clients;
using Surge.Clients.Dtos;
using Surge.Models;
using Surge.Services;

namespace Surge.Tests.Helpers;

public static class MockFactory
{
    public static Mock<IEventsClient> CreateEventsClient(
        List<EventDto>? events = null,
        double demandMultiplier = 1.0,
        bool returnsNull = false)
    {
        var mock = new Mock<IEventsClient>();

        if (returnsNull)
        {
            mock.Setup(x => x.GetTodayEventsAsync())
                .ReturnsAsync((TodayEventsResponse?)null);
            mock.Setup(x => x.GetDemandMultiplierAsync())
                .ReturnsAsync((DemandMultiplierResponse?)null);
            return mock;
        }

        mock.Setup(x => x.GetTodayEventsAsync())
            .ReturnsAsync(new TodayEventsResponse
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Events = events ?? []
            });

        mock.Setup(x => x.GetDemandMultiplierAsync())
            .ReturnsAsync(new DemandMultiplierResponse
            {
                Timestamp = DateTime.UtcNow,
                Multiplier = demandMultiplier,
                Reason = "Test scenario"
            });

        return mock;
    }

    public static Mock<IWeatherClient> CreateWeatherClient(
        WeatherData? weatherData = null,
        bool returnsNull = false)
    {
        var mock = new Mock<IWeatherClient>();

        if (returnsNull)
        {
            mock.Setup(x => x.GetHourlyWeatherAsync())
                .ReturnsAsync((WeatherData?)null);
            return mock;
        }

        mock.Setup(x => x.GetHourlyWeatherAsync())
            .ReturnsAsync(weatherData ?? new WeatherData());

        return mock;
    }

    public static EventDto CreateEvent(
        DateTime kickoff,
        double demandMultiplier = 2.0,
        string eventId = "EVT-001",
        int durationMinutes = 120)
    {
        return new EventDto
        {
            EventId = eventId,
            Sport = "football",
            Competition = "Six Nations",
            HomeTeam = "England",
            AwayTeam = "France",
            Kickoff = kickoff,
            ExpectedEnd = kickoff.AddMinutes(durationMinutes),
            DemandMultiplier = demandMultiplier
        };
    }

    public static WeatherData CreateWeatherData(
        DateTime baseDate,
        double temperature = 8.5,
        double rainProbability = 15.0)
    {
        var times = new List<string>();
        var temps = new List<double>();
        var rains = new List<double>();

        for (var hour = 0; hour < 24; hour++)
        {
            times.Add(new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hour, 0, 0)
                .ToString("yyyy-MM-ddTHH:00"));
            temps.Add(temperature);
            rains.Add(rainProbability);
        }

        return new WeatherData
        {
            Times = times,
            Temperatures = temps,
            RainProbabilities = rains
        };
    }

    public static ILogger<T> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }
}
