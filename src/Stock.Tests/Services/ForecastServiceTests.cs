using Moq;
using Stock.Models;
using Stock.Services;

namespace Stock.Tests.Services;

public class ForecastServiceTests
{
    private static Mock<IStockQueryService> CreateStockQueryMock()
    {
        var mock = new Mock<IStockQueryService>();
        return mock;
    }

    private static Mock<IDemandMultiplierService> CreateDemandMultiplierMock(double multiplier = 1.0, bool isDefault = false)
    {
        var mock = new Mock<IDemandMultiplierService>();
        mock.Setup(m => m.GetDemandMultiplierAsync())
            .ReturnsAsync(new DemandMultiplierResponse(multiplier, IsDefault: isDefault, Source: "Test"));
        return mock;
    }

    private static Mock<IForecastConfidenceStrategy> CreateConfidenceStrategyMock()
    {
        var mock = new Mock<IForecastConfidenceStrategy>();
        mock.Setup(s => s.CalculateConfidence(It.IsAny<DemandMultiplierResponse>(), It.IsAny<double>(), It.IsAny<bool>()))
            .Returns("MEDIUM");
        mock.Setup(s => s.GenerateRecommendation(It.IsAny<double>(), It.IsAny<bool>()))
            .Returns("Test recommendation");
        return mock;
    }

    private static IStockCalculationService CreateCalculationService()
    {
        return new StockCalculationService();
    }

    private static StockLevel CreateStockLevel(string productId = "GUINNESS", double currentLevel = 50, double consumptionRate = 8)
    {
        return new StockLevel
        {
            Id = 1,
            PubId = "PUB-001",
            ProductId = productId,
            CurrentLevel = currentLevel,
            Product = new Product
            {
                ProductId = productId,
                ProductName = "Test Product",
                Category = "beer",
                Unit = "pints",
                BaseConsumptionRate = consumptionRate
            }
        };
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsNull_WhenStockNotFound()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((StockLevel?)null);
        var demandMock = CreateDemandMultiplierMock();
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsNull_WhenProductIsNull()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new StockLevel { Product = null });
        var demandMock = CreateDemandMultiplierMock();
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_ExecutesInParallel_StockAndDemandCalls()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel());
        var demandMock = CreateDemandMultiplierMock(1.5);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var startTime = DateTime.UtcNow;
        await service.GetForecastAsync("PUB-001", "GUINNESS");
        var duration = DateTime.UtcNow - startTime;

        stockMock.Verify(s => s.GetProductStockAsync("PUB-001", "GUINNESS"), Times.Once);
        demandMock.Verify(m => m.GetDemandMultiplierAsync(), Times.Once);
        Assert.True(duration.TotalMilliseconds < 500);
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsForecastWithCorrectBasicData()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel());
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("PUB-001", result.PubId);
        Assert.Equal("GUINNESS", result.ProductId);
        Assert.Equal("Test Product", result.ProductName);
        Assert.Equal(50, result.CurrentLevel);
        Assert.Equal(8, result.BaseConsumptionRate);
        Assert.Equal("pints_per_hour", result.RateUnit);
    }

    [Fact]
    public async Task GetForecastAsync_CalculatesAdjustedRate_WithMultiplier()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(consumptionRate: 10));
        var demandMock = CreateDemandMultiplierMock(2.5);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal(2.5, result.DemandMultiplier);
        Assert.Equal(25, result.AdjustedRate);
    }

    [Fact]
    public async Task GetForecastAsync_CalculatesHoursRemaining_Correctly()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 80, consumptionRate: 8));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal(10.0, result.HoursRemaining);
    }

    [Fact]
    public async Task GetForecastAsync_CalculatesDepletionTime_Correctly()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 40, consumptionRate: 10));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);
        var beforeTime = DateTime.UtcNow;

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.NotNull(result.EstimatedDepletionTime);
        Assert.True(result.EstimatedDepletionTime > beforeTime.AddHours(3.5));
        Assert.True(result.EstimatedDepletionTime < beforeTime.AddHours(4.5));
    }

    [Fact]
    public async Task GetForecastAsync_SetsWillDepleteTrue_WhenWithinWindow()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 20, consumptionRate: 10));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);

        Assert.NotNull(result);
        Assert.True(result.WillDepleteInWindow);
    }

    [Fact]
    public async Task GetForecastAsync_SetsWillDepleteFalse_WhenOutsideWindow()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 100, consumptionRate: 10));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);

        Assert.NotNull(result);
        Assert.False(result.WillDepleteInWindow);
    }

    [Fact]
    public async Task GetForecastAsync_HandlesZeroConsumptionRate()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(consumptionRate: 0));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Null(result.HoursRemaining);
        Assert.Null(result.EstimatedDepletionTime);
        Assert.False(result.WillDepleteInWindow);
    }

    [Fact]
    public async Task GetForecastAsync_UsesLowConfidence_WhenDemandIsDefault()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel());
        var demandMock = CreateDemandMultiplierMock(1.0, isDefault: true);
        var strategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategy, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("LOW", result.Confidence);
    }

    [Fact]
    public async Task GetForecastAsync_UsesHighConfidence_WhenMultiplierIsHigh()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel());
        var demandMock = CreateDemandMultiplierMock(2.5);
        var strategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategy, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("HIGH", result.Confidence);
    }

    [Fact]
    public async Task GetForecastAsync_UsesMediumConfidence_WhenMultiplierIsModerate()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel());
        var demandMock = CreateDemandMultiplierMock(1.7);
        var strategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategy, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("MEDIUM", result.Confidence);
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsUrgentRecommendation_WhenLevelCritical()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 8));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategy, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("URGENT: Restock immediately", result.Recommendation);
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsRestockRecommendation_WhenLowAndWillDeplete()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 25, consumptionRate: 10));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategy, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);

        Assert.NotNull(result);
        Assert.Equal("Restock before match ends", result.Recommendation);
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsAdequateRecommendation_WhenStockHigh()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 100));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategy, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("Stock levels adequate", result.Recommendation);
    }

    [Fact]
    public async Task GetForecastAsync_UsesCustomHoursWindow()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateStockLevel(currentLevel: 60, consumptionRate: 10));
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS", hours: 8);

        Assert.NotNull(result);
        Assert.True(result.WillDepleteInWindow);
    }

    [Fact]
    public async Task GetForecastAsync_DoesNotCallDemandService_WhenStockNotFound()
    {
        var stockMock = CreateStockQueryMock();
        stockMock.Setup(s => s.GetProductStockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((StockLevel?)null);
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateConfidenceStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new ForecastService(stockMock.Object, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetForecastAsync("PUB-001", "GUINNESS");

        Assert.Null(result);
        demandMock.Verify(m => m.GetDemandMultiplierAsync(), Times.Once);
    }
}
