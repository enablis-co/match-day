using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class WeightRedistributionTests
{
    [Fact]
    public async Task All_Available_Weights_Sum_To_1()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        var totalWeight = result.Signals.Sum(s => s.Weight);
        Assert.Equal(1.0m, totalWeight);
    }

    [Fact]
    public async Task All_Available_Has_Standard_Weights()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        var eventsSignal = result.Signals.First(s => s.Source == SignalSource.EVENTS);
        var stockSignal = result.Signals.First(s => s.Source == SignalSource.STOCK);
        var historicalSignal = result.Signals.First(s => s.Source == SignalSource.HISTORICAL);

        Assert.Equal(0.6m, eventsSignal.Weight);
        Assert.Equal(0.3m, stockSignal.Weight);
        Assert.Equal(0.1m, historicalSignal.Weight);
    }

    [Fact]
    public async Task Stock_Unavailable_Redistributes_Weights_To_Sum_To_1()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        var totalWeight = result.Signals.Sum(s => s.Weight);
        Assert.Equal(1.0m, totalWeight);

        // No stock signal present
        Assert.DoesNotContain(result.Signals, s => s.Source == SignalSource.STOCK);
    }

    [Fact]
    public async Task Stock_Unavailable_Events_Weight_Increases()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        var eventsSignal = result.Signals.First(s => s.Source == SignalSource.EVENTS);
        // Events weight should be > 0.6 when stock is missing
        Assert.True(eventsSignal.Weight > 0.6m);
    }

    [Fact]
    public async Task Events_Unavailable_Weights_Still_Sum_To_1()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(overallPressure: "LOW", alertCount: 0);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        var totalWeight = result.Signals.Sum(s => s.Weight);
        Assert.Equal(1.0m, totalWeight);
    }

    [Fact]
    public async Task Both_Unavailable_Historical_Gets_Full_Weight()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Single(result.Signals);
        var historicalSignal = result.Signals.First();
        Assert.Equal(SignalSource.HISTORICAL, historicalSignal.Source);
        Assert.Equal(1.0m, historicalSignal.Weight);
    }
}
