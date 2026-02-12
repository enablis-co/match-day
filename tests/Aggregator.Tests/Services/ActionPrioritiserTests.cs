namespace Aggregator.Services.Tests;

public class ActionPrioritiserTests
{
    private readonly ActionPrioritiser _prioritiser = new();
    private readonly DateTime _now = new(2025, 2, 10, 17, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void PrioritiseActions_NoIssues_ReturnsEmptyList()
    {
        var events = new EventSummary(Active: false, Current: null, DemandMultiplier: 1.0, EndsAt: null);
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");
        var pricing = new PricingSummary(OffersActive: 2, OffersSuspended: 0, SuspensionReason: null);

        var result = _prioritiser.PrioritiseActions(events, stock, staffing, pricing, _now);

        Assert.Empty(result);
    }

    [Fact]
    public void PrioritiseActions_StockDepletion_UnderOneHour_IsPriority1()
    {
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:00");
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 0, SuspensionReason: null);

        var result = _prioritiser.PrioritiseActions(null, stock, staffing, pricing, _now);

        Assert.Single(result);
        Assert.Equal(1, result[0].Priority);
        Assert.Equal("RESTOCK_GUINNESS", result[0].Action);
        Assert.Equal(ActionSource.STOCK, result[0].Source);
    }

    [Fact]
    public void PrioritiseActions_HighUrgencyStaffing_ComesBeforeLowUrgencyStock()
    {
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Lager"], EstimatedShortfall: "20:00");
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 0, SuspensionReason: null);

        var result = _prioritiser.PrioritiseActions(null, stock, staffing, pricing, _now);

        Assert.Equal(2, result.Count);
        Assert.Equal("CALL_EXTRA_STAFF", result[0].Action);
        Assert.Equal("RESTOCK_LAGER", result[1].Action);
    }

    [Fact]
    public void PrioritiseActions_SuspendedOffers_IncludesPricingAction()
    {
        var stock = new StockSummary(AlertCount: 0, CriticalItems: [], EstimatedShortfall: null);
        var staffing = new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH");
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 2, SuspensionReason: "Match day rules");

        var result = _prioritiser.PrioritiseActions(null, stock, staffing, pricing, _now);

        Assert.Single(result);
        Assert.Equal("REVIEW_SUSPENDED_OFFERS", result[0].Action);
        Assert.Equal("Match day rules", result[0].Reason);
        Assert.Equal(ActionSource.PRICING, result[0].Source);
    }

    [Fact]
    public void PrioritiseActions_FullScenario_CorrectPriorityOrder()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: _now.AddHours(1.5));
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:00");
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 2, SuspensionReason: "Match day rules");

        var result = _prioritiser.PrioritiseActions(events, stock, staffing, pricing, _now);

        Assert.Equal(3, result.Count);
        Assert.Equal("RESTOCK_GUINNESS", result[0].Action);
        Assert.Equal("CALL_EXTRA_STAFF", result[1].Action);
        Assert.Equal("REVIEW_SUSPENDED_OFFERS", result[2].Action);
        Assert.Equal(1, result[0].Priority);
        Assert.Equal(2, result[1].Priority);
        Assert.Equal(3, result[2].Priority);
    }

    [Fact]
    public void PrioritiseActions_NullInputs_ReturnsEmptyList()
    {
        var result = _prioritiser.PrioritiseActions(null, null, null, null, _now);

        Assert.Empty(result);
    }

    [Fact]
    public void PrioritiseActions_SequentialPriorities_AreAssignedCorrectly()
    {
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:00");
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 1, SuspensionReason: "Match day");

        var result = _prioritiser.PrioritiseActions(null, stock, staffing, pricing, _now);

        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(i + 1, result[i].Priority);
        }
    }
}
