using Staffing.Services;

namespace Staffing.Endpoints;

public static class StaffingEndpoints
{
    public static WebApplication MapStaffingEndpoints(this WebApplication app)
    {
        app.MapGet("/staffing/recommendation", async (
            string pubId,
            DateTime? time,
            IStaffingService staffingService) =>
        {
            var recommendation = await staffingService.GetRecommendationAsync(pubId, time);
            return Results.Ok(recommendation);
        })
        .WithTags("Staffing")
        .WithName("GetStaffingRecommendation")
        .WithOpenApi();

        app.MapGet("/staffing/signals", async (
            string pubId,
            DateTime? time,
            IStaffingService staffingService) =>
        {
            var signals = await staffingService.GetSignalsAsync(pubId, time);
            return Results.Ok(signals);
        })
        .WithTags("Staffing")
        .WithName("GetStaffingSignals")
        .WithOpenApi();

        app.MapGet("/staffing/history", (
            string pubId,
            int? days,
            IStaffingService staffingService) =>
        {
            var history = staffingService.GetHistory(pubId, days ?? 7);
            return Results.Ok(history);
        })
        .WithTags("Staffing")
        .WithName("GetStaffingHistory")
        .WithOpenApi();

        return app;
    }
}
