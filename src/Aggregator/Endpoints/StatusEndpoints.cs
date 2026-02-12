using Aggregator.Services;

namespace Aggregator.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app)
    {
        app.MapGet("/status/{pubId}", async (string pubId, DateTime? time, IAggregatorService aggregatorService) =>
        {
            var result = await aggregatorService.GetStatusAsync(pubId, time);
            return Results.Ok(result);
        })
        .WithTags("Status")
        .WithName("GetStatus")
        .WithOpenApi();

        app.MapGet("/status/{pubId}/summary", async (string pubId, DateTime? time, IAggregatorService aggregatorService) =>
        {
            var result = await aggregatorService.GetSummaryAsync(pubId, time);
            return Results.Ok(result);
        })
        .WithTags("Status")
        .WithName("GetStatusSummary")
        .WithOpenApi();

        app.MapGet("/status/{pubId}/actions", async (string pubId, DateTime? time, IAggregatorService aggregatorService) =>
        {
            var result = await aggregatorService.GetActionsAsync(pubId, time);
            return Results.Ok(result);
        })
        .WithTags("Status")
        .WithName("GetStatusActions")
        .WithOpenApi();
    }
}
