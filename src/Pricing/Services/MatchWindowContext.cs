namespace Pricing.Services;

public record MatchWindowContext(
    DateTime Timestamp,
    bool IsActive,
    double DemandMultiplier,
    DateTime? EndTime);
