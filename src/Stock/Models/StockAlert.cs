using Stock.Enums;

namespace Stock.Models;

public record StockAlert(
    string PubId,
    string ProductId,
    string ProductName,
    string Category,
    double CurrentLevel,
    string Unit,
    double ConsumptionRate,
    double DemandMultiplier,
    double AdjustedConsumptionRate,
    double? HoursRemaining,
    DateTime? EstimatedDepletionTime,
    AlertSeverity Severity,
    string Message
);