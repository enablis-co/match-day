using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class UrgencyTests
{
    [Fact]
    public async Task Low_Multiplier_No_Stock_Returns_Low_Urgency()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Urgency.LOW, result.Recommendation.Urgency);
    }

    [Fact]
    public async Task Multiplier_1_5_Returns_Medium_Urgency()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.5);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Urgency.MEDIUM, result.Recommendation.Urgency);
    }

    [Fact]
    public async Task Multiplier_2_0_Returns_High_Urgency()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Urgency.HIGH, result.Recommendation.Urgency);
    }

    [Fact]
    public async Task High_Stock_Pressure_Returns_High_Urgency_Even_With_Low_Multiplier()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Urgency.HIGH, result.Recommendation.Urgency);
    }
}
