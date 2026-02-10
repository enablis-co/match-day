# Pub Status Aggregator Specification

## Purpose

The Pub Status Aggregator provides a unified view of a pub's operational status by orchestrating calls to all other services. It answers the question: **"What's happening at this pub right now, and what should we do about it?"**

## Domain Ownership

This service owns:
- Aggregation logic
- Risk level calculation
- Action prioritisation
- Unified response format

This service does NOT own:
- Any primary data (it only orchestrates)
- Business rules for individual domains
- Historical data storage

## Dependencies

| Service | Purpose |
|---------|---------|
| Events Service | Match window and demand data |
| Pricing & Offers Service | Active and suspended offers |
| Stock & Forecasting Service | Stock levels and alerts |
| Staffing Signal Service | Staffing recommendations |

## API Endpoints

### GET /status/{pubId}
Returns complete operational status for a pub.

**Query Parameters:**
- `time` (optional) — ISO timestamp. Defaults to current time.

**Response:**
```json
{
  "pubId": "PUB-001",
  "pubName": "The Crown & Anchor",
  "timestamp": "2025-02-10T17:30:00Z",
  "status": {
    "overall": "ELEVATED",
    "riskLevel": "HIGH",
    "matchDay": true
  },
  "events": {
    "active": true,
    "current": "England vs France",
    "demandMultiplier": 2.0,
    "endsAt": "2025-02-10T19:00:00Z"
  },
  "pricing": {
    "offersActive": 0,
    "offersSuspended": 2,
    "suspensionReason": "Match day rules"
  },
  "stock": {
    "alertCount": 1,
    "criticalItems": ["Guinness"],
    "estimatedShortfall": "18:45"
  },
  "staffing": {
    "recommendation": "INCREASE",
    "additionalRequired": 2,
    "urgency": "HIGH",
    "confidence": "HIGH"
  },
  "actions": [
    {
      "priority": 1,
      "action": "RESTOCK_GUINNESS",
      "reason": "Will deplete before match ends",
      "deadline": "2025-02-10T18:00:00Z"
    },
    {
      "priority": 2,
      "action": "CALL_EXTRA_STAFF",
      "reason": "High demand expected",
      "deadline": "2025-02-10T17:00:00Z"
    }
  ],
  "serviceHealth": {
    "events": "OK",
    "pricing": "OK",
    "stock": "OK",
    "staffing": "OK"
  }
}
```

### GET /status/{pubId}/summary
Returns a simplified status (for dashboards or quick checks).

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T17:30:00Z",
  "matchDay": true,
  "riskLevel": "HIGH",
  "actionCount": 2,
  "topAction": "Restock Guinness before 18:00"
}
```

### GET /status/{pubId}/actions
Returns only the prioritised action list.

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T17:30:00Z",
  "actions": [
    {
      "priority": 1,
      "action": "RESTOCK_GUINNESS",
      "reason": "Will deplete before match ends",
      "deadline": "2025-02-10T18:00:00Z",
      "source": "STOCK"
    },
    {
      "priority": 2,
      "action": "CALL_EXTRA_STAFF",
      "reason": "High demand expected",
      "deadline": "2025-02-10T17:00:00Z",
      "source": "STAFFING"
    }
  ]
}
```

### GET /health
Returns aggregator health and downstream service status.

**Response:**
```json
{
  "status": "HEALTHY",
  "services": {
    "events": { "status": "OK", "latency": 45 },
    "pricing": { "status": "OK", "latency": 62 },
    "stock": { "status": "OK", "latency": 38 },
    "staffing": { "status": "OK", "latency": 71 }
  }
}
```

## Data Model

```
PubStatus
├── pubId: string
├── timestamp: datetime
├── overall: enum (NORMAL, ELEVATED, HIGH, CRITICAL)
├── riskLevel: enum (LOW, MEDIUM, HIGH)
├── matchDay: boolean
├── events: EventSummary
├── pricing: PricingSummary
├── stock: StockSummary
├── staffing: StaffingSummary
├── actions: Action[]
└── serviceHealth: ServiceHealthMap

Action
├── priority: integer
├── action: string
├── reason: string
├── deadline: datetime
└── source: enum (EVENTS, PRICING, STOCK, STAFFING, AGGREGATOR)
```

## Business Rules

**Risk level calculation:**
| Condition | Risk Level |
|-----------|------------|
| No active events, no alerts | LOW |
| Active event OR stock alert | MEDIUM |
| Active event AND stock alert | HIGH |
| Multiple critical conditions | CRITICAL |

**Action prioritisation:**
1. Stock depletion with deadline < 1 hour
2. Staffing increase with urgency HIGH
3. Stock depletion with deadline < 2 hours
4. Other staffing recommendations
5. Pricing/offer notifications

**Overall status:**
- NORMAL: Business as usual
- ELEVATED: Match day active, no issues
- HIGH: Match day with stock or staffing concerns
- CRITICAL: Multiple urgent issues

## Integration Pattern

```
1. Request received for pub status
2. Call all four services in parallel:
   - Events: GET /events/active
   - Pricing: GET /offers/active?pubId={pubId}
   - Stock: GET /stock/alerts?pubId={pubId}
   - Staffing: GET /staffing/recommendation?pubId={pubId}
3. Await all responses (with timeouts)
4. Aggregate into unified response
5. Calculate risk level and prioritise actions
6. Return complete status
```

## Failure Handling

| Failed Service | Behaviour |
|----------------|-----------|
| Events | Assume no active match, mark service DEGRADED |
| Pricing | Omit pricing section, mark service DEGRADED |
| Stock | Omit stock section, mark service DEGRADED, risk = UNKNOWN |
| Staffing | Omit staffing section, mark service DEGRADED |
| Multiple | Return partial response with clear health indicators |

The aggregator should never fail completely. Partial data is better than no data.

## Workshop Scenarios

**Scenario 1:** All services healthy
- Full response with all sections populated
- Actions prioritised correctly

**Scenario 2:** One service slow
- Response returns with partial data
- Health section shows which service timed out

**Scenario 3:** The twist
- Events data changes dramatically
- Aggregator pulls new data, recalculates risk
- Action priorities shift

**Scenario 4:** Conflicting signals
- Events says high demand
- Stock says everything fine
- How does aggregator express confidence?

## Copilot Prompts

- "How do I call four HTTP endpoints in parallel in .NET and wait for all of them?"
- "Create a method that handles partial failures gracefully when aggregating API responses"
- "Write a risk calculation function that combines multiple boolean signals into a severity level"

## Discussion Points

- Should the aggregator cache responses, or always call live?
- How long should it wait for slow services before returning partial data?
- Who owns the risk calculation logic — aggregator or a separate service?
- Should actions include recommended deadlines, or just surface raw data?

## Why This Service Is the Final Boss

This is where integration gets real. The Aggregator doesn't own data — it owns orchestration. Building it forces you to:

- Handle partial failures
- Think about timeouts
- Prioritise conflicting information
- Design a coherent response from messy inputs

It's the hardest service to build, which is why it should be tackled last (or by the most confident team).
