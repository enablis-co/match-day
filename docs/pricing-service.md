# Pricing & Offers Service Specification

## Purpose

The Pricing & Offers Service is the canonical source of truth for promotional pricing and offer rules. It answers the question: **"What should things cost right now, and what offers are active?"**

## Domain Ownership

This service owns:
- Promotion definitions (happy hour, 2-for-1, etc.)
- Time-based pricing rules
- Match day overrides
- Price calculation logic

This service does NOT own:
- Fixture data (consumes from Events Service)
- Stock levels
- Staffing decisions
- Base product catalogue

## Dependencies

| Service | Purpose |
|---------|---------|
| Events Service | Check if match window is active |

## API Endpoints

### GET /offers/active
Returns currently active offers for a pub.

**Query Parameters:**
- `pubId` — The pub to check offers for
- `time` (optional) — ISO timestamp. Defaults to current time.

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:30:00Z",
  "activeOffers": [
    {
      "offerId": "OFFER-001",
      "name": "Happy Hour",
      "description": "50% off selected drinks",
      "status": "ACTIVE",
      "endsAt": "2025-02-10T17:00:00Z"
    }
  ],
  "suspendedOffers": [
    {
      "offerId": "OFFER-002",
      "name": "2-for-1 Cocktails",
      "reason": "Suspended during match day",
      "resumesAt": "2025-02-10T19:30:00Z"
    }
  ]
}
```

### GET /offers/{offerId}
Returns details for a specific offer.

**Response:**
```json
{
  "offerId": "OFFER-001",
  "name": "Happy Hour",
  "description": "50% off selected pints",
  "discountType": "PERCENTAGE",
  "discountValue": 50,
  "applicableProducts": ["PINT_LAGER", "PINT_ALE"],
  "schedule": {
    "days": ["MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY"],
    "startTime": "16:00",
    "endTime": "18:00"
  },
  "matchDayRule": "SUSPEND"
}
```

### GET /pricing/current
Returns current pricing with any active discounts applied.

**Query Parameters:**
- `pubId` — The pub to get pricing for
- `productId` — Specific product (optional, returns all if omitted)

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:30:00Z",
  "prices": [
    {
      "productId": "PINT_GUINNESS",
      "basePrice": 5.50,
      "currentPrice": 5.50,
      "discount": null
    },
    {
      "productId": "PINT_STELLA",
      "basePrice": 5.20,
      "currentPrice": 2.60,
      "discount": {
        "offerId": "OFFER-001",
        "description": "Happy Hour 50% off"
      }
    }
  ]
}
```

### GET /pricing/match-day-status
Returns whether match day pricing rules are in effect.

**Response:**
```json
{
  "timestamp": "2025-02-10T17:30:00Z",
  "matchDayActive": true,
  "affectedOffers": [
    {
      "offerId": "OFFER-001",
      "name": "Happy Hour",
      "action": "ENDED_EARLY",
      "reason": "Match window started"
    },
    {
      "offerId": "OFFER-002",
      "name": "2-for-1 Cocktails",
      "action": "SUSPENDED",
      "reason": "Match day rule"
    }
  ]
}
```

## Data Model

```
Offer
├── offerId: string
├── name: string
├── description: string
├── discountType: enum (PERCENTAGE, FIXED_AMOUNT, BUY_ONE_GET_ONE)
├── discountValue: decimal
├── applicableProducts: string[]
├── schedule: Schedule
└── matchDayRule: enum (CONTINUE, SUSPEND, END_EARLY)

Schedule
├── days: DayOfWeek[]
├── startTime: time
└── endTime: time
```

## Business Rules

**Happy Hour** runs 4pm-6pm weekdays, but ends early if a match window starts.

**2-for-1 offers** are suspended entirely during match windows (margin protection).

**Match day pricing** — No discounts apply during match windows with demand multiplier > 1.5.

**Rule conflicts** — If multiple rules apply, the most restrictive wins.

## Integration Pattern

This service calls Events Service to check match day status:

```
1. Request comes in for active offers
2. Call Events Service: GET /events/active
3. If match window active, apply match day rules
4. Return filtered/modified offer list
```

## Workshop Scenarios

**Scenario 1:** Normal day
- Happy Hour active 4pm-6pm
- All offers available

**Scenario 2:** Match day kicks in
- 4:30pm: Happy Hour active
- 5:00pm: Match starts, Happy Hour ends early
- 2-for-1 suspended

**Scenario 3:** The twist
- Match moves to 7:30pm
- Happy Hour can now run its full window
- But 2-for-1 still suspended for the later match

## Copilot Prompts

- "Create an endpoint that returns active offers, filtering out any suspended by match day rules"
- "How do I call another service in .NET minimal API and handle the case where it's unavailable?"
- "Write a method that evaluates whether an offer should be active given a time and match day status"

## Discussion Points

- Should this service cache Events data, or always call live?
- What happens if Events Service is down — fail open (offers active) or fail closed (no offers)?
- Who decides the match day rules — this service or a config service?
