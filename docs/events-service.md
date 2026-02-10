# Events Service Specification

## Purpose

The Events Service is the canonical source of truth for sporting fixtures and their operational impact on pubs. It answers the question: **"What events are happening, and when?"**

## Domain Ownership

This service owns:
- Fixture data (teams, sport, competition)
- Start and end times
- Expected attendance multipliers
- Match window calculations

This service does NOT own:
- Stock levels
- Pricing rules
- Staffing decisions
- Pub-specific data

## API Endpoints

### GET /events/today
Returns all fixtures scheduled for today.

**Response:**
```json
{
  "date": "2025-02-10",
  "events": [
    {
      "eventId": "EVT-001",
      "sport": "football",
      "competition": "Six Nations",
      "homeTeam": "England",
      "awayTeam": "France",
      "kickoff": "2025-02-10T17:00:00Z",
      "expectedEnd": "2025-02-10T19:00:00Z",
      "demandMultiplier": 2.0
    }
  ]
}
```

### GET /events/{eventId}
Returns details for a specific event.

**Response:**
```json
{
  "eventId": "EVT-001",
  "sport": "football",
  "competition": "Six Nations",
  "homeTeam": "England",
  "awayTeam": "France",
  "kickoff": "2025-02-10T17:00:00Z",
  "expectedEnd": "2025-02-10T19:00:00Z",
  "demandMultiplier": 2.0,
  "status": "SCHEDULED"
}
```

### GET /events/active
Returns events currently in progress (within match window).

**Query Parameters:**
- `time` (optional) — ISO timestamp to check against. Defaults to current time.

**Response:**
```json
{
  "timestamp": "2025-02-10T17:30:00Z",
  "activeEvents": [
    {
      "eventId": "EVT-001",
      "description": "England vs France",
      "minutesRemaining": 90,
      "demandMultiplier": 2.0
    }
  ],
  "inMatchWindow": true
}
```

### GET /events/demand-multiplier
Returns the current aggregate demand multiplier based on active events.

**Response:**
```json
{
  "timestamp": "2025-02-10T17:30:00Z",
  "multiplier": 2.0,
  "reason": "England vs France in progress"
}
```

## Data Model

```
Event
├── eventId: string
├── sport: enum (football, rugby, cricket, other)
├── competition: string
├── homeTeam: string
├── awayTeam: string
├── kickoff: datetime
├── expectedEnd: datetime
├── demandMultiplier: decimal (1.0 = normal, 2.0 = double)
└── status: enum (SCHEDULED, LIVE, COMPLETED, CANCELLED)
```

## Business Rules

**Match window** extends from 30 minutes before kickoff to 30 minutes after expected end. This accounts for pre-match buildup and post-match drinking.

**Demand multiplier** is based on:
- England matches: 2.0x
- Other Six Nations: 1.5x
- Premier League (major): 1.5x
- Premier League (other): 1.2x
- Default: 1.0x

**Overlapping events** — If multiple events overlap, return the highest multiplier.

## Integration Notes

Other services call this service to determine:
- Whether a match window is active
- What demand multiplier to apply to forecasts
- When the current event ends

This service does not call any other services. It is a source of truth with no upstream dependencies.

## Workshop Scenarios

**Scenario 1:** Basic query
- Call `/events/today` to see the fixture list
- Call `/events/active` to check if we're in a match window

**Scenario 2:** Time manipulation
- Use the `time` parameter to simulate different points in the day
- "What's the demand multiplier at 4pm vs 6pm?"

**Scenario 3:** The twist
- Fixture time changes mid-workshop
- Demand multiplier doubles
- Other services must adapt

## Copilot Prompts

Starter prompts for building this service:

- "Create a .NET 10 minimal API with an endpoint that returns today's events"
- "Add a method that calculates whether a given time falls within a match window"
- "How do I handle overlapping events and return the maximum demand multiplier?"
