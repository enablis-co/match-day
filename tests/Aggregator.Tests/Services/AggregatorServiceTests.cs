namespace Aggregator.Services.Tests;

public class AggregatorServiceTests
{
    private readonly Mock<IEventsClient> _mockEvents;
    private readonly Mock<IPricingClient> _mockPricing;
    private readonly Mock<IStockClient> _mockStock;
    private readonly Mock<IStaffingClient> _mockStaffing;
    private readonly Mock<IRiskCalculator> _mockRiskCalculator;
    private readonly Mock<IActionPrioritiser> _mockActionPrioritiser;
    private readonly AggregatorService _service;

    private readonly DateTime _now = new(2025, 2, 10, 17, 30, 0, DateTimeKind.Utc);

    public AggregatorServiceTests()
    {
        _mockEvents = new Mock<IEventsClient>();
        _mockPricing = new Mock<IPricingClient>();
        _mockStock = new Mock<IStockClient>();
        _mockStaffing = new Mock<IStaffingClient>();
        _mockRiskCalculator = new Mock<IRiskCalculator>();
        _mockActionPrioritiser = new Mock<IActionPrioritiser>();

        _service = new AggregatorService(
            _mockEvents.Object,
            _mockPricing.Object,
            _mockStock.Object,
            _mockStaffing.Object,
            _mockRiskCalculator.Object,
            _mockActionPrioritiser.Object,
            Mock.Of<ILogger<AggregatorService>>());
    }

    [Fact]
    public async Task GetStatusAsync_AllServicesHealthy_ReturnsFullResponse()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: _now.AddHours(1));
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 2, SuspensionReason: "Match day rules");
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:45");
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");

        _mockEvents.Setup(e => e.GetActiveEventsAsync(_now)).ReturnsAsync(events);
        _mockPricing.Setup(p => p.GetActiveOffersAsync("PUB-001", _now)).ReturnsAsync(pricing);
        _mockStock.Setup(s => s.GetStockAlertsAsync("PUB-001")).ReturnsAsync(stock);
        _mockStaffing.Setup(s => s.GetRecommendationAsync("PUB-001", _now)).ReturnsAsync(staffing);
        _mockRiskCalculator.Setup(r => r.CalculateRiskLevel(events, stock, staffing)).Returns(RiskLevel.HIGH);
        _mockRiskCalculator.Setup(r => r.CalculateOverallStatus(true, stock, staffing)).Returns(OverallStatus.HIGH);
        _mockActionPrioritiser.Setup(a => a.PrioritiseActions(events, stock, staffing, pricing, _now)).Returns([]);

        var result = await _service.GetStatusAsync("PUB-001", _now);

        Assert.Equal("PUB-001", result.PubId);
        Assert.Equal("The Crown & Anchor", result.PubName);
        Assert.True(result.Status.MatchDay);
        Assert.Equal(OverallStatus.HIGH, result.Status.Overall);
        Assert.Equal(RiskLevel.HIGH, result.Status.RiskLevel);
        Assert.NotNull(result.Events);
        Assert.NotNull(result.Pricing);
        Assert.NotNull(result.Stock);
        Assert.NotNull(result.Staffing);
        Assert.Equal(ServiceStatus.OK, result.ServiceHealth.Events.Status);
        Assert.Equal(ServiceStatus.OK, result.ServiceHealth.Pricing.Status);
        Assert.Equal(ServiceStatus.OK, result.ServiceHealth.Stock.Status);
        Assert.Equal(ServiceStatus.OK, result.ServiceHealth.Staffing.Status);
    }

    [Fact]
    public async Task GetStatusAsync_EventsServiceFails_ReturnsPartialResponseWithDegradedHealth()
    {
        _mockEvents.Setup(e => e.GetActiveEventsAsync(_now)).ThrowsAsync(new HttpRequestException("Connection refused"));
        _mockPricing.Setup(p => p.GetActiveOffersAsync("PUB-001", _now)).ReturnsAsync(new PricingSummary(2, 0, null));
        _mockStock.Setup(s => s.GetStockAlertsAsync("PUB-001")).ReturnsAsync(new StockSummary(0, [], null));
        _mockStaffing.Setup(s => s.GetRecommendationAsync("PUB-001", _now)).ReturnsAsync(new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH"));
        _mockRiskCalculator.Setup(r => r.CalculateRiskLevel(null, It.IsAny<StockSummary>(), It.IsAny<StaffingSummary>())).Returns(RiskLevel.LOW);
        _mockRiskCalculator.Setup(r => r.CalculateOverallStatus(false, It.IsAny<StockSummary>(), It.IsAny<StaffingSummary>())).Returns(OverallStatus.NORMAL);
        _mockActionPrioritiser.Setup(a => a.PrioritiseActions(null, It.IsAny<StockSummary>(), It.IsAny<StaffingSummary>(), It.IsAny<PricingSummary>(), _now)).Returns([]);

        var result = await _service.GetStatusAsync("PUB-001", _now);

        Assert.Null(result.Events);
        Assert.False(result.Status.MatchDay);
        Assert.Equal(ServiceStatus.DEGRADED, result.ServiceHealth.Events.Status);
        Assert.Equal(ServiceStatus.OK, result.ServiceHealth.Pricing.Status);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsSimplifiedStatus()
    {
        var events = new EventSummary(Active: true, Current: "England vs France", DemandMultiplier: 2.0, EndsAt: _now.AddHours(1));
        var pricing = new PricingSummary(OffersActive: 0, OffersSuspended: 2, SuspensionReason: "Match day rules");
        var stock = new StockSummary(AlertCount: 1, CriticalItems: ["Guinness"], EstimatedShortfall: "18:45");
        var staffing = new StaffingSummary("INCREASE", 2, "HIGH", "HIGH");

        _mockEvents.Setup(e => e.GetActiveEventsAsync(_now)).ReturnsAsync(events);
        _mockPricing.Setup(p => p.GetActiveOffersAsync("PUB-001", _now)).ReturnsAsync(pricing);
        _mockStock.Setup(s => s.GetStockAlertsAsync("PUB-001")).ReturnsAsync(stock);
        _mockStaffing.Setup(s => s.GetRecommendationAsync("PUB-001", _now)).ReturnsAsync(staffing);
        _mockRiskCalculator.Setup(r => r.CalculateRiskLevel(events, stock, staffing)).Returns(RiskLevel.HIGH);
        _mockRiskCalculator.Setup(r => r.CalculateOverallStatus(true, stock, staffing)).Returns(OverallStatus.HIGH);

        var restockAction = new PubAction(1, "RESTOCK_GUINNESS", "Will deplete before 18:00", new DateTime(2025, 2, 10, 18, 0, 0, DateTimeKind.Utc), ActionSource.STOCK);
        _mockActionPrioritiser.Setup(a => a.PrioritiseActions(events, stock, staffing, pricing, _now)).Returns([restockAction]);

        var result = await _service.GetSummaryAsync("PUB-001", _now);

        Assert.Equal("PUB-001", result.PubId);
        Assert.True(result.MatchDay);
        Assert.Equal("HIGH", result.RiskLevel);
        Assert.Equal(1, result.ActionCount);
        Assert.Contains("18:00", result.TopAction!);
    }

    [Fact]
    public async Task GetActionsAsync_ReturnsOnlyActions()
    {
        var events = new EventSummary(Active: false, Current: null, DemandMultiplier: 1.0, EndsAt: null);
        _mockEvents.Setup(e => e.GetActiveEventsAsync(_now)).ReturnsAsync(events);
        _mockPricing.Setup(p => p.GetActiveOffersAsync("PUB-001", _now)).ReturnsAsync(new PricingSummary(0, 0, null));
        _mockStock.Setup(s => s.GetStockAlertsAsync("PUB-001")).ReturnsAsync(new StockSummary(0, [], null));
        _mockStaffing.Setup(s => s.GetRecommendationAsync("PUB-001", _now)).ReturnsAsync(new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH"));
        _mockRiskCalculator.Setup(r => r.CalculateRiskLevel(It.IsAny<EventSummary>(), It.IsAny<StockSummary>(), It.IsAny<StaffingSummary>())).Returns(RiskLevel.LOW);
        _mockRiskCalculator.Setup(r => r.CalculateOverallStatus(false, It.IsAny<StockSummary>(), It.IsAny<StaffingSummary>())).Returns(OverallStatus.NORMAL);
        _mockActionPrioritiser.Setup(a => a.PrioritiseActions(It.IsAny<EventSummary>(), It.IsAny<StockSummary>(), It.IsAny<StaffingSummary>(), It.IsAny<PricingSummary>(), _now)).Returns([]);

        var result = await _service.GetActionsAsync("PUB-001", _now);

        Assert.Equal("PUB-001", result.PubId);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public async Task GetHealthAsync_AllServicesUp_ReturnsHealthy()
    {
        _mockEvents.Setup(e => e.GetActiveEventsAsync(null)).ReturnsAsync(new EventSummary(false, null, 1.0, null));
        _mockPricing.Setup(p => p.GetActiveOffersAsync("PUB-001", null)).ReturnsAsync(new PricingSummary(0, 0, null));
        _mockStock.Setup(s => s.GetStockAlertsAsync("PUB-001")).ReturnsAsync(new StockSummary(0, [], null));
        _mockStaffing.Setup(s => s.GetRecommendationAsync("PUB-001", null)).ReturnsAsync(new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH"));

        var result = await _service.GetHealthAsync();

        Assert.Equal("HEALTHY", result.Status);
        Assert.Equal(ServiceStatus.OK, result.Services.Events.Status);
        Assert.Equal(ServiceStatus.OK, result.Services.Pricing.Status);
        Assert.Equal(ServiceStatus.OK, result.Services.Stock.Status);
        Assert.Equal(ServiceStatus.OK, result.Services.Staffing.Status);
    }

    [Fact]
    public async Task GetHealthAsync_ServiceDown_ReturnsDegraded()
    {
        _mockEvents.Setup(e => e.GetActiveEventsAsync(null)).ThrowsAsync(new HttpRequestException());
        _mockPricing.Setup(p => p.GetActiveOffersAsync("PUB-001", null)).ReturnsAsync(new PricingSummary(0, 0, null));
        _mockStock.Setup(s => s.GetStockAlertsAsync("PUB-001")).ReturnsAsync(new StockSummary(0, [], null));
        _mockStaffing.Setup(s => s.GetRecommendationAsync("PUB-001", null)).ReturnsAsync(new StaffingSummary("MAINTAIN", 0, "LOW", "HIGH"));

        var result = await _service.GetHealthAsync();

        Assert.Equal("DEGRADED", result.Status);
        Assert.Equal(ServiceStatus.DEGRADED, result.Services.Events.Status);
    }
}
