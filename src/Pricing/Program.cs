using Pricing.Data;
using Pricing.Endpoints;
using Pricing.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pricing & Offers Service",
        Version = "v1",
        Description = "Match Day Mode — Promotional pricing and offer rules"
    });
});

var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:8080";
builder.Services.AddHttpClient("EventsService", client =>
{
    client.BaseAddress = new Uri(eventsServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Register repositories and services
builder.Services.AddSingleton<OfferRepository>();
builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddScoped<EventsService>();
builder.Services.AddScoped<OfferEvaluationService>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<PricingService>();

// ─── Build and Configure App ──────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ─── Map Endpoints ────────────────────────────────────────────────────────────

app.MapHealthEndpoints();
app.MapOffersEndpoints();
app.MapPricingEndpoints();

app.Run();
