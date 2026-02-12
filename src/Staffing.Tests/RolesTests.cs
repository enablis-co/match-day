using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class RolesTests
{
    [Fact]
    public async Task Maintain_Returns_No_Roles()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Empty(result.Recommendation.Roles);
    }

    [Fact]
    public async Task Plus_1_Staff_Includes_Bar_Role()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.5);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Contains("bar", result.Recommendation.Roles);
        Assert.DoesNotContain("floor", result.Recommendation.Roles);
    }

    [Fact]
    public async Task Plus_2_Staff_Includes_Bar_And_Floor()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Contains("bar", result.Recommendation.Roles);
        Assert.Contains("floor", result.Recommendation.Roles);
    }

    [Fact]
    public async Task High_Stock_Pressure_Adds_Cellar_Role()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Contains("cellar", result.Recommendation.Roles);
    }

    [Fact]
    public async Task Low_Stock_Pressure_Does_Not_Add_Cellar_Role()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "LOW", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.DoesNotContain("cellar", result.Recommendation.Roles);
    }
}
