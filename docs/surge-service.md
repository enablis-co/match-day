Surge Predictor Service
Overview
A demand curve prediction engine that answers the question every pub manager actually cares about: "When exactly will the rush hit, and how bad will it be?"

This service combines live event data from the Events service with real weather data from a third-party API, then runs a weighted prediction algorithm to produce a per-hour demand forecast for the rest of the day. The output is a timeline of predicted surge periods with intensity scores and confidence levels.

Port: 5007 Container name: surge Depends on: Events Service, OpenMeteo API (free, no key required)


Why This Service
Match day demand isn't a flat multiplier — it has a shape. There's a pre-match build (people arriving 60–90 mins early), a half-time spike (everyone orders at once), a post-match surge (celebrations or commiserations), and weather changes everything. A sunny Saturday with a 3pm kickoff is a completely different demand profile to a rainy Tuesday evening.

Right now the platform knows that it's match day. This service predicts what the demand curve looks like hour by hour — so the other services can plan ahead, not just react.


The Algorithm
Input Signals
Signal
Source
Weight
Range
Event timeline
Events Service /events/today
0.40
Kickoff times, durations
Demand multiplier
Events Service /events/demand-multiplier
0.25
1.0 – 4.0
Weather conditions
Open-Meteo API
0.20
Temperature, rain probability
Time-of-day pattern
Built-in curve
0.15
Historical hourly pattern

Prediction Logic
For each hour in the forecast window, calculate a Surge Score (0.0 to 10.0):

surgeScore = (eventSignal × 0.40) + (demandSignal × 0.25) + (weatherSignal × 0.20) + (timeSignal × 0.15)

Event Signal (0–10):

No event: 0
Event in 2+ hours: 2 (early arrivals)
Event in 60–120 mins: 5 (pre-match build)
Event in 0–60 mins: 8 (rush)
Half-time window (kickoff + 45–60 mins): 10 (peak)
Post-match (0–30 mins after): 7 (surge)
Post-match (30–90 mins after): 4 (wind-down)

Demand Signal (0–10):

Map the multiplier from Events: min(multiplier × 2.5, 10)

Weather Signal (0–10):

Base: temperature mapped to 0–5 (warmer = more footfall)
Rain modifier: rain probability > 60% adds +3 (people come inside to the pub)
Sunny + warm: +2 (beer garden effect)
Formula: min(tempScore + rainModifier + sunBonus, 10)

Time-of-Day Signal (0–10): Built-in baseline curve for a typical pub:

06:00–10:00 → 0 (closed/quiet)
10:00–12:00 → 2 (morning trickle)
12:00–14:00 → 5 (lunch)
14:00–16:00 → 4 (afternoon)
16:00–18:00 → 6 (after work)
18:00–21:00 → 8 (evening peak)
21:00–23:00 → 6 (wind-down)
Confidence Scoring
Confidence reflects how much data is actually available:

Condition
Confidence
Events + Weather both available
HIGH
Events available, Weather fallback
MEDIUM
Events fallback, Weather available
MEDIUM
Both using fallbacks
LOW



Endpoints
GET /surge/forecast
Returns hourly demand prediction for the rest of the day.

Query params:

pubId (string, optional, default: "PUB-001")
hours (int, optional, default: 8) — how many hours ahead to forecast

Response:

{
  "pubId": "PUB-001",
  "generatedAt": "2026-02-12T13:00:00Z",
  "confidence": "HIGH",
  "signals": {
    "eventsAvailable": true,
    "weatherAvailable": true,
    "activeEvents": 1,
    "temperature": 8.5,
    "rainProbability": 0.15
  },
  "forecast": [
    {
      "hour": "13:00",
      "surgeScore": 4.2,
      "intensity": "MODERATE",
      "label": "Pre-match build-up starting",
      "breakdown": {
        "event": 5.0,
        "demand": 3.75,
        "weather": 3.5,
        "timeOfDay": 5.0
      }
    },
    {
      "hour": "14:00",
      "surgeScore": 7.8,
      "intensity": "HIGH",
      "label": "Kickoff — expect rush at the bar",
      "breakdown": {
        "event": 8.0,
        "demand": 6.25,
        "weather": 3.5,
        "timeOfDay": 4.0
      }
    },
    {
      "hour": "15:00",
      "surgeScore": 9.1,
      "intensity": "CRITICAL",
      "label": "Half-time surge — all hands on deck",
      "breakdown": {
        "event": 10.0,
        "demand": 6.25,
        "weather": 3.5,
        "timeOfDay": 4.0
      }
    }
  ]
}
GET /surge/peak
Returns just the single highest predicted surge period. Useful for Aggregator and Staffing.

Response:

{
  "pubId": "PUB-001",
  "peakHour": "15:00",
  "surgeScore": 9.1,
  "intensity": "CRITICAL",
  "label": "Half-time surge — all hands on deck",
  "confidence": "HIGH"
}
GET /health
{
  "status": "OK",
  "service": "Surge Predictor",
  "dependencies": {
    "events": "UP",
    "weather": "UP"
  }
}


Intensity Thresholds
Score
Intensity
Meaning
0.0–2.0
QUIET
Normal or below normal
2.1–4.0
MODERATE
Slightly above baseline
4.1–6.0
BUSY
Noticeable increase
6.1–8.0
HIGH
Significant demand
8.1–10.0
CRITICAL
All hands on deck



Third-Party Integration: Open-Meteo
Why Open-Meteo: Free, no API key required, reliable, good UK coverage. No sign-up friction for a workshop.

Endpoint:

GET https://api.open-meteo.com/v1/forecast?latitude=51.5&longitude=-0.1&hourly=temperature_2m,precipitation_probability&forecast_days=1

Response (simplified):

{
  "hourly": {
    "time": ["2026-02-12T13:00", "2026-02-12T14:00", ...],
    "temperature_2m": [8.5, 8.2, ...],
    "precipitation_probability": [15, 20, ...]
  }
}

Resilience:

Cache the weather response for 30 minutes (it doesn't change fast)
If the API is down, fall back to a neutral weather score of 5.0
Log when using cached vs live data
Set a 3-second HTTP timeout
Copilot Prompts for Weather Integration
"Create an IWeatherService interface with a method that returns hourly temperature and rain probability for London coordinates"
"Implement WeatherService that calls the Open-Meteo API, caches the response for 30 minutes, and falls back to defaults if the API is unreachable"
"Write unit tests for the weather service using a fake HttpMessageHandler that simulates API timeouts and error responses"


Docker Compose Addition
surge:
  build:
    context: ./src/Surge
    dockerfile: Dockerfile
  ports:
    - "5007:8080"
  environment:
    - ASPNETCORE_URLS=http://+:8080
    - EVENTS_SERVICE_URL=http://events:8080


Dashboard Integration
Add a Demand Curve card to the dashboard — a simple bar chart or spark-line showing the hourly surge scores. This is the visual payoff.

nginx addition:

location /api/surge/ {
    proxy_pass http://surge:8080/surge/;
}

Dashboard card concept:

Title: "Predicted Demand"
Visual: Horizontal bar chart, one bar per hour, coloured by intensity (green → amber → red)
Current hour highlighted
Peak hour called out: "Peak expected at 15:00"


The AI / Algorithm Challenge
This is where it gets interesting for Copilot. The weighted algorithm is straightforward to describe but has real implementation decisions:

Weight tuning — the default weights (0.40/0.25/0.20/0.15) are a starting point. Can Copilot help reason about better weights? Ask it: "If the weather is extreme (35°C heatwave or heavy snow), should weather weight increase dynamically?"

Overlapping events — what happens when two matches overlap? The event signal shouldn't just double. Ask Copilot: "How should I combine surge scores when two events have overlapping half-time windows?"

Non-linear scaling — half-time demand isn't just "high", it's a spike. Should the event signal use a bell curve instead of flat buckets? Ask Copilot: "Model the half-time surge as a Gaussian distribution centred on kickoff + 47 minutes with a standard deviation of 8 minutes"

Weather interaction effects — rain + match day is different from rain + quiet day. The signals aren't independent. Ask Copilot: "How do I model interaction effects between weather and event signals in my prediction algorithm?"
Copilot Prompts for the Algorithm
"Create a SurgePredictionService that takes event data and weather data, applies weighted scoring, and returns an hourly forecast with intensity labels"
"Write the event signal calculation that maps minutes-to-kickoff and minutes-since-kickoff to a 0–10 score, with peak at half-time"
"Add a method that detects overlapping events and combines their signals without exceeding 10.0"
"Write tests that verify the surge prediction for a known scenario: Arsenal kickoff at 14:00, 8°C, 15% rain chance"


Starter Scaffold
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Surge Predictor Service", Version = "v1" });
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
        "/surge/forecast",
        "/surge/peak",
    }
};

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "Surge Predictor" }))
    .WithTags("Health")
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapGet("/surge/forecast", () => Results.Ok(stubResponse))
    .WithTags("Surge")
    .WithName("GetSurgeForecast")
    .WithOpenApi();

app.MapGet("/surge/peak", () => Results.Ok(stubResponse))
    .WithTags("Surge")
    .WithName("GetPeakSurge")
    .WithOpenApi();

app.Run();


Acceptance Criteria
Service runs on port 5007 in Docker Compose
/surge/forecast returns hourly predictions with surge scores
/surge/peak returns the single highest predicted hour
Weather data fetched from Open-Meteo (real HTTP call)
Graceful fallback when weather API is unreachable
Graceful fallback when Events service is down
Confidence score reflects data availability
Intensity labels match threshold table
Algorithm handles overlapping events sensibly
Unit tests cover the prediction algorithm with known inputs
/health reports dependency status

