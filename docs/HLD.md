High-Level Design: Match Day Mode Platform
Overview
Match Day Mode is an internal API platform that enables pubs to automatically adapt operations when high-demand events occur — primarily football and rugby fixtures.

The platform follows a domain-driven microservices architecture where each service owns a specific bounded context and communicates via REST APIs.
Business Context
When a major sporting event is on, pubs face operational challenges: demand spikes unpredictably, stock depletes faster than normal, staffing may be insufficient, and promotions designed for quiet periods can erode margin during peak hours.

Match Day Mode provides automated signals and recommendations to help pub managers and head office respond to these pressures.
Architecture Principles
Domain ownership — Each service owns its data. No shared databases. If Service A needs data from Service B, it calls Service B's API.

Loose coupling — Services can function independently. If the Events Service is unavailable, other services degrade gracefully rather than failing entirely.

Simple contracts — All services expose REST APIs with OpenAPI/Swagger documentation. No complex messaging infrastructure for the workshop scope.

Containerised deployment — Each service runs in its own container. Docker Compose orchestrates local development.
System Context
┌─────────────────────────────────────────────────────────────────┐
│                        Pub Operations                           │
│  (Managers, Head Office, Scheduling Systems, Stock Systems)     │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Pub Status Aggregator                        │
│         (Unified view of pub operational state)                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                ▼               ▼               ▼
┌───────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
│  Events Service   │ │ Pricing Service │ │ Forecasting Service │
│  (Fixtures, times)│ │ (Offers, rules) │ │ (Stock, burn rate)  │
└───────────────────┘ └─────────────────┘ └─────────────────────┘
                                │
                                ▼
                ┌─────────────────────────┐
                │  Staffing Signal Service │
                │  (Recommendations)       │
                └─────────────────────────┘
Service Overview
Service
Owns
Consumes From
Events Service
Fixtures, match windows, demand multipliers
None (source of truth)
Pricing & Offers Service
Promotion rules, pricing logic
Events Service
Stock & Forecasting Service
Stock levels, consumption rates, depletion estimates
Events Service
Staffing Signal Service
Staffing recommendations
Events, Forecasting
Pub Status Aggregator
Unified operational view
All services

Data Flow Example
Scenario: England vs France kicks off at 5pm. A pub manager wants to know if they're prepared.

Pub Status Aggregator receives request for pub status
Calls Events Service → "Match window active, demand multiplier 2.0x"
Calls Pricing Service → "Happy hour paused due to match day rule"
Calls Stock & Forecasting Service → "Guinness will deplete by 18:45 at current rate"
Calls Staffing Signal Service → "Recommend +2 staff, confidence HIGH"
Aggregator returns unified response:

{
  "pubId": "PUB-001",
  "timestamp": "2025-02-10T16:30:00Z",
  "matchDay": true,
  "riskLevel": "HIGH",
  "signals": {
    "stockRisk": "Guinness depletion expected before full time",
    "staffingRecommendation": "Increase by 2",
    "pricingStatus": "Happy hour suspended"
  },
  "actions": ["RESTOCK_GUINNESS", "CALL_EXTRA_STAFF"]
}
Technology Stack
.NET 10 — All services
Docker — Containerisation
Docker Compose — Local orchestration
Swagger/OpenAPI — API documentation and testing
REST/JSON — Service communication
Non-Functional Considerations
For workshop purposes, we simplify:

No authentication between services
No service discovery — hardcoded URLs in Docker network
No resilience patterns (circuit breakers, retries) — though these make good discussion points
In-memory data stores — no databases

In production, you'd add:

API gateway
Service mesh or discovery
Event-driven patterns for real-time updates
Proper observability (tracing, metrics)
Bounded Contexts
Each service represents a bounded context with clear ownership:

Events — The canonical source for "what's happening when"
Pricing — The canonical source for "what should things cost right now"
Stock — The canonical source for "what do we have and how fast is it going"
Staffing — The canonical source for "do we have enough people"
Aggregator — No canonical data; orchestration only

Cross-cutting concerns like "is this pub busy" are answered by aggregation, not by giving one service ownership of another's domain.
Workshop Progression
Phase 1: Build individual services with 1-2 endpoints each Phase 2: Services start calling each other Phase 3: Inject change (fixture time moves, demand doubles) — observe integration stress Phase 4: Aggregator pulls everything together
Open Questions for Discussion
These are intentionally unresolved — they're conversation starters:

Should Pricing know about Stock levels, or is that crossing a boundary?
If Events Service goes down, should other services cache the last known state?
Who owns the "risk level" calculation — Aggregator or a dedicated service?
Should services push updates or should consumers poll?




Team Breakdown
Quick reference for each service team — what to build and what to expect.


Pricing & Offers
What you're building:

Return active offers for a pub
Return current prices with discounts applied
Suspend or end offers when a match is on

Key endpoints:

GET /offers/active?pubId=PUB-001
GET /pricing/current?pubId=PUB-001
GET /pricing/match-day-status

You'll need to call:

Events Service → GET /events/active to check if match window is active

What you'll hit:

Events might return no active match — your logic needs to handle both states
Decision: should you cache the Events response or call fresh every time?


Stock & Forecasting
What you're building:

Return current stock levels
Forecast when a product will run out
Flag products at risk of depletion

Key endpoints:

GET /stock/current?pubId=PUB-001
GET /stock/{productId}/forecast?pubId=PUB-001
GET /stock/alerts?pubId=PUB-001

You'll need to call:

Events Service → GET /events/demand-multiplier to adjust your burn rate

What you'll hit:

Your forecast changes dramatically when demand multiplier jumps from 1.0 to 2.0
You'll need fake stock data — hardcode some sensible starting levels
Decision: how confident is your forecast? What if Events is unavailable?


Staffing Signals
What you're building:

Recommend whether to increase, decrease, or maintain staffing
Show what signals drove the recommendation
Express confidence in the recommendation

Key endpoints:

GET /staffing/recommendation?pubId=PUB-001
GET /staffing/signals?pubId=PUB-001

You'll need to call:

Events Service → GET /events/active or /demand-multiplier
Stock Service → GET /stock/alerts to factor in stock pressure

What you'll hit:

You depend on TWO other services — if Stock isn't built yet, you need to handle that
Conflicting signals: high demand but no stock alerts — what do you recommend?
Decision: if Stock service is down, do you still make a recommendation with lower confidence?


Pub Status Aggregator
What you're building:

Call all four services and combine into one response
Calculate overall risk level
Prioritise actions

Key endpoints:

GET /status/{pubId}
GET /status/{pubId}/summary
GET /status/{pubId}/actions

You'll need to call:

Events Service → GET /events/active
Pricing Service → GET /offers/active
Stock Service → GET /stock/alerts
Staffing Service → GET /staffing/recommendation

What you'll hit:

You depend on ALL other services — you'll be waiting on others to finish
Some services will return stub data initially — handle gracefully
Parallel vs sequential: calling four services one at a time is slow
Partial failure: if Staffing is down, do you fail entirely or return what you have?
Decision: who owns the "risk level" calculation — you, or do you just pass through what others say?

