namespace Stock.Services;

public record ForecastResult(
    string PubId,
    string ProductId,
    string ProductName,
    double CurrentLevel,
    double BaseConsumptionRate,
    string RateUnit,
    double DemandMultiplier,
    double AdjustedRate,
    DateTime? EstimatedDepletionTime,
    double? HoursRemaining,
    bool WillDepleteInWindow,
    string Confidence,
    string Recommendation
);

public interface IForecastService
{
    Task<ForecastResult?> GetForecastAsync(string pubId, string productId, int hours = 4);
}