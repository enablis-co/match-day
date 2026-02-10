# Staffing Signal Service Specification

## Purpose

The Staffing Signal Service generates recommendations for staffing adjustments based on demand signals. It answers the question: **"Do we have enough people, and if not, what should we do?"**

## Domain Ownership

This service owns:
- Staffing recommendations
- Signal aggregation logic
- Confidence calculations
- Recommendation history

This service does NOT own:
- Fixture data (consumes from Events Service)
- Stock levels (consumes from Stock Service)
- Actual staff rosters
- HR or scheduling systems

## Dependencies

| Service | Purpose |
|---------|---------|
| Events Service | Get demand multiplier and event timing |
| Stock & Forecasting Service | Get risk signals for stock pressure |

## API Endpoints

### GET /staffing/recommendation
Returns staffing recommendation for a pub.

**Query Parameters:**
- `pubId` — The pub to get recommendation for
- `time` (optional) — ISO timestamp. Defaults to current time.

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:30:00Z",
  "recommendation": {
    "action": "INCREASE",
    "additionalStaff": 2,
    "roles": ["bar", "floor"],
    "urgency": "HIGH",
    "windowStart": "2025-02-10T16:30:00Z",
    "windowEnd": "2025-02-10T19:30:00Z"
  },
  "confidence": "HIGH",
  "signals": [
    {
      "source": "EVENTS",
      "signal": "Match window active",
      "weight": 0.6
    },
    {
      "source": "STOCK",
      "signal": "High depletion rate expected",
      "weight": 0.3
    },
    {
      "source": "HISTORICAL",
      "signal": "Similar events averaged +2 staff",
      "weight": 0.1
    }
  ]
}
```

### GET /staffing/signals
Returns raw signals without recommendation (useful for debugging).

**Query Parameters:**
- `pubId` — The pub to check

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:30:00Z",
  "signals": {
    "demandMultiplier": 2.0,
    "matchWindowActive": true,
    "matchDescription": "England vs France",
    "stockPressure": "HIGH",
    "stockAlerts": 1,
    "historicalAverage": 2.1
  }
}
```

### GET /staffing/history
Returns recent recommendations for a pub.

**Query Parameters:**
- `pubId` — The pub to check
- `days` (optional) — How many days back. Default 7.

**Response:**
```json
{
  "pubId": "PUB-001",
  "recommendations": [
    {
      "date": "2025-02-03",
      "event": "England vs Ireland",
      "recommended": 2,
      "outcome": "FOLLOWED",
      "feedback": "Adequate staffing"
    }
  ]
}
```

## Data Model

```
StaffingRecommendation
├── pubId: string
├── timestamp: datetime
├── action: enum (INCREASE, DECREASE, MAINTAIN)
├── additionalStaff: integer
├── roles: string[]
├── urgency: enum (LOW, MEDIUM, HIGH)
├── windowStart: datetime
├── windowEnd: datetime
├── confidence: enum (LOW, MEDIUM, HIGH)
└── signals: Signal[]

Signal
├── source: enum (EVENTS, STOCK, HISTORICAL, WEATHER)
├── signal: string
├── weight: decimal
└── rawValue: any
```

## Business Rules

**Recommendation calculation:**
1. Get demand multiplier from Events Service
2. Get stock pressure from Stock Service
3. Apply weights to each signal
4. Calculate staff delta

**Weighting:**
| Signal | Weight |
|--------|--------|
| Demand multiplier | 60% |
| Stock pressure | 30% |
| Historical pattern | 10% |

**Thresholds:**
- Multiplier ≥ 1.5 → Recommend +1 staff
- Multiplier ≥ 2.0 → Recommend +2 staff
- Stock alerts HIGH → Add +1 to recommendation
- Maximum recommendation: +4 staff

**Confidence:**
- HIGH: All signals available and consistent
- MEDIUM: Some signals unavailable, using defaults
- LOW: Conflicting signals or stale data

## Integration Pattern

```
1. Request for recommendation received
2. Call Events Service: GET /events/active
3. Call Stock Service: GET /stock/alerts
4. Aggregate signals with weights
5. Apply thresholds to calculate recommendation
6. Return recommendation with signal breakdown
```

## Failure Handling

| Failed Service | Fallback |
|----------------|----------|
| Events Service | Use baseline multiplier (1.0), confidence LOW |
| Stock Service | Exclude stock signal, adjust weights, confidence MEDIUM |
| Both | Return MAINTAIN with confidence LOW |

## Workshop Scenarios

**Scenario 1:** Normal day
- Low demand multiplier
- No stock alerts
- Recommendation: MAINTAIN

**Scenario 2:** Match day
- Multiplier 2.0, stock alert HIGH
- Recommendation: INCREASE by 2, urgency HIGH

**Scenario 3:** The twist
- Multiplier jumps to 4.0 (doubled)
- Recommendation changes to +3 or +4
- But Events Service might be slow to update

## Copilot Prompts

- "Create a method that aggregates weighted signals and returns a staffing recommendation"
- "How do I call multiple services in parallel in .NET and combine their results?"
- "Write logic that degrades gracefully when one dependency is unavailable"

## Discussion Points

- Should this service just emit signals, or actually make recommendations?
- Who owns the decision logic — this service, or the consumer?
- If signals conflict (high demand but low stock alerts), what wins?
- Should recommendations be cached, or always calculated fresh?

## Why Quiet Engineers Like This Service

This is pure logic. No UI, no database complexity. Take inputs, apply rules, emit outputs. The interesting work is in:
- How you weight signals
- How you handle missing data
- How you express confidence

It's a great service for someone who wants to think about algorithms without worrying about infrastructure.
