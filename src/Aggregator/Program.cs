var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pub Status Aggregator",
        Version = "v1",
        Description = "Match Day Mode — Unified pub operational status"
    });
});

var app = builder.Build();

// Read upstream service URLs from environment (stubs don't use them yet)
var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:5001";
var pricingServiceUrl = Environment.GetEnvironmentVariable("PRICING_SERVICE_URL") ?? "http://localhost:5002";
var stockServiceUrl = Environment.GetEnvironmentVariable("STOCK_SERVICE_URL") ?? "http://localhost:5003";
var staffingServiceUrl = Environment.GetEnvironmentVariable("STAFFING_SERVICE_URL") ?? "http://localhost:5004";

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new
{
    status = "OK",
    service = "Aggregator"
}))
.WithTags("Health")
.WithName("GetHealth")
.WithOpenApi();

app.MapGet("/status/{pubId}", (string pubId) => Results.Ok(new
{
    message = "Not implemented yet — this is your job!",
    hint = "Check the service spec for endpoint details",
    endpoints = new[] { "/status/{pubId}", "/status/{pubId}/summary", "/status/{pubId}/actions" }
}))
.WithTags("Status")
.WithName("GetStatus")
.WithOpenApi();

app.MapGet("/status/{pubId}/summary", (string pubId) => Results.Ok(new
{
    message = "Not implemented yet — this is your job!",
    hint = "Check the service spec for endpoint details",
    endpoints = new[] { "/status/{pubId}", "/status/{pubId}/summary", "/status/{pubId}/actions" }
}))
.WithTags("Status")
.WithName("GetStatusSummary")
.WithOpenApi();

app.MapGet("/status/{pubId}/actions", (string pubId) => Results.Ok(new
{
    message = "Not implemented yet — this is your job!",
    hint = "Check the service spec for endpoint details",
    endpoints = new[] { "/status/{pubId}", "/status/{pubId}/summary", "/status/{pubId}/actions" }
}))
.WithTags("Status")
.WithName("GetStatusActions")
.WithOpenApi();

app.Run();
