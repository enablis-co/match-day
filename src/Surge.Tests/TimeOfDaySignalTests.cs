using Surge.Services;

namespace Surge.Tests;

public class TimeOfDaySignalTests
{
    private readonly TimeOfDaySignalCalculator _calculator = new();

    [Theory]
    [InlineData(3, 0.0)]   // 03:00 — late night
    [InlineData(7, 0.0)]   // 07:00 — closed/quiet
    [InlineData(10, 2.0)]  // 10:00 — morning trickle
    [InlineData(11, 2.0)]  // 11:00 — morning trickle
    [InlineData(12, 5.0)]  // 12:00 — lunch
    [InlineData(13, 5.0)]  // 13:00 — lunch
    [InlineData(14, 4.0)]  // 14:00 — afternoon
    [InlineData(15, 4.0)]  // 15:00 — afternoon
    [InlineData(16, 6.0)]  // 16:00 — after work
    [InlineData(17, 6.0)]  // 17:00 — after work
    [InlineData(18, 8.0)]  // 18:00 — evening peak
    [InlineData(20, 8.0)]  // 20:00 — evening peak
    [InlineData(21, 6.0)]  // 21:00 — wind-down
    [InlineData(22, 6.0)]  // 22:00 — wind-down
    [InlineData(23, 0.0)]  // 23:00 — late night
    public void ReturnsExpectedScore(int hour, double expectedScore)
    {
        var forecastHour = new DateTime(2026, 2, 12, hour, 0, 0, DateTimeKind.Utc);
        var result = _calculator.Calculate(forecastHour);
        Assert.Equal(expectedScore, result);
    }
}
