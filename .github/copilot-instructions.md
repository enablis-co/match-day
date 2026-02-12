# Copilot Instructions

## Architecture
- Microservices platform: Events, Pricing & Offers, Stock & Forecasting, Staffing Signal, Pub Status Aggregator
- All services use ASP.NET Core Minimal APIs — no controllers
- Services communicate via REST/JSON over Docker Compose network
- Upstream service URLs are read from environment variables with localhost fallbacks
- No shared databases — each service owns its own data
- In-memory data stores only — no external databases
- Each service is a bounded context with explicit ownership boundaries (see service specs for "owns" and "does NOT own" lists)

## Bounded Contexts
- Events — canonical source for fixtures, match windows, demand multipliers (source of truth, no upstream dependencies)
- Pricing & Offers — canonical source for promotion rules, pricing logic, match day overrides (consumes Events)
- Stock & Forecasting — canonical source for inventory levels, consumption rates, depletion forecasts (consumes Events)
- Staffing Signal — canonical source for staffing recommendations, confidence, signal aggregation (consumes Events + Stock)
- Pub Status Aggregator — orchestration only, owns no primary data (consumes all services)

## Code Structure (per service)
- Models/ — domain models, enums, DTOs
- Services/ — business logic (interface + implementation)
- Endpoints/ — static extension methods mapping minimal API routes
- Clients/ — typed HTTP clients for upstream service calls (interface + implementation)
- Data/ — repositories or static data files
- One type per file ONLY (exception: DTO containers may group related DTOs)

## Design Principles
- SOLID principles throughout
- Constructor injection with interfaces — no concrete dependencies
- Register all dependencies in DI: interface → implementation
- Keep interfaces focused and minimal (1–5 methods)
- Use IHttpClientFactory with typed clients for service-to-service calls
- Graceful degradation: when an upstream service is unavailable, degrade response quality (e.g. lower confidence) rather than failing
- Services must never own or make decisions about another service's domain

## Naming Conventions
- Interfaces: IMyService, IMyClient, IMyRepository
- Classes: MyService, MyClient, MyRepository
- Endpoints: MyEndpoints (static class) with MapMyEndpoints extension method
- Enums: PascalCase names, SCREAMING_CASE values
- Test files: {Concept}Tests.cs (e.g. ConfidenceTests.cs, not ConfidenceTest.cs)

## Endpoints
- Use static extension methods on WebApplication (e.g. app.MapMyEndpoints())
- Always include .WithTags(), .WithName(), .WithOpenApi() on endpoints
- Health check endpoint at GET /health on every service

## Testing
- xUnit + Moq for unit tests
- Centralise mock creation in a Helpers/MockFactory.cs
- Test files grouped by concept, not by class
- Tests should be isolated — no dependency on shared mutable state across tests
- Events Service exposes a simulated clock (POST /clock) for time-based testing across services

## Communication
- Upstream calls use typed HttpClient with 5-second timeout
- Clients return null on failure — callers handle partial data
- Call independent upstream services in parallel with Task.WhenAll

## Do NOT
- Use controllers — this is a Minimal API project
- Share databases between services
- Use new keyword for dependencies — always inject via constructor
- Put multiple unrelated types in one file
- Cross bounded context boundaries — do not make decisions about another service's domain

## Tech Stack
- Always use the latest stable .NET and ASP.NET Core versions
- Always use the latest stable Swashbuckle.AspNetCore for Swagger/OpenAPI
- Docker with multi-stage builds (SDK → aspnet runtime) — use latest stable tags
- Dashboard: Vite + React (JSX) + Tailwind CSS — use latest stable versions
- When adding NuGet packages, always use the latest stable release

## Git Conventions
- Branch naming: feat/{feature-name} (e.g. feat/pricing, feat/stock-service)
- Commit messages: conventional commits (feat:, fix:, test:, docs:)
- One branch per feature/service

## Docker
- Each service has its own Dockerfile using multi-stage build
- All services listen on port 8080 inside their container
- Docker Compose maps to external ports 5001–5006
- Use docker compose up --build {service} to rebuild a single service

## Error Handling
- HTTP clients catch all exceptions and return null — never throw on upstream failure
- Callers check for null and degrade gracefully
- Log warnings on upstream failures, not errors

## Data Conventions
- Timestamps in ISO 8601 / UTC
- IDs use format PREFIX-NNN (e.g. PUB-001, EVT-001, OFFER-001)
- Enum values serialised as strings in JSON (use JsonStringEnumConverter)

## Reference Documents
- docs/HLD.md — high-level architecture and bounded contexts
- docs/{service}-service.md — detailed spec per service (endpoints, models, business rules)
- Refer to these specs before implementing new features