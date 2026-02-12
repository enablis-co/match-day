using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class SignalsEndpointTests
{
    [Fact]
    public async Task Signals_Returns_Demand_Multiplier_From_Events()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0, description: "England vs France");
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetSignalsAsync("PUB-001");

        Assert.Equal(2.0, result.Signals.DemandMultiplier);
        Assert.True(result.Signals.MatchWindowActive);
        Assert.Equal("England vs France", result.Signals.MatchDescription);
    }

    [Fact]
    public async Task Signals_Returns_Baseline_When_Events_Unavailable()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetSignalsAsync("PUB-001");

        Assert.Equal(1.0, result.Signals.DemandMultiplier);
        Assert.False(result.Signals.MatchWindowActive);
        Assert.Empty(result.Signals.MatchDescription);
    }

    [Fact]
    public async Task Signals_Returns_Stock_Pressure_When_Available()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.5);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 3);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetSignalsAsync("PUB-001");

        Assert.Equal("HIGH", result.Signals.StockPressure);
        Assert.Equal(3, result.Signals.StockAlerts);
    }

    [Fact]
    public async Task Signals_Returns_Default_Stock_When_Unavailable()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.5);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetSignalsAsync("PUB-001");

        Assert.Equal("NONE", result.Signals.StockPressure);
        Assert.Equal(0, result.Signals.StockAlerts);
    }

    [Fact]
    public async Task Signals_Includes_Correct_PubId()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetSignalsAsync("PUB-042");

        Assert.Equal("PUB-042", result.PubId);
    }

    [Fact]
    public async Task Signals_No_Active_Match_Returns_Baseline()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetSignalsAsync("PUB-001");

        Assert.Equal(1.0, result.Signals.DemandMultiplier);
        Assert.False(result.Signals.MatchWindowActive);
    }
}
