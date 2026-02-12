namespace Aggregator.Services.Tests;

public class RiskCalculatorTests
{
    private readonly RiskCalculator _calculator = new();

    // ── Risk Level ────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateRiskLevel_NoActiveEvents_NoAlerts_ReturnsLow()
    {
        var events = new EventSummary(Active: false, Current: null, DemandMultiplier: 1.0, EndsAt: null);
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateRiskLevel(events, stock, staffing);

        Assert.Equal(RiskLevel.LOW, result);
    }

    [Fact]
    public void CalculateRiskLevel_ActiveEventOnly_ReturnsMedium()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: DateTime.UtcNow.AddHours(1));
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateRiskLevel(events, stock, staffing);

        Assert.Equal(RiskLevel.MEDIUM, result);
    }

    [Fact]
    public void CalculateRiskLevel_StockAlertOnly_ReturnsMedium()
    {
        var events = new EventSummary(Active: false, Current: null, DemandMultiplier: 1.0, EndsAt: null);
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:45");
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateRiskLevel(events, stock, staffing);

        Assert.Equal(RiskLevel.MEDIUM, result);
    }

    [Fact]
    public void CalculateRiskLevel_ActiveEventAndStockAlert_ReturnsHigh()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: DateTime.UtcNow.AddHours(1));
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:45");
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateRiskLevel(events, stock, staffing);

        Assert.Equal(RiskLevel.HIGH, result);
    }

    [Fact]
    public void CalculateRiskLevel_MultipleCriticalConditions_ReturnsCritical()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: DateTime.UtcNow.AddHours(1));
        var stock = new StockSummary(AlertCount: 2, CriticalItems: ["Guinness", "Carling"], EstimatedShortfall: "18:00");
        var staffing = new StaffingSummary("INCREASE", 3, "HIGH", "HIGH");

        var result = _calculator.CalculateRiskLevel(events, stock, staffing);

        Assert.Equal(RiskLevel.CRITICAL, result);
    }

    [Fact]
    public void CalculateRiskLevel_NullStock_ReturnsUnknown()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: DateTime.UtcNow.AddHours(1));

        var result = _calculator.CalculateRiskLevel(events, null, null);

        Assert.Equal(RiskLevel.UNKNOWN, result);
    }

    // ── Overall Status ────────────────────────────────────────────────────────

    [Fact]
    public void CalculateOverallStatus_NoMatchDay_ReturnsNormal()
    {
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateOverallStatus(false, stock, staffing);

        Assert.Equal(OverallStatus.NORMAL, result);
    }

    [Fact]
    public void CalculateOverallStatus_MatchDayNoIssues_ReturnsElevated()
    {
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateOverallStatus(true, stock, staffing);

        Assert.Equal(OverallStatus.ELEVATED, result);
    }

    [Fact]
    public void CalculateOverallStatus_MatchDayWithStockConcern_ReturnsHigh()
    {
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:45");
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");

        var result = _calculator.CalculateOverallStatus(true, stock, staffing);

        Assert.Equal(OverallStatus.HIGH, result);
    }

    [Fact]
    public void CalculateOverallStatus_MatchDayWithStaffingConcern_ReturnsHigh()
    {
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");

        var result = _calculator.CalculateOverallStatus(true, stock, staffing);

        Assert.Equal(OverallStatus.HIGH, result);
    }

    [Fact]
    public void CalculateOverallStatus_MatchDayWithBothConcerns_ReturnsCritical()
    {
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:45");
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");

        var result = _calculator.CalculateOverallStatus(true, stock, staffing);

        Assert.Equal(OverallStatus.CRITICAL, result);
    }
}
