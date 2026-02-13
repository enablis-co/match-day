using Surge.Models;

namespace Surge.Clients;

public interface IWeatherClient
{
    Task<WeatherData?> GetHourlyWeatherAsync();
}
