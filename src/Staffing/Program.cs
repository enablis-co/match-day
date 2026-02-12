using Staffing.Clients;
using Staffing.Endpoints;
using Staffing.Services;

var builder = WebApplication.CreateBuilder(args);

// Read upstream service URLs from environment
var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:5001";
var stockServiceUrl = Environment.GetEnvironmentVariable("STOCK_SERVICE_URL") ?? "http://localhost:5003";

// Register HTTP clients
builder.Services.AddHttpClient<IEventsClient, EventsClient>(client =>
{
    client.BaseAddress = new Uri(eventsServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IStockClient, StockClient>(client =>
{
    client.BaseAddress = new Uri(stockServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Register services
builder.Services.AddScoped<IStaffingService, StaffingService>();

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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Staffing Signal Service v1");
});

app.MapHealthEndpoints();
app.MapStaffingEndpoints();

app.Run();
