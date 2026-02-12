using Microsoft.EntityFrameworkCore;
using Moq;
using Stock.Data;
using Stock.Enums;
using Stock.Models;
using Stock.Services;

namespace Stock.Tests.Services;

public class AlertServiceTests
{
    private static StockDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<StockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new StockDbContext(options);
        SeedTestData(context);
        return context;
    }

    private static void SeedTestData(StockDbContext context)
    {
        var products = new[]
        {
            new Product { ProductId = "GUINNESS", ProductName = "Guinness Draught", Category = "beer", Unit = "pints", BaseConsumptionRate = 8 },
            new Product { ProductId = "STELLA", ProductName = "Stella Artois", Category = "beer", Unit = "pints", BaseConsumptionRate = 5 },
            new Product { ProductId = "JAMESON", ProductName = "Jameson Irish Whiskey", Category = "spirits", Unit = "shots", BaseConsumptionRate = 5 }
        };

        var stockLevels = new[]
        {
            new StockLevel { Id = 1, PubId = "PUB-001", ProductId = "GUINNESS", CurrentLevel = 16, LastUpdated = DateTime.UtcNow.AddHours(-2) },
            new StockLevel { Id = 2, PubId = "PUB-001", ProductId = "STELLA", CurrentLevel = 50, LastUpdated = DateTime.UtcNow.AddHours(-1) },
            new StockLevel { Id = 3, PubId = "PUB-001", ProductId = "JAMESON", CurrentLevel = 8, LastUpdated = DateTime.UtcNow.AddMinutes(-30) }
        };

        context.Products.AddRange(products);
        context.StockLevels.AddRange(stockLevels);
        context.SaveChanges();
    }

    private static Mock<IDemandMultiplierService> CreateDemandMultiplierMock(double multiplier = 1.0)
    {
        var mock = new Mock<IDemandMultiplierService>();
        mock.Setup(m => m.GetDemandMultiplierAsync())
            .ReturnsAsync(new DemandMultiplierResponse(multiplier, IsDefault: false, Source: "Test"));
        return mock;
    }

    private static Mock<IAlertSeverityStrategy> CreateSeverityStrategyMock()
    {
        var mock = new Mock<IAlertSeverityStrategy>();
        mock.Setup(s => s.CalculateSeverity(It.IsAny<double>()))
            .Returns<double>(hours => hours switch
            {
                < 2 => AlertSeverity.CRITICAL,
                < 4 => AlertSeverity.HIGH,
                < 8 => AlertSeverity.MEDIUM,
                _ => AlertSeverity.LOW
            });
        mock.Setup(s => s.GenerateMessage(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<AlertSeverity>()))
            .Returns<string, double, AlertSeverity>((name, hours, severity) => $"{severity}: {name} alert");
        return mock;
    }

    private static IStockCalculationService CreateCalculationService()
    {
        return new StockCalculationService();
    }

    [Fact]
    public async Task GetStockAlertsAsync_ReturnsAlertsForItemsWithinThreshold()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 12);

        Assert.Equal(3, result.Count());
        Assert.Contains(result, a => a.ProductId == "GUINNESS");
        Assert.Contains(result, a => a.ProductId == "JAMESON");
        Assert.Contains(result, a => a.ProductId == "STELLA");
    }

    [Fact]
    public async Task GetStockAlertsAsync_ExecutesInParallel_DatabaseAndHttpCalls()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var startTime = DateTime.UtcNow;
        await service.GetStockAlertsAsync("PUB-001", 12);
        var duration = DateTime.UtcNow - startTime;

        demandMock.Verify(m => m.GetDemandMultiplierAsync(), Times.Once);
        Assert.True(duration.TotalMilliseconds < 500);
    }

    [Fact]
    public async Task GetStockAlertsAsync_ExcludesItemsAboveThreshold()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 2);

        Assert.DoesNotContain(result, a => a.ProductId == "STELLA");
    }

    [Fact]
    public async Task GetStockAlertsAsync_AdjustsConsumptionRateByMultiplier()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(2.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 12);
        var guinness = result.First(a => a.ProductId == "GUINNESS");

        Assert.Equal(8, guinness.ConsumptionRate);
        Assert.Equal(2.0, guinness.DemandMultiplier);
        Assert.Equal(16, guinness.AdjustedConsumptionRate);
    }

    [Fact]
    public async Task GetStockAlertsAsync_CalculatesHoursRemaining_Correctly()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 12);
        var guinness = result.First(a => a.ProductId == "GUINNESS");

        Assert.Equal(2.0, guinness.HoursRemaining);
    }

    [Fact]
    public async Task GetStockAlertsAsync_SetsEstimatedDepletionTime()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);
        var beforeTime = DateTime.UtcNow;

        var result = await service.GetStockAlertsAsync("PUB-001", 12);
        var guinness = result.First(a => a.ProductId == "GUINNESS");

        Assert.NotNull(guinness.EstimatedDepletionTime);
        Assert.True(guinness.EstimatedDepletionTime > beforeTime);
        Assert.True(guinness.EstimatedDepletionTime < beforeTime.AddHours(3));
    }

    [Fact]
    public async Task GetStockAlertsAsync_UsesCustomThreshold()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 1.8);

        Assert.Single(result);
        Assert.Equal("JAMESON", result.First().ProductId);
    }

    [Fact]
    public async Task GetStockAlertsAsync_OrdersByHoursRemaining()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 12);
        var alerts = result.ToList();

        Assert.Equal("JAMESON", alerts[0].ProductId);
        Assert.Equal("GUINNESS", alerts[1].ProductId);
    }

    [Fact]
    public async Task GetStockAlertsAsync_CallsSeverityStrategy()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        await service.GetStockAlertsAsync("PUB-001", 12);

        strategyMock.Verify(s => s.CalculateSeverity(It.IsAny<double>()), Times.AtLeastOnce);
        strategyMock.Verify(s => s.GenerateMessage(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<AlertSeverity>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetStockAlertsAsync_SkipsItemsWithNullProduct()
    {
        using var context = CreateInMemoryContext();
        context.StockLevels.Add(new StockLevel 
        { 
            Id = 99, 
            PubId = "PUB-001", 
            ProductId = "ORPHAN", 
            CurrentLevel = 5, 
            Product = null 
        });
        context.SaveChanges();

        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 12);

        Assert.DoesNotContain(result, a => a.ProductId == "ORPHAN");
    }

    [Fact]
    public async Task GetStockAlertsAsync_HandlesZeroConsumptionRate()
    {
        using var context = CreateInMemoryContext();
        context.Products.Add(new Product 
        { 
            ProductId = "ZERO", 
            ProductName = "Zero Rate", 
            Category = "test", 
            Unit = "units", 
            BaseConsumptionRate = 0 
        });
        context.StockLevels.Add(new StockLevel 
        { 
            Id = 98, 
            PubId = "PUB-001", 
            ProductId = "ZERO", 
            CurrentLevel = 50 
        });
        context.SaveChanges();

        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetStockAlertsAsync("PUB-001", 12);

        Assert.DoesNotContain(result, a => a.ProductId == "ZERO");
    }

    [Fact]
    public async Task GetAlertsBySeverityAsync_FiltersBySeverity()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetAlertsBySeverityAsync("PUB-001", AlertSeverity.CRITICAL);

        Assert.All(result, alert => Assert.Equal(AlertSeverity.CRITICAL, alert.Severity));
    }

    [Fact]
    public async Task GetCriticalAlertsAsync_ReturnsCriticalAlerts()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetCriticalAlertsAsync("PUB-001");

        Assert.All(result, alert => Assert.Equal(AlertSeverity.CRITICAL, alert.Severity));
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsStockBelowThreshold()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetLowStockAlertsAsync("PUB-001", 30);

        Assert.Equal(2, result.Count());
        Assert.All(result, stock => Assert.True(stock.CurrentLevel <= 30));
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_UsesDefaultThreshold()
    {
        using var context = CreateInMemoryContext();
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var calculationService = CreateCalculationService();
        var service = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var result = await service.GetLowStockAlertsAsync("PUB-001");

        Assert.Equal(2, result.Count());
    }
}

