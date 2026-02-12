using Surge.Models;
using Surge.Services;
using Surge.Tests.Helpers;

namespace Surge.Tests;

public class WeatherSignalTests
{
    private readonly WeatherSignalCalculator _calculator = new();
    private readonly DateTime _baseDate = new(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NullWeatherData_ReturnsFallback()
    {
        var result = _calculator.Calculate(DateTime.UtcNow, null);
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void ColdDay_LowRain_ReturnsLowScore()
    {
        var weather = MockFactory.CreateWeatherData(_baseDate, temperature: 5.0, rainProbability: 10.0);
        var forecastHour = new DateTime(2026, 2, 12, 14, 0, 0, DateTimeKind.Utc);

        var result = _calculator.Calculate(forecastHour, weather);
        // tempScore = 5.0 / 5.0 = 1.0, no rain modifier, no sun bonus
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void WarmDay_LowRain_IncludesSunBonus()
    {
        var weather = MockFactory.CreateWeatherData(_baseDate, temperature: 22.0, rainProbability: 10.0);
        var forecastHour = new DateTime(2026, 2, 12, 14, 0, 0, DateTimeKind.Utc);

        var result = _calculator.Calculate(forecastHour, weather);
        // tempScore = 22/5 = 4.4, no rain modifier, sun bonus = 2.0
        Assert.Equal(6.4, result);
    }

    [Fact]
    public void RainyDay_HighProbability_AddsRainModifier()
    {
        var weather = MockFactory.CreateWeatherData(_baseDate, temperature: 10.0, rainProbability: 75.0);
        var forecastHour = new DateTime(2026, 2, 12, 14, 0, 0, DateTimeKind.Utc);

        var result = _calculator.Calculate(forecastHour, weather);
        // tempScore = 10/5 = 2.0, rain modifier = 3.0, no sun bonus
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void VeryWarmRainyDay_CapsAtTen()
    {
        var weather = MockFactory.CreateWeatherData(_baseDate, temperature: 30.0, rainProbability: 80.0);
        var forecastHour = new DateTime(2026, 2, 12, 14, 0, 0, DateTimeKind.Utc);

        var result = _calculator.Calculate(forecastHour, weather);
        // tempScore = capped at 5.0, rain modifier = 3.0, sun bonus = 0 (rainy)
        // 5 + 3 = 8, capped at 10
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void HourNotInData_ReturnsFallback()
    {
        var weather = MockFactory.CreateWeatherData(_baseDate, temperature: 10.0);
        var forecastHour = new DateTime(2026, 2, 13, 14, 0, 0, DateTimeKind.Utc); // Wrong day

        var result = _calculator.Calculate(forecastHour, weather);
        Assert.Equal(5.0, result);
    }
}
