using Stock.Models;

namespace Stock.Services;

public interface IForecastConfidenceStrategy
{
    string CalculateConfidence(DemandMultiplierResponse demandResult, double currentLevel, bool willDeplete);
    string GenerateRecommendation(double currentLevel, bool willDeplete);
}
