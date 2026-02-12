namespace Stock.Services;

public class StockCalculationService : IStockCalculationService
{
    public double CalculateAdjustedRate(double baseConsumptionRate, double demandMultiplier)
    {
        return baseConsumptionRate * demandMultiplier;
    }

    public double? CalculateHoursRemaining(double currentLevel, double adjustedRate)
    {
        return adjustedRate > 0 ? currentLevel / adjustedRate : null;
    }

    public DateTime? CalculateDepletionTime(double? hoursRemaining)
    {
        return hoursRemaining.HasValue 
            ? DateTime.UtcNow.AddHours(hoursRemaining.Value) 
            : null;
    }

    public bool WillDeplete(double? hoursRemaining, double thresholdHours)
    {
        return hoursRemaining.HasValue && hoursRemaining.Value <= thresholdHours;
    }
}
