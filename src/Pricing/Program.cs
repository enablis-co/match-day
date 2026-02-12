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

// Register repositories and services with interfaces (DIP)
builder.Services.AddSingleton<IOfferRepository, OfferRepository>();
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IEventsService, EventsService>();
builder.Services.AddScoped<IOfferEvaluationService, OfferEvaluationService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IMatchWindowService, MatchWindowService>();

// ─── Build and Configure App ──────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ─── Map Endpoints ────────────────────────────────────────────────────────────

app.MapHealthEndpoints();
app.MapOffersEndpoints();
app.MapPricingEndpoints();

app.Run();
