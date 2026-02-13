using Surge.Models;

namespace Surge.Services;

public class WeatherSignalCalculator : IWeatherSignalCalculator
{
    private const double FallbackScore = 5.0;

    public double Calculate(DateTime forecastHour, WeatherData? weatherData)
    {
        if (weatherData is null)
            return FallbackScore;

        var (temperature, rainProbability) = GetWeatherForHour(forecastHour, weatherData);

        if (temperature is null)
            return FallbackScore;

        // Temperature mapped to 0–5 (warmer = more footfall)
        // Assume 0°C → 0, 25°C+ → 5
        var tempScore = Math.Clamp(temperature.Value / 5.0, 0.0, 5.0);

        // Rain modifier: >60% probability adds +3
        var rainModifier = (rainProbability ?? 0) > 60 ? 3.0 : 0.0;

        // Sunny + warm bonus: low rain (<30%) and warm (>18°C) adds +2
        var sunBonus = (rainProbability ?? 100) < 30 && temperature.Value > 18 ? 2.0 : 0.0;

        return Math.Min(tempScore + rainModifier + sunBonus, 10.0);
    }

    private static (double? Temperature, double? RainProbability) GetWeatherForHour(
        DateTime forecastHour, WeatherData weatherData)
    {
        var targetHourStr = forecastHour.ToString("yyyy-MM-ddTHH:00");

        for (var i = 0; i < weatherData.Times.Count; i++)
        {
            if (weatherData.Times[i] == targetHourStr)
            {
                var temp = i < weatherData.Temperatures.Count ? weatherData.Temperatures[i] : (double?)null;
                var rain = i < weatherData.RainProbabilities.Count ? weatherData.RainProbabilities[i] : (double?)null;
                return (temp, rain);
            }
        }

        return (null, null);
    }
}
