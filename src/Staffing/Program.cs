var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Staffing Signal Service",
        Version = "v1",
        Description = "Match Day Mode — Staffing recommendations based on demand signals"
    });
});

var app = builder.Build();

// Read upstream service URLs from environment (stubs don't use them yet)
var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:5001";
var stockServiceUrl = Environment.GetEnvironmentVariable("STOCK_SERVICE_URL") ?? "http://localhost:5002";

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Staffing Signal Service v1");
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "OK",
    service = "Staffing"
}))
.WithTags("Health")
.WithName("HealthCheck")
.WithOpenApi();

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

app.Run();
