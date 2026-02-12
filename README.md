# Match Day Mode

Backend platform for pub operations during high-demand sporting events.

## Quick Start

```bash
docker-compose up --build
```

## Services

| Service | URL | Swagger |
|---------|-----|---------|
| Events | http://localhost:5001 | http://localhost:5001/swagger |
| Pricing | http://localhost:5002 | http://localhost:5002/swagger |
| Stock | http://localhost:5003 | http://localhost:5003/swagger |
| Staffing | http://localhost:5004 | http://localhost:5004/swagger |
| Aggregator | http://localhost:5005 | http://localhost:5005/swagger |
| Surge | http://localhost:5007 | http://localhost:5007/swagger |
| Dashboard | http://localhost:5006 | — |
## Prerequisites

- Docker Desktop installed and running
- Ports 5001–5007 available

## Workshop Instructions

1. Pick a service (Pricing, Stock, Staffing, Surge, or Aggregator)
2. Check the spec in `/docs`
3. Build endpoints using Copilot
4. Test via Swagger
5. Call other services when needed

The **Events Service** is fully working as a reference — look at its code for patterns.

## Manipulating Time

The Events Service has a simulated clock so you can test different points in the day without waiting.

```bash
# Get current simulated time
curl http://localhost:5001/clock

# Set time to 5:30pm (match in progress)
curl -X POST http://localhost:5001/clock \
  -H "Content-Type: application/json" \
  -d '{"time": "2026-02-12T17:30:00Z"}'

# Set time to 3pm (Arsenal vs Chelsea in progress)
curl -X POST http://localhost:5001/clock \
  -H "Content-Type: application/json" \
  -d '{"time": "2026-02-12T15:00:00Z"}'
```

## Service Dependencies

```
Events ← Pricing
Events ← Stock
Events ← Surge
Events + Stock ← Staffing
Events + Pricing + Stock + Staffing + Surge ← Aggregator
Aggregator + Events ← Dashboard (http://localhost:5006)
```

Services call each other over HTTP using Docker networking. The base URLs are passed as environment variables (e.g. `EVENTS_SERVICE_URL=http://events:8080`).