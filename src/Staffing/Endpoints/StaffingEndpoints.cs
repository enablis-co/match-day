namespace Staffing.Endpoints;

public static class StaffingEndpoints
{
    public static WebApplication MapStaffingEndpoints(this WebApplication app)
    {
        app.MapGet("/staffing/recommendation", () => Results.Ok(new
        {
            message = "Not implemented yet — this is your job!",
            hint = "Check the service spec for endpoint details",
            endpoints = new[] { "/staffing/recommendation", "/staffing/signals", "/staffing/history" }
        }))
        .WithTags("Staffing")
        .WithName("GetStaffingRecommendation")
        .WithOpenApi();

        app.MapGet("/staffing/signals", () => Results.Ok(new
        {
            message = "Not implemented yet — this is your job!",
            hint = "Check the service spec for endpoint details",
            endpoints = new[] { "/staffing/recommendation", "/staffing/signals", "/staffing/history" }
        }))
        .WithTags("Staffing")
        .WithName("GetStaffingSignals")
        .WithOpenApi();

        app.MapGet("/staffing/history", () => Results.Ok(new
        {
            message = "Not implemented yet — this is your job!",
            hint = "Check the service spec for endpoint details",
            endpoints = new[] { "/staffing/recommendation", "/staffing/signals", "/staffing/history" }
        }))
        .WithTags("Staffing")
        .WithName("GetStaffingHistory")
        .WithOpenApi();

        return app;
    }
}
