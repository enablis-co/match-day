namespace Aggregator.Models;

public record HealthResponse(
    string Status,
    ServiceHealthMap Services
);
