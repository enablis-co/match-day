using Microsoft.Extensions.Caching.Memory;
using Surge.Clients.Dtos;
using Surge.Models;

namespace Surge.Clients;

public class WeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherClient> _logger;
    private readonly IMemoryCache _cache;

    private const string CacheKey = "weather_hourly_data";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public WeatherClient(HttpClient httpClient, ILogger<WeatherClient> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<WeatherData?> GetHourlyWeatherAsync()
    {
        if (_cache.TryGetValue(CacheKey, out WeatherData? cached) && cached is not null)
        {
            _logger.LogInformation("Returning cached weather data");
            return cached;
        }

        try
        {
            var url = "/v1/forecast?latitude=51.5&longitude=-0.1&hourly=temperature_2m,precipitation_probability&forecast_days=1";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var meteoResponse = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>();

            if (meteoResponse?.Hourly is null)
            {
                _logger.LogWarning("Open-Meteo returned null hourly data");
                return null;
            }

            var weatherData = new WeatherData
            {
                Times = meteoResponse.Hourly.Time,
                Temperatures = meteoResponse.Hourly.Temperature_2m,
                RainProbabilities = meteoResponse.Hourly.Precipitation_probability
            };

            _cache.Set(CacheKey, weatherData, CacheDuration);
            _logger.LogInformation("Fetched live weather data, cached for {Duration} minutes", CacheDuration.TotalMinutes);
            return weatherData;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch weather data from Open-Meteo — using fallback");
            return null;
        }
    }
}
