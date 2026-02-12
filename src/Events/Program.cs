using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Events Service",
        Version = "v1",
        Description = "Match Day Mode — Fixtures and demand data"
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// --- Load fixtures from JSON ---
var fixturesPath = Path.Combine(app.Environment.ContentRootPath, "Data", "fixtures.json");
var fixturesJson = File.ReadAllText(fixturesPath);
var fixtureData = JsonSerializer.Deserialize<FixtureFile>(fixturesJson)!;
var events = fixtureData.Events;

// --- Simulated Clock ---
var simulatedTime = (DateTime?)null;

DateTime Now() => simulatedTime ?? DateTime.UtcNow;

// --- Clock Endpoints ---
app.MapGet("/clock", () => new { currentTime = Now() })
   .WithTags("Clock");

app.MapPost("/clock", (ClockUpdate update) =>
{
    simulatedTime = update.Time;
    return new { currentTime = Now() };
})
.WithTags("Clock");

// --- Health ---
app.MapGet("/health", () => new { status = "OK", service = "Events" })
   .WithTags("Health");

// --- Events Endpoints ---

app.MapGet("/events/today", () =>
{
    var today = Now().Date;
    var todayEvents = events.Where(e => e.Kickoff.Date == today).ToList();
    return new { date = today.ToString("yyyy-MM-dd"), events = todayEvents };
})
.WithTags("Events");

app.MapGet("/events/{eventId}", (string eventId) =>
{
    var evt = events.FirstOrDefault(e => e.EventId == eventId);
    if (evt is null) return Results.NotFound(new { error = $"Event {eventId} not found" });
    return Results.Ok(evt);
})
.WithTags("Events");

app.MapGet("/events/active", (DateTime? time) =>
{
    var now = time ?? Now();
    var windowMinutes = 30;

    var active = events
        .Where(e => now >= e.Kickoff.AddMinutes(-windowMinutes) && now <= e.ExpectedEnd.AddMinutes(windowMinutes))
        .Select(e => new
        {
            e.EventId,
            Description = $"{e.HomeTeam} vs {e.AwayTeam}",
            MinutesRemaining = Math.Max(0, (int)(e.ExpectedEnd - now).TotalMinutes),
            e.DemandMultiplier,
            e.ExpectedEnd
        })
        .ToList();

    return new
    {
        Timestamp = now,
        ActiveEvents = active,
        InMatchWindow = active.Any()
    };
})
.WithTags("Events");

app.MapGet("/events/demand-multiplier", (DateTime? time) =>
{
    var now = time ?? Now();
    var windowMinutes = 30;

    var active = events
        .Where(e => now >= e.Kickoff.AddMinutes(-windowMinutes) && now <= e.ExpectedEnd.AddMinutes(windowMinutes))
        .ToList();

    if (!active.Any())
    {
        return new { Timestamp = now, Multiplier = 1.0, Reason = "No active events" };
    }

    var top = active.OrderByDescending(e => e.DemandMultiplier).First();
    return new
    {
        Timestamp = now,
        Multiplier = top.DemandMultiplier,
        Reason = $"{top.HomeTeam} vs {top.AwayTeam} in progress"
    };
})
.WithTags("Events");

app.Run();

// --- Models ---

public class FixtureFile
{
    [JsonPropertyName("events")]
    public List<SportingEvent> Events { get; set; } = new();
}

public class SportingEvent
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = "";
    [JsonPropertyName("sport")]
    public string Sport { get; set; } = "";
    [JsonPropertyName("competition")]
    public string Competition { get; set; } = "";
    [JsonPropertyName("homeTeam")]
    public string HomeTeam { get; set; } = "";
    [JsonPropertyName("awayTeam")]
    public string AwayTeam { get; set; } = "";
    [JsonPropertyName("kickoff")]
    public DateTime Kickoff { get; set; }
    [JsonPropertyName("expectedEnd")]
    public DateTime ExpectedEnd { get; set; }
    [JsonPropertyName("demandMultiplier")]
    public double DemandMultiplier { get; set; }
    [JsonPropertyName("status")]
    public string Status { get; set; } = "SCHEDULED";
}

public record ClockUpdate(DateTime Time);
