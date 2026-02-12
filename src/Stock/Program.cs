using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stock.Configuration;
using Stock.Data;
using Stock.Enums;
using Stock.Models;
using Stock.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<EventsServiceOptions>(
    builder.Configuration.GetSection(EventsServiceOptions.SectionName));

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

// Add in-memory database
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseInMemoryDatabase("StockDb"));

// Add services
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<IStockQueryService>(sp => sp.GetRequiredService<StockService>());
builder.Services.AddScoped<IStockCommandService>(sp => sp.GetRequiredService<StockService>());
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IForecastService, ForecastService>();
builder.Services.AddScoped<IAlertSeverityStrategy, DefaultAlertSeverityStrategy>();
builder.Services.AddScoped<IForecastConfidenceStrategy, DefaultForecastConfidenceStrategy>();
builder.Services.AddHttpClient<IDemandMultiplierService, DemandMultiplierService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<EventsServiceOptions>>().Value;
    var baseUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? options.BaseUrl;
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = options.Timeout;
});

var app = builder.Build();

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "Stock" }))
    .WithTags("Health")
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapGet("/stock/current", async (string pubId, string? category, IStockQueryService stockQueryService) =>
{
    var stock = await stockQueryService.GetCurrentStockAsync(pubId, category);
    return Results.Ok(new
    {
        pubId,
        timestamp = DateTime.UtcNow,
        stock = stock.Select(s => new
        {
            productId = s.ProductId,
            productName = s.Product?.ProductName,
            category = s.Product?.Category,
            currentLevel = s.CurrentLevel,
            unit = s.Product?.Unit,
            status = s.Status
        })
    });
})
    .WithTags("Stock")
    .WithName("GetCurrentStock")
    .WithOpenApi();

app.MapGet("/stock/{productId}/forecast", async (string productId, string pubId, int hours, IForecastService forecastService) =>
{
    var forecast = await forecastService.GetForecastAsync(pubId, productId, hours);
    return forecast != null ? Results.Ok(forecast) : Results.NotFound();
})
    .WithTags("Forecast")
    .WithName("GetProductForecast")
    .WithOpenApi();

app.MapGet("/stock/alerts", async (string pubId, double? threshold, string? severity, IAlertService alertService) =>
{
    IEnumerable<StockAlert> alerts;
    
    if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AlertSeverity>(severity, true, out var severityEnum))
    {
        alerts = await alertService.GetAlertsBySeverityAsync(pubId, severityEnum);
    }
    else
    {
        alerts = await alertService.GetStockAlertsAsync(pubId, threshold ?? 12);
    }
    
    return Results.Ok(new
    {
        pubId,
        timestamp = DateTime.UtcNow,
        alertCount = alerts.Count(),
        alerts
    });
})
    .WithTags("Alerts")
    .WithName("GetStockAlerts")
    .WithOpenApi();

app.MapGet("/stock/alerts/critical", async (string pubId, IAlertService alertService) =>
{
    var alerts = await alertService.GetCriticalAlertsAsync(pubId);
    return Results.Ok(new
    {
        pubId,
        timestamp = DateTime.UtcNow,
        criticalCount = alerts.Count(),
        alerts
    });
})
    .WithTags("Alerts")
    .WithName("GetCriticalAlerts")
    .WithOpenApi();

app.MapPost("/stock/consumption", async (ConsumptionRequest request, IStockService stockService) =>
{
    var success = await stockService.RecordConsumptionAsync(request.PubId, request.ProductId, request.Amount);
    return success ? Results.Ok(new { message = "Consumption recorded" }) : Results.NotFound();
})
    .WithTags("Stock")
    .WithName("RecordConsumption")
    .WithOpenApi();

app.Run();
