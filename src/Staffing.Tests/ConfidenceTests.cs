using Staffing.Models;
using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class ConfidenceTests
{
    [Fact]
    public async Task All_Services_Available_Returns_High_Confidence()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(overallPressure: "HIGH", alertCount: 1);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Confidence.HIGH, result.Confidence);
    }

    [Fact]
    public async Task Stock_Unavailable_Returns_Medium_Confidence()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Confidence.MEDIUM, result.Confidence);
    }

    [Fact]
    public async Task Stock_Stub_Response_Returns_Medium_Confidence()
    {
        // This is the real-world case: Stock service returns 200 with stub data (empty pubId)
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Confidence.MEDIUM, result.Confidence);
    }

    [Fact]
    public async Task Events_Unavailable_Returns_Low_Confidence()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(overallPressure: "LOW", alertCount: 0);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Confidence.LOW, result.Confidence);
    }

    [Fact]
    public async Task Both_Unavailable_Returns_Low_Confidence()
    {
        var events = MockFactory.CreateEventsClient(returnsNull: true);
        var stock = MockFactory.CreateStockClient(returnsNull: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var result = await service.GetRecommendationAsync("PUB-001");

        Assert.Equal(Confidence.LOW, result.Confidence);
    }
}
