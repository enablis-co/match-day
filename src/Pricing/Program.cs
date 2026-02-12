using Pricing.Data;
using Pricing.Endpoints;
using Pricing.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Logging & Tracing ────────────────────────────────────────────────────────

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Enable Activity tracing for diagnostics
AppContext.SetSwitch("System.Diagnostics.ActivityListener.SuppressIsEnabledCheck", true);

var listener = new System.Diagnostics.ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData
};
System.Diagnostics.ActivitySource.AddActivityListener(listener);

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

// Register base offer evaluation service
builder.Services.AddScoped<IOfferEvaluationService, OfferEvaluationService>();

// Wrap with tracing decorator
builder.Services.Decorate<IOfferEvaluationService, TracedOfferEvaluationService>();

builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IMatchWindowService, MatchWindowService>();

// Register tracing services
builder.Services.AddScoped<IOfferOperationTracer, OfferOperationTracer>();

// ─── Build and Configure App ──────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ─── Map Endpoints ────────────────────────────────────────────────────────────

app.MapHealthEndpoints();
app.MapOffersEndpoints();
app.MapPricingEndpoints();

app.Run();
