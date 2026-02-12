using Aggregator.Clients;
using Aggregator.Endpoints;
using Aggregator.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Service URLs ─────────────────────────────────────────────────────────────

var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:5001";
var pricingServiceUrl = Environment.GetEnvironmentVariable("PRICING_SERVICE_URL") ?? "http://localhost:5002";
var stockServiceUrl = Environment.GetEnvironmentVariable("STOCK_SERVICE_URL") ?? "http://localhost:5003";
var staffingServiceUrl = Environment.GetEnvironmentVariable("STAFFING_SERVICE_URL") ?? "http://localhost:5004";

// ─── Swagger ──────────────────────────────────────────────────────────────────

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

// ─── HTTP Clients ─────────────────────────────────────────────────────────────

builder.Services.AddHttpClient<IEventsClient, EventsClient>(client =>
{
    client.BaseAddress = new Uri(eventsServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IPricingClient, PricingClient>(client =>
{
    client.BaseAddress = new Uri(pricingServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IStockClient, StockClient>(client =>
{
    client.BaseAddress = new Uri(stockServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IStaffingClient, StaffingClient>(client =>
{
    client.BaseAddress = new Uri(staffingServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ─── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddScoped<IRiskCalculator, RiskCalculator>();
builder.Services.AddScoped<IActionPrioritiser, ActionPrioritiser>();
builder.Services.AddScoped<IAggregatorService, AggregatorService>();

// ─── Build and Configure App ──────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ─── Map Endpoints ────────────────────────────────────────────────────────────

app.MapStatusEndpoints();
app.MapHealthEndpoints();

app.Run();
