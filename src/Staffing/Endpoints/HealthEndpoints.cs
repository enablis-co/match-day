namespace Staffing.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "OK",
            service = "Staffing"
        }))
        .WithTags("Health")
        .WithName("HealthCheck")
        .WithOpenApi();

        return app;
    }
}
