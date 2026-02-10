# Stock & Forecasting Service Specification

## Purpose

The Stock & Forecasting Service is the canonical source of truth for inventory levels and consumption predictions. It answers the question: **"What do we have, how fast is it going, and will we run out?"**

## Domain Ownership

This service owns:
- Current stock levels
- Historical consumption rates
- Depletion forecasts
- Stock alerts

This service does NOT own:
- Fixture data (consumes from Events Service)
- Pricing or offers
- Staffing decisions
- Reordering/procurement

## Dependencies

| Service | Purpose |
|---------|---------|
| Events Service | Get demand multiplier for forecasts |

## API Endpoints

### GET /stock/current
Returns current stock levels for a pub.

**Query Parameters:**
- `pubId` — The pub to check
- `category` (optional) — Filter by category (beer, spirits, etc.)

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:00:00Z",
  "stock": [
    {
      "productId": "GUINNESS",
      "productName": "Guinness Draught",
      "category": "beer",
      "currentLevel": 45,
      "unit": "pints",
      "status": "OK"
    },
    {
      "productId": "STELLA",
      "productName": "Stella Artois",
      "category": "beer",
      "currentLevel": 120,
      "unit": "pints",
      "status": "OK"
    }
  ]
}
```

### GET /stock/{productId}/forecast
Returns depletion forecast for a specific product.

**Query Parameters:**
- `pubId` — The pub to forecast for
- `hours` (optional) — Forecast horizon in hours. Default 4.

**Response:**
```json
{
  "pubId": "PUB-001",
  "productId": "GUINNESS",
  "productName": "Guinness Draught",
  "currentLevel": 45,
  "forecast": {
    "baseConsumptionRate": 8,
    "rateUnit": "pints_per_hour",
    "demandMultiplier": 2.0,
    "adjustedRate": 16,
    "estimatedDepletionTime": "2025-02-10T18:45:00Z",
    "hoursRemaining": 2.75,
    "willDepleteInWindow": true,
    "confidence": "MEDIUM"
  },
  "recommendation": "Restock before match ends"
}
```

### GET /stock/alerts
Returns products at risk of depletion.

**Query Parameters:**
- `pubId` — The pub to check
- `threshold` (optional) — Hours of stock remaining to trigger alert. Default 2.

**Response:**
```json
{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:30:00Z",
  "alerts": [
    {
      "productId": "GUINNESS",
      "productName": "Guinness Draught",
      "severity": "HIGH",
      "currentLevel": 45,
      "estimatedDepletionTime": "2025-02-10T18:45:00Z",
      "hoursRemaining": 2.25,
      "reason": "High demand during match window"
    }
  ],
  "summary": {
    "highSeverity": 1,
    "mediumSeverity": 0,
    "lowSeverity": 2
  }
}
```

### POST /stock/consumption
Records a consumption event (simulates sales).

**Request:**
```json
{
  "pubId": "PUB-001",
  "productId": "GUINNESS",
  "quantity": 2,
  "timestamp": "2025-02-10T16:35:00Z"
}
```

**Response:**
```json
{
  "recorded": true,
  "newLevel": 43,
  "productId": "GUINNESS"
}
```

## Data Model

```
StockLevel
├── pubId: string
├── productId: string
├── productName: string
├── category: string
├── currentLevel: integer
├── unit: string
├── lastUpdated: datetime
└── status: enum (OK, LOW, CRITICAL)

ConsumptionRecord
├── pubId: string
├── productId: string
├── quantity: integer
├── timestamp: datetime
└── source: enum (SALE, ADJUSTMENT, WASTE)

Forecast
├── baseConsumptionRate: decimal
├── demandMultiplier: decimal
├── adjustedRate: decimal
├── estimatedDepletionTime: datetime
└── confidence: enum (HIGH, MEDIUM, LOW)
```

## Business Rules

**Base consumption rates** are calculated from historical averages (hardcoded for workshop).

**Demand adjustment** — Multiply base rate by demand multiplier from Events Service.

**Alert thresholds:**
- HIGH: Less than 2 hours of stock at current rate
- MEDIUM: Less than 4 hours
- LOW: Less than 6 hours

**Confidence levels:**
- HIGH: Demand multiplier from active event
- MEDIUM: Predicted event window
- LOW: No event data, using baseline

## Integration Pattern

```
1. Request for forecast received
2. Get current stock level
3. Call Events Service: GET /events/demand-multiplier
4. Apply multiplier to base consumption rate
5. Calculate depletion time
6. Return forecast with confidence level
```

## Workshop Scenarios

**Scenario 1:** Normal afternoon
- Base consumption rate applies
- Forecasts show comfortable stock levels

**Scenario 2:** Match kicks off
- Demand multiplier jumps to 2.0
- Guinness forecast suddenly shows depletion during match
- Alert triggers

**Scenario 3:** The twist
- Match moves later, but demand multiplier doubles
- Forecasts recalculate — stock that was safe is now at risk

## Copilot Prompts

- "Create a forecast method that calculates depletion time given current stock, consumption rate, and a demand multiplier"
- "How do I structure an alert system that categorises severity based on hours of stock remaining?"
- "Write a .NET minimal API endpoint that calls another service and gracefully handles timeout"

## Discussion Points

- Should forecasts account for restocking during the day, or assume fixed stock?
- How accurate does this need to be? What's the cost of a false alarm vs missed alert?
- If Events Service is slow, should we use cached multipliers or assume baseline?
