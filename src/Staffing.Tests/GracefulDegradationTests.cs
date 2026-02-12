using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class GracefulDegradationTests
{
    [Fact]
    public async Task Events_Down_Returns_Maintain_With_Baseline_Multiplier()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(overallPressure: "LOW", alertCount: 0);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        // No events data → baseline multiplier 1.0 → MAINTAIN
        Assert.Equal(StaffingAction.MAINTAIN, result.Recommendation.Action);
        Assert.Equal(0, result.Recommendation.AdditionalStaff);
        Assert.Equal(Confidence.LOW, result.Confidence);
    }

    [Fact]
    public async Task Stock_Down_Still_Provides_Recommendation_From_Events()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        // Events available with 2.0 multiplier → still recommends +2
        Assert.Equal(StaffingAction.INCREASE, result.Recommendation.Action);
        Assert.Equal(2, result.Recommendation.AdditionalStaff);
        Assert.Equal(Confidence.MEDIUM, result.Confidence);
    }

    [Fact]
    public async Task Both_Down_Returns_Maintain_With_Low_Confidence()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(StaffingAction.MAINTAIN, result.Recommendation.Action);
        Assert.Equal(0, result.Recommendation.AdditionalStaff);
        Assert.Equal(Confidence.LOW, result.Confidence);
    }

    [Fact]
    public async Task Stock_Down_Excludes_Stock_Signal_From_Response()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.DoesNotContain(result.Signals, s => s.Source == SignalSource.STOCK);
    }

    [Fact]
    public async Task Events_Down_Excludes_Events_Signal_From_Response()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(overallPressure: "LOW", alertCount: 0);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.DoesNotContain(result.Signals, s => s.Source == SignalSource.EVENTS);
    }
}
