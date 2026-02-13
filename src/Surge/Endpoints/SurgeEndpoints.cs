using Surge.Services;

namespace Surge.Endpoints;

public static class SurgeEndpoints
{
    public static WebApplication MapSurgeEndpoints(this WebApplication app)
    {
        app.MapGet("/surge/forecast", async (
            string? pubId,
            int? hours,
            ISurgePredictionService predictionService) =>
        {
            var effectivePubId = pubId ?? "PUB-001";
            var effectiveHours = hours ?? 8;

            var forecast = await predictionService.GetForecastAsync(effectivePubId, effectiveHours);
            return Results.Ok(forecast);
        })
        .WithTags("Surge")
        .WithName("GetSurgeForecast")
        .WithOpenApi();

        app.MapGet("/surge/peak", async (
            string? pubId,
            int? hours,
            ISurgePredictionService predictionService) =>
        {
            var effectivePubId = pubId ?? "PUB-001";
            var effectiveHours = hours ?? 8;

            var peak = await predictionService.GetPeakAsync(effectivePubId, effectiveHours);
            return Results.Ok(peak);
        })
        .WithTags("Surge")
        .WithName("GetPeakSurge")
        .WithOpenApi();

        return app;
    }
}
