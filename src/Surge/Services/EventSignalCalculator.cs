using Surge.Clients.Dtos;

namespace Surge.Services;

public class EventSignalCalculator : IEventSignalCalculator
{
    public double Calculate(DateTime forecastHour, List<EventDto> events)
    {
        if (events.Count == 0)
            return 0.0;

        var combinedSignal = 0.0;

        foreach (var evt in events)
        {
            var signal = CalculateForEvent(forecastHour, evt);
            combinedSignal += signal;
        }

        return Math.Min(combinedSignal, 10.0);
    }

    private static double CalculateForEvent(DateTime forecastHour, EventDto evt)
    {
        var minutesToKickoff = (evt.Kickoff - forecastHour).TotalMinutes;
        var minutesSinceKickoff = (forecastHour - evt.Kickoff).TotalMinutes;
        var matchDurationMinutes = (evt.ExpectedEnd - evt.Kickoff).TotalMinutes;
        var minutesSinceEnd = (forecastHour - evt.ExpectedEnd).TotalMinutes;

        // Early arrivals: 2+ hours before kickoff
        if (minutesToKickoff > 120)
            return 2.0;

        // Pre-match build: 60–120 minutes before
        if (minutesToKickoff is > 60 and <= 120)
            return 5.0;

        // Pre-match rush: 0–60 minutes before
        if (minutesToKickoff is > 0 and <= 60)
            return 8.0;

        // During match: kickoff to expected end
        if (minutesSinceKickoff >= 0 && forecastHour <= evt.ExpectedEnd)
        {
            // Half-time window: kickoff + 45–60 minutes
            if (minutesSinceKickoff >= 45 && minutesSinceKickoff <= 60)
                return 10.0;

            // Rush around kickoff (0–60 mins into match)
            return 8.0;
        }

        // Post-match: 0–30 minutes after end
        if (minutesSinceEnd is >= 0 and <= 30)
            return 7.0;

        // Post-match wind-down: 30–90 minutes after end
        if (minutesSinceEnd is > 30 and <= 90)
            return 4.0;

        // Beyond 90 minutes after end
        return 0.0;
    }
}
