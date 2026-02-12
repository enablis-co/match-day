using Surge.Models;

namespace Surge.Services;

public interface IWeatherSignalCalculator
{
    double Calculate(DateTime forecastHour, WeatherData? weatherData);
}
