using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class StaffingRecommendationTests
{
    // ──────────────────────────────────────────────────
    // Threshold tests: demand multiplier → staff delta
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Multiplier_Below_1_5_Returns_Maintain()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.2);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.MAINTAIN, result.Recommendation.Action);
        Assert.Equal(0, result.Recommendation.AdditionalStaff);
    }

    [Fact]
    public async Task Multiplier_1_5_Returns_Increase_By_1()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.5);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        Assert.Equal(1, result.Recommendation.AdditionalStaff);
    }

    [Fact]
    public async Task Multiplier_2_0_Returns_Increase_By_2()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        Assert.Equal(2, result.Recommendation.AdditionalStaff);
    }

    [Fact]
    public async Task Multiplier_Above_2_0_Still_Returns_2_Without_Stock()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 3.5);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        Assert.Equal(2, result.Recommendation.AdditionalStaff);
    }

    // ──────────────────────────────────────────────────
    // Stock pressure adds +1 staff
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task High_Stock_Pressure_Adds_1_Staff()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 2);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        // 2.0 multiplier = +2, HIGH stock = +1 → total +3
        Assert.Equal(3, result.Recommendation.AdditionalStaff);
    }

    [Fact]
    public async Task Low_Stock_Pressure_Does_Not_Add_Staff()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "LOW", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(2, result.Recommendation.AdditionalStaff);
    }

    [Fact]
    public async Task High_Stock_With_1_5_Multiplier_Gives_Plus_2()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.5);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        // 1.5 multiplier = +1, HIGH stock = +1 → total +2
        Assert.Equal(2, result.Recommendation.AdditionalStaff);
    }

    // ──────────────────────────────────────────────────
    // Max staff cap at +4
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Staff_Recommendation_Capped_At_4()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 4.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 5);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        // 4.0 multiplier = +2, HIGH stock = +1 → 3, but even extreme values should not exceed 4
        Assert.True(result.Recommendation.AdditionalStaff <= 4);
    }
}
