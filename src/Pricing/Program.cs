var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:8080";

app.UseSwagger();
app.UseSwaggerUI();

var stubResponse = new
{
    message = "Not implemented yet — this is your job!",
    hint = "Check the service spec for endpoint details",
    endpoints = new[] { "/offers/active", "/offers/{offerId}", "/pricing/current", "/pricing/match-day-status" }
};

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "Pricing" }))
    .WithTags("Health");

app.MapGet("/offers/active", () => Results.Ok(stubResponse))
    .WithTags("Offers");

app.MapGet("/offers/{offerId}", (string offerId) => Results.Ok(stubResponse))
    .WithTags("Offers");

app.MapGet("/pricing/current", () => Results.Ok(stubResponse))
    .WithTags("Pricing");

app.MapGet("/pricing/match-day-status", () => Results.Ok(stubResponse))
    .WithTags("Pricing");

app.Run();
