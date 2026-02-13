using Surge.Clients;

namespace Surge.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", async (IEventsClient eventsClient, IWeatherClient weatherClient) =>
        {
            var eventsStatus = "DOWN";
            var weatherStatus = "DOWN";

            try
            {
                var events = await eventsClient.GetTodayEventsAsync();
                if (events is not null) eventsStatus = "UP";
            }
            catch { /* already handled */ }

            try
            {
                var weather = await weatherClient.GetHourlyWeatherAsync();
                if (weather is not null) weatherStatus = "UP";
            }
            catch { /* already handled */ }

            return Results.Ok(new
            {
                status = "OK",
                service = "Surge Predictor",
                dependencies = new
                {
                    events = eventsStatus,
                    weather = weatherStatus
                }
            });
        })
        .WithTags("Health")
        .WithName("HealthCheck")
        .WithOpenApi();

        return app;
    }
}
