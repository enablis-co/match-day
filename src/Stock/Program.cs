var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Stock & Forecasting Service",
        Version = "v1",
        Description = "Match Day Mode — Inventory levels and consumption predictions"
    });
});

var app = builder.Build();

var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:8080";

app.UseSwagger();
app.UseSwaggerUI();

var stubResponse = new
{
    message = "Not implemented yet — this is your job!",
    hint = "Check the service spec for endpoint details",
    endpoints = new[]
    {
        "/stock/current",
        "/stock/{productId}/forecast",
        "/stock/alerts",
        "POST /stock/consumption"
    }
};

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "Stock" }))
    .WithTags("Health")
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapGet("/stock/current", () => Results.Ok(stubResponse))
    .WithTags("Stock")
    .WithName("GetCurrentStock")
    .WithOpenApi();

app.MapGet("/stock/{productId}/forecast", (string productId) => Results.Ok(stubResponse))
    .WithTags("Forecast")
    .WithName("GetProductForecast")
    .WithOpenApi();

app.MapGet("/stock/alerts", () => Results.Ok(stubResponse))
    .WithTags("Stock")
    .WithName("GetStockAlerts")
    .WithOpenApi();

app.MapPost("/stock/consumption", () => Results.Ok(stubResponse))
    .WithTags("Stock")
    .WithName("RecordConsumption")
    .WithOpenApi();

app.Run();
