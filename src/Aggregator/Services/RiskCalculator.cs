using Aggregator.Models;

namespace Aggregator.Services;

public class RiskCalculator : IRiskCalculator
{
    public RiskLevel CalculateRiskLevel(EventSummary? events, StockSummary? stock, StaffingSummary? staffing)
    {
        if (stock == null)
        {
            return RiskLevel.UNKNOWN;
        }

        var hasActiveEvent = events?.Active ?? false;
        var hasStockAlert = stock.AlertCount > 0;
        var hasHighUrgencyStaffing = string.Equals(staffing?.Urgency, "HIGH", StringComparison.OrdinalIgnoreCase);

        var criticalConditions = 0;
        if (hasActiveEvent) criticalConditions++;
        if (hasStockAlert) criticalConditions++;
        if (hasHighUrgencyStaffing) criticalConditions++;

        if (criticalConditions >= 3)
        {
            return RiskLevel.CRITICAL;
        }

        if (hasActiveEvent && hasStockAlert)
        {
            return RiskLevel.HIGH;
        }

        if (hasActiveEvent || hasStockAlert)
        {
            return RiskLevel.MEDIUM;
        }

        return RiskLevel.LOW;
    }

    public OverallStatus CalculateOverallStatus(bool matchDay, StockSummary? stock, StaffingSummary? staffing)
    {
        var hasStockConcerns = stock != null && stock.AlertCount > 0;
        var hasStaffingConcerns = staffing != null &&
            string.Equals(staffing.Recommendation, "INCREASE", StringComparison.OrdinalIgnoreCase);

        var issueCount = 0;
        if (hasStockConcerns) issueCount++;
        if (hasStaffingConcerns) issueCount++;

        if (matchDay && issueCount >= 2)
        {
            return OverallStatus.CRITICAL;
        }

        if (matchDay && issueCount >= 1)
        {
            return OverallStatus.HIGH;
        }

        if (matchDay)
        {
            return OverallStatus.ELEVATED;
        }

        return OverallStatus.NORMAL;
    }
}
