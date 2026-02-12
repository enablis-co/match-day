using Aggregator.Models;

namespace Aggregator.Services;

public interface IRiskCalculator
{
    RiskLevel CalculateRiskLevel(EventSummary? events, StockSummary? stock, StaffingSummary? staffing);

    OverallStatus CalculateOverallStatus(bool matchDay, StockSummary? stock, StaffingSummary? staffing);
}
