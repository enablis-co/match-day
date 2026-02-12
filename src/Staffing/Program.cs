using Staffing.Endpoints;

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

app.MapHealthEndpoints();
app.MapStaffingEndpoints();

app.Run();
