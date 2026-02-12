namespace Aggregator.Models;

public record SummaryResponse(
    string PubId,
    DateTime Timestamp,
    bool MatchDay,
    string RiskLevel,
    int ActionCount,
    string? TopAction
);
