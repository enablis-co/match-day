using Aggregator.Services;

namespace Aggregator.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", async (IAggregatorService aggregatorService) =>
        {
            var result = await aggregatorService.GetHealthAsync();
            return Results.Ok(result);
        })
        .WithTags("Health")
        .WithName("GetHealth")
        .WithOpenApi();
    }
}
