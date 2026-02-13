using Surge.Clients.Dtos;
using Surge.Services;
using Surge.Tests.Helpers;

namespace Surge.Tests;

public class EventSignalTests
{
    private readonly EventSignalCalculator _calculator = new();

    [Fact]
    public void NoEvents_ReturnsZero()
    {
        var result = _calculator.Calculate(DateTime.UtcNow, []);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void MoreThanTwoHoursBeforeKickoff_ReturnsEarlyArrivals()
    {
        var kickoff = DateTime.UtcNow.AddHours(3);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(2.0, result);
    }

    [Fact]
    public void NinetyMinutesBeforeKickoff_ReturnsPreMatchBuild()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(90);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void ThirtyMinutesBeforeKickoff_ReturnsRush()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(30);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void ExactlyAtKickoff_ReturnsRush()
    {
        var now = DateTime.UtcNow;
        var events = new List<EventDto> { MockFactory.CreateEvent(now) };

        var result = _calculator.Calculate(now, events);
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void AtKickoff_ReturnsRush()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(-10);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void HalfTimeWindow_ReturnsPeak()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(-50);
        var events = new List<EventDto> { MockFactory.CreateEvent(kickoff) };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(10.0, result);
    }

    [Fact]
    public void PostMatch_ThirtyMinutesAfter_ReturnsSurge()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(-140);
        var evt = MockFactory.CreateEvent(kickoff);
        // 140 mins since kickoff, event is 120 mins, so 20 mins post-match
        var events = new List<EventDto> { evt };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(7.0, result);
    }

    [Fact]
    public void PostMatch_SixtyMinutesAfter_ReturnsWindDown()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(-180);
        var evt = MockFactory.CreateEvent(kickoff);
        // 180 mins since kickoff, event is 120 mins, so 60 mins post-match
        var events = new List<EventDto> { evt };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(4.0, result);
    }

    [Fact]
    public void PostMatch_BeyondNinetyMinutes_ReturnsZero()
    {
        var kickoff = DateTime.UtcNow.AddMinutes(-240);
        var evt = MockFactory.CreateEvent(kickoff);
        // 240 mins since kickoff, event is 120 mins, so 120 mins post-match
        var events = new List<EventDto> { evt };

        var result = _calculator.Calculate(DateTime.UtcNow, events);
        Assert.Equal(0.0, result);
    }
}
