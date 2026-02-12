using System.Text.Json.Serialization;
using Surge.Clients;
using Surge.Endpoints;
using Surge.Services;

var builder = WebApplication.CreateBuilder(args);

// Read upstream service URLs from environment
var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:5001";

// Register HTTP clients
builder.Services.AddHttpClient<IEventsClient, EventsClient>(client =>
{
    client.BaseAddress = new Uri(eventsServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IWeatherClient, WeatherClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com");
    client.Timeout = TimeSpan.FromSeconds(3);
});

// Register memory cache for weather data
builder.Services.AddMemoryCache();

// Register services
builder.Services.AddScoped<IEventSignalCalculator, EventSignalCalculator>();
builder.Services.AddScoped<IWeatherSignalCalculator, WeatherSignalCalculator>();
builder.Services.AddScoped<ITimeOfDaySignalCalculator, TimeOfDaySignalCalculator>();
builder.Services.AddScoped<ISurgePredictionService, SurgePredictionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Surge Predictor Service",
        Version = "v1",
        Description = "Match Day Mode — Demand curve prediction engine with hourly surge forecasts"
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Surge Predictor Service v1");
});

app.MapHealthEndpoints();
app.MapSurgeEndpoints();

app.Run();
