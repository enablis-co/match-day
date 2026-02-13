using Surge.Clients.Dtos;
using Surge.Services;
using Surge.Tests.Helpers;

namespace Surge.Tests;

public class OverlappingEventsTests
{
    private readonly EventSignalCalculator _calculator = new();

    [Fact]
    public void TwoEventsAtSameTime_CappedAtTen()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(-50); // Half-time for both
        var events = new List<EventDto>
        {
            MockFactory.CreateEvent(kickoff, eventId: "EVT-001"),
            MockFactory.CreateEvent(kickoff, eventId: "EVT-002")
        };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        // Each event would score 10.0 at half-time, combined = 20 but capped at 10
        Assert.Equal(10.0, result);
    }

    [Fact]
    public void TwoEventsStaggered_CombinesButCaps()
    {
        var now = DateTime.UtcNow;
        var events = new List<EventDto>
        {
            MockFactory.CreateEvent(now.AddMinutes(-10), eventId: "EVT-001"),   // During match = 8.0
            MockFactory.CreateEvent(now.AddMinutes(30), eventId: "EVT-002")     // Pre-match rush = 8.0
        };

        var result = _calculator.Calculate(now, events);
        // 8.0 + 8.0 = 16.0, capped at 10.0
        Assert.Equal(10.0, result);
    }

    [Fact]
    public void TwoEvents_BothLowSignal_CombinesBelow10()
    {
        var now = DateTime.UtcNow;
        var events = new List<EventDto>
        {
            MockFactory.CreateEvent(now.AddHours(3), eventId: "EVT-001"),    // Early arrivals = 2.0
            MockFactory.CreateEvent(now.AddHours(4), eventId: "EVT-002")     // Early arrivals = 2.0
        };

        var result = _calculator.Calculate(now, events);
        Assert.Equal(4.0, result);
    }
}
