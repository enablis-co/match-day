using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class WorkshopScenarioTests
{
    /// <summary>
    /// Scenario 1 from the spec: Normal day — low demand, no stock alerts → MAINTAIN
    /// </summary>
    [Fact]
    public async Task Scenario1_NormalDay_Returns_Maintain()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(overallPressure: "NONE", alertCount: 0);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.MAINTAIN, result.Recommendation.Action);
        Assert.Equal(0, result.Recommendation.AdditionalStaff);
        Assert.Equal(Urgency.LOW, result.Recommendation.Urgency);
    }

    /// <summary>
    /// Scenario 2 from the spec: Match day — multiplier 2.0, stock HIGH → INCREASE by 3, urgency HIGH
    /// </summary>
    [Fact]
    public async Task Scenario2_MatchDay_Returns_Increase_By_3_High_Urgency()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0, description: "England vs France");
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        // 2.0 multiplier = +2, HIGH stock = +1 → total +3
        Assert.Equal(3, result.Recommendation.AdditionalStaff);
        Assert.Equal(Urgency.HIGH, result.Recommendation.Urgency);
        Assert.Equal(Confidence.HIGH, result.Confidence);
    }

    /// <summary>
    /// Scenario 3 from the spec: The twist — multiplier jumps to 4.0 → +3 (2 from demand + 1 from HIGH stock), capped at 4
    /// </summary>
    [Fact]
    public async Task Scenario3_TheTwist_HighMultiplier_Returns_Increase_Capped()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 4.0, description: "England vs France (DOUBLED)");
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 2);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        // 4.0 multiplier = +2 (same as 2.0), HIGH stock = +1 → 3, within cap of 4
        Assert.True(result.Recommendation.AdditionalStaff >= 3);
        Assert.True(result.Recommendation.AdditionalStaff <= 4);
        Assert.Equal(Urgency.HIGH, result.Recommendation.Urgency);
    }

    /// <summary>
    /// Scenario from spec: Conflicting signals — high demand but Events service is slow to update
    /// Simulated by Events being unavailable
    /// </summary>
    [Fact]
    public async Task Scenario_EventsSlowToUpdate_Degrades_Gracefully()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 2);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        // No events → baseline 1.0, but HIGH stock still adds +1
        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        Assert.Equal(1, result.Recommendation.AdditionalStaff);
        Assert.Equal(Confidence.LOW, result.Confidence);
    }
}
