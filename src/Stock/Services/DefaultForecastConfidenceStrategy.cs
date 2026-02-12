using Stock.Models;

namespace Stock.Services;

public class DefaultForecastConfidenceStrategy : IForecastConfidenceStrategy
{
    public string CalculateConfidence(DemandMultiplierResponse demandResult, double currentLevel, bool willDeplete)
    {
        return demandResult switch
        {
            { IsDefault: true } => "LOW",
            { Multiplier: >= 2.0 } => "HIGH",
            { Multiplier: >= 1.5 } => "MEDIUM",
            _ => "LOW"
        };
    }

    public string GenerateRecommendation(double currentLevel, bool willDeplete)
    {
        return currentLevel switch
        {
            <= 10 => "URGENT: Restock immediately",
            <= 30 when willDeplete => "Restock before match ends",
            _ => "Stock levels adequate"
        };
    }
}
