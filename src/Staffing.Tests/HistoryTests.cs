using Staffing.Services;
using Staffing.Tests.Helpers;

namespace Staffing.Tests;

public class HistoryTests
{
    [Fact]
    public async Task Recommendation_Is_Stored_In_History()
    {
        var uniqueEvent = $"TestMatch-{Guid.NewGuid():N}";
        var events = MockFactory.CreateEventsClient(demandMultiplier: 2.0, description: uniqueEvent);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        await service.GetRecommendationAsync("PUB-001");

        var history = service.GetHistory("PUB-001");

        Assert.NotEmpty(history.Recommendations);
        var matching = history.Recommendations.Where(r => r.Event == uniqueEvent).ToList();
        Assert.NotEmpty(matching);
        Assert.Equal("PENDING", matching.Last().Outcome);
    }

    [Fact]
    public async Task History_Returns_Correct_PubId()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        var history = service.GetHistory("PUB-099");

        Assert.Equal("PUB-099", history.PubId);
    }

    [Fact]
    public async Task No_Event_Stores_No_Event_In_History()
    {
        var events = MockFactory.CreateEventsClient(demandMultiplier: 1.0, matchActive: false);
        var stock = MockFactory.CreateStockClient(returnsStub: true);
        var service = new StaffingService(events.Object, stock.Object, MockFactory.CreateLogger<StaffingService>());

        await service.GetRecommendationAsync("PUB-001");

        var history = service.GetHistory("PUB-001");
        var matching = history.Recommendations.Where(r => r.Event == "No event").ToList();
        Assert.NotEmpty(matching);
    }
}
