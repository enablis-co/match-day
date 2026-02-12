namespace Aggregator.Models;

public record EventSummary(
    bool Active,
    string? Current,
    double DemandMultiplier,
    DateTime? EndsAt
);
