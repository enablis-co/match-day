namespace Aggregator.Models;

public record StockSummary(
    int AlertCount,
    List<string> CriticalItems,
    string? EstimatedShortfall
);
