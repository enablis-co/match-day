namespace Stock.Services;

public interface IStockCalculationService
{
    double CalculateAdjustedRate(double baseConsumptionRate, double demandMultiplier);
    double? CalculateHoursRemaining(double currentLevel, double adjustedRate);
    DateTime? CalculateDepletionTime(double? hoursRemaining);
    bool WillDeplete(double? hoursRemaining, double thresholdHours);
}
