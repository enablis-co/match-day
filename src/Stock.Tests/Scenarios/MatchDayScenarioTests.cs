using Microsoft.EntityFrameworkCore;
using Moq;
using Stock.Data;
using Stock.Enums;
using Stock.Models;
using Stock.Services;

namespace Stock.Tests.Scenarios;

public class MatchDayScenarioTests
{
    private static StockDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<StockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new StockDbContext(options);
        SeedMatchDayScenarioData(context);
        return context;
    }

    private static void SeedMatchDayScenarioData(StockDbContext context)
    {
        var products = new[]
        {
            new Product { ProductId = "GUINNESS", ProductName = "Guinness Draught", Category = "beer", Unit = "pints", BaseConsumptionRate = 8 },
            new Product { ProductId = "STELLA", ProductName = "Stella Artois", Category = "beer", Unit = "pints", BaseConsumptionRate = 5 },
            new Product { ProductId = "CARLSBERG", ProductName = "Carlsberg", Category = "beer", Unit = "pints", BaseConsumptionRate = 7 },
            new Product { ProductId = "JAMESON", ProductName = "Jameson Irish Whiskey", Category = "spirits", Unit = "shots", BaseConsumptionRate = 5 }
        };

        var stockLevels = new[]
        {
            new StockLevel { Id = 1, PubId = "PUB-001", ProductId = "GUINNESS", CurrentLevel = 80, LastUpdated = DateTime.UtcNow.AddHours(-2) },
            new StockLevel { Id = 2, PubId = "PUB-001", ProductId = "STELLA", CurrentLevel = 60, LastUpdated = DateTime.UtcNow.AddHours(-1) },
            new StockLevel { Id = 3, PubId = "PUB-001", ProductId = "CARLSBERG", CurrentLevel = 50, LastUpdated = DateTime.UtcNow.AddHours(-1.5) },
            new StockLevel { Id = 4, PubId = "PUB-001", ProductId = "JAMESON", CurrentLevel = 30, LastUpdated = DateTime.UtcNow.AddMinutes(-45) }
        };

        context.Products.AddRange(products);
        context.StockLevels.AddRange(stockLevels);
        context.SaveChanges();
    }

    private static Mock<IDemandMultiplierService> CreateDemandMultiplierMock(double multiplier, bool isDefault = false)
    {
        var mock = new Mock<IDemandMultiplierService>();
        mock.Setup(m => m.GetDemandMultiplierAsync())
            .ReturnsAsync(new DemandMultiplierResponse(multiplier, IsDefault: isDefault, Source: isDefault ? "DefaultFallback" : "EventsService"));
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
            .Returns<string, double, AlertSeverity>((name, hours, severity) => 
                $"{severity}: {name} will deplete in {hours:F1} hours");
        return mock;
    }

    private static IStockCalculationService CreateCalculationService()
    {
        return new StockCalculationService();
    }

    [Fact]
    public async Task Scenario1_NormalAfternoon_AllStockLevelsAdequate()
    {
        using var context = CreateInMemoryContext();
        var stockQueryService = new StockService(context);
        var demandMock = CreateDemandMultiplierMock(1.0);
        var strategyMock = CreateSeverityStrategyMock();
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        
        var calculationService = CreateCalculationService();
        var forecastService = new ForecastService(stockQueryService, demandMock.Object, confidenceStrategy, calculationService);
        var alertService = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var guinnesseForecast = await forecastService.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var stellaForecast = await forecastService.GetForecastAsync("PUB-001", "STELLA", hours: 4);
        var alerts = await alertService.GetStockAlertsAsync("PUB-001", hoursThreshold: 4);

        Assert.NotNull(guinnesseForecast);
        Assert.Equal(1.0, guinnesseForecast.DemandMultiplier);
        Assert.Equal(8.0, guinnesseForecast.AdjustedRate);
        Assert.Equal(10.0, guinnesseForecast.HoursRemaining);
        Assert.False(guinnesseForecast.WillDepleteInWindow);
        Assert.Equal("Stock levels adequate", guinnesseForecast.Recommendation);
        Assert.Equal("LOW", guinnesseForecast.Confidence);

        Assert.NotNull(stellaForecast);
        Assert.Equal(12.0, stellaForecast.HoursRemaining);
        Assert.False(stellaForecast.WillDepleteInWindow);
        
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task Scenario2_MatchKicksOff_GuinnessBecomesUrgent()
    {
        using var context = CreateInMemoryContext();
        var stockQueryService = new StockService(context);
        var demandMock = CreateDemandMultiplierMock(2.0);
        var strategyMock = CreateSeverityStrategyMock();
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        
        var calculationService = CreateCalculationService();
        var forecastService = new ForecastService(stockQueryService, demandMock.Object, confidenceStrategy, calculationService);
        var alertService = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var guinnesseForecast = await forecastService.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var alerts = await alertService.GetStockAlertsAsync("PUB-001", hoursThreshold: 12);
        var guinnessAlert = alerts.FirstOrDefault(a => a.ProductId == "GUINNESS");

        Assert.NotNull(guinnesseForecast);
        Assert.Equal(2.0, guinnesseForecast.DemandMultiplier);
        Assert.Equal(16.0, guinnesseForecast.AdjustedRate);
        Assert.Equal(5.0, guinnesseForecast.HoursRemaining);
        Assert.False(guinnesseForecast.WillDepleteInWindow);
        Assert.Equal("Stock levels adequate", guinnesseForecast.Recommendation);
        Assert.Equal("HIGH", guinnesseForecast.Confidence);

        Assert.NotNull(guinnessAlert);
        Assert.Equal(5.0, guinnessAlert.HoursRemaining);
        Assert.Equal(AlertSeverity.MEDIUM, guinnessAlert.Severity);
        Assert.Equal(16.0, guinnessAlert.AdjustedConsumptionRate);
    }

    [Fact]
    public async Task Scenario3_DemandDoubles_StockTransitionsFromSafeToAtRisk()
    {
        using var context = CreateInMemoryContext();
        var stockQueryService = new StockService(context);
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        var calculationService = CreateCalculationService();

        var normalDemandMock = CreateDemandMultiplierMock(1.0);
        var forecastServiceNormal = new ForecastService(stockQueryService, normalDemandMock.Object, confidenceStrategy, calculationService);
        var forecastNormal = await forecastServiceNormal.GetForecastAsync("PUB-001", "STELLA", hours: 8);

        var highDemandMock = CreateDemandMultiplierMock(2.0);
        var forecastServiceHigh = new ForecastService(stockQueryService, highDemandMock.Object, confidenceStrategy, calculationService);
        var forecastHigh = await forecastServiceHigh.GetForecastAsync("PUB-001", "STELLA", hours: 8);

        Assert.NotNull(forecastNormal);
        Assert.Equal(1.0, forecastNormal.DemandMultiplier);
        Assert.Equal(5.0, forecastNormal.AdjustedRate);
        Assert.Equal(12.0, forecastNormal.HoursRemaining);
        Assert.False(forecastNormal.WillDepleteInWindow);
        Assert.Equal("Stock levels adequate", forecastNormal.Recommendation);

        Assert.NotNull(forecastHigh);
        Assert.Equal(2.0, forecastHigh.DemandMultiplier);
        Assert.Equal(10.0, forecastHigh.AdjustedRate);
        Assert.Equal(6.0, forecastHigh.HoursRemaining);
        Assert.True(forecastHigh.WillDepleteInWindow);
        Assert.Equal("Stock levels adequate", forecastHigh.Recommendation);
    }

    [Fact]
    public async Task Scenario4_MatchEnds_DemandDrops_AlertsDisappear()
    {
        using var context = CreateInMemoryContext();
        var strategyMock = CreateSeverityStrategyMock();

        var calculationService = CreateCalculationService();

        var duringMatchDemandMock = CreateDemandMultiplierMock(2.5);
        var alertServiceDuringMatch = new AlertService(context, duringMatchDemandMock.Object, strategyMock.Object, calculationService);
        var alertsDuringMatch = await alertServiceDuringMatch.GetStockAlertsAsync("PUB-001", hoursThreshold: 3);

        var afterMatchDemandMock = CreateDemandMultiplierMock(1.0);
        var alertServiceAfterMatch = new AlertService(context, afterMatchDemandMock.Object, strategyMock.Object, calculationService);
        var alertsAfterMatch = await alertServiceAfterMatch.GetStockAlertsAsync("PUB-001", hoursThreshold: 3);

        var carlsbergDuringMatch = alertsDuringMatch.FirstOrDefault(a => a.ProductId == "CARLSBERG");
        Assert.NotNull(carlsbergDuringMatch);
        Assert.Equal(2.5, carlsbergDuringMatch.DemandMultiplier);
        Assert.True(carlsbergDuringMatch.HoursRemaining <= 3);

        var carlsbergAfterMatch = alertsAfterMatch.FirstOrDefault(a => a.ProductId == "CARLSBERG");
        Assert.Null(carlsbergAfterMatch);
    }

    [Fact]
    public async Task Scenario5_MultipleProducts_DifferentImpactLevels()
    {
        using var context = CreateInMemoryContext();
        var stockQueryService = new StockService(context);
        var demandMock = CreateDemandMultiplierMock(2.0);
        var strategyMock = CreateSeverityStrategyMock();
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        
        var calculationService = CreateCalculationService();
        var forecastService = new ForecastService(stockQueryService, demandMock.Object, confidenceStrategy, calculationService);
        var alertService = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var guinnesseForecast = await forecastService.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var stellaForecast = await forecastService.GetForecastAsync("PUB-001", "STELLA", hours: 4);
        var carlsbergForecast = await forecastService.GetForecastAsync("PUB-001", "CARLSBERG", hours: 4);
        var jamesonForecast = await forecastService.GetForecastAsync("PUB-001", "JAMESON", hours: 4);

        Assert.NotNull(guinnesseForecast);
        Assert.Equal(5.0, guinnesseForecast.HoursRemaining);
        Assert.False(guinnesseForecast.WillDepleteInWindow);

        Assert.NotNull(stellaForecast);
        Assert.Equal(6.0, stellaForecast.HoursRemaining);
        Assert.False(stellaForecast.WillDepleteInWindow);

        Assert.NotNull(carlsbergForecast);
        Assert.NotNull(carlsbergForecast.HoursRemaining);
        Assert.Equal(50.0 / 14.0, carlsbergForecast.HoursRemaining.Value, precision: 1);
        Assert.True(carlsbergForecast.WillDepleteInWindow);

        Assert.NotNull(jamesonForecast);
        Assert.Equal(3.0, jamesonForecast.HoursRemaining);
        Assert.True(jamesonForecast.WillDepleteInWindow);

        var alerts = await alertService.GetStockAlertsAsync("PUB-001", hoursThreshold: 4);
        Assert.Equal(2, alerts.Count());
        Assert.Contains(alerts, a => a.ProductId == "CARLSBERG");
        Assert.Contains(alerts, a => a.ProductId == "JAMESON");
        Assert.DoesNotContain(alerts, a => a.ProductId == "GUINNESS");
        Assert.DoesNotContain(alerts, a => a.ProductId == "STELLA");
    }

    [Fact]
    public async Task Scenario6_EventsServiceDown_FallsBackToNormalDemand()
    {
        using var context = CreateInMemoryContext();
        var stockQueryService = new StockService(context);
        var demandMock = CreateDemandMultiplierMock(1.0, isDefault: true);
        var strategyMock = CreateSeverityStrategyMock();
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        
        var calculationService = CreateCalculationService();
        var forecastService = new ForecastService(stockQueryService, demandMock.Object, confidenceStrategy, calculationService);
        var alertService = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var guinnesseForecast = await forecastService.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);

        Assert.NotNull(guinnesseForecast);
        Assert.Equal(1.0, guinnesseForecast.DemandMultiplier);
        Assert.Equal(8.0, guinnesseForecast.AdjustedRate);
        Assert.Equal(10.0, guinnesseForecast.HoursRemaining);
        Assert.False(guinnesseForecast.WillDepleteInWindow);
        Assert.Equal("LOW", guinnesseForecast.Confidence);
    }

    [Fact]
    public async Task Scenario7_CriticalStock_UrgentRecommendation_RegardlessOfDemand()
    {
        using var context = CreateInMemoryContext();
        
        context.Products.Add(new Product 
        { 
            ProductId = "EMERGENCY", 
            ProductName = "Emergency Stock", 
            Category = "test", 
            Unit = "units", 
            BaseConsumptionRate = 2 
        });
        context.StockLevels.Add(new StockLevel 
        { 
            Id = 99, 
            PubId = "PUB-001", 
            ProductId = "EMERGENCY", 
            CurrentLevel = 5 
        });
        context.SaveChanges();

        var stockQueryService = new StockService(context);
        var normalDemandMock = CreateDemandMultiplierMock(1.0);
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        
        var calculationService = CreateCalculationService();
        var forecastService = new ForecastService(stockQueryService, normalDemandMock.Object, confidenceStrategy, calculationService);
        var forecast = await forecastService.GetForecastAsync("PUB-001", "EMERGENCY");

        Assert.NotNull(forecast);
        Assert.Equal("URGENT: Restock immediately", forecast.Recommendation);
    }

    [Fact]
    public async Task Scenario8_AlertSeverity_EscalatesWithTime()
    {
        using var context = CreateInMemoryContext();
        var strategyMock = CreateSeverityStrategyMock();

        var demandMock = CreateDemandMultiplierMock(2.5);
        var calculationService = CreateCalculationService();
        var alertService = new AlertService(context, demandMock.Object, strategyMock.Object, calculationService);

        var alerts = await alertService.GetStockAlertsAsync("PUB-001", hoursThreshold: 12);
        var sortedAlerts = alerts.OrderBy(a => a.HoursRemaining).ToList();

        Assert.True(sortedAlerts.Count > 0);
        for (int i = 0; i < sortedAlerts.Count - 1; i++)
        {
            var currentAlert = sortedAlerts[i];
            var nextAlert = sortedAlerts[i + 1];
            
            Assert.True(currentAlert.HoursRemaining <= nextAlert.HoursRemaining);
        }
    }

    [Fact]
    public async Task Scenario9_ConsumptionDuringMatch_StockDepletesQuicker()
    {
        using var context = CreateInMemoryContext();
        var stockService = new StockService(context);
        var demandMock = CreateDemandMultiplierMock(2.0);
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();
        
        var calculationService = CreateCalculationService();
        var forecastService = new ForecastService(stockService, demandMock.Object, confidenceStrategy, calculationService);

        var forecastBefore = await forecastService.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        Assert.NotNull(forecastBefore);
        Assert.Equal(80, forecastBefore.CurrentLevel);
        Assert.Equal(5.0, forecastBefore.HoursRemaining);

        await stockService.RecordConsumptionAsync("PUB-001", "GUINNESS", 16);

        var forecastAfter = await forecastService.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        Assert.NotNull(forecastAfter);
        Assert.Equal(64, forecastAfter.CurrentLevel);
        Assert.Equal(4.0, forecastAfter.HoursRemaining);
        Assert.True(forecastAfter.WillDepleteInWindow);
    }

    [Fact]
    public async Task Scenario10_MatchDay_CompleteWorkflow()
    {
        using var context = CreateInMemoryContext();
        var stockService = new StockService(context);
        var strategyMock = CreateSeverityStrategyMock();
        var confidenceStrategy = new DefaultForecastConfidenceStrategy();

        var beforeMatchDemand = CreateDemandMultiplierMock(1.0);
        var calculationService = CreateCalculationService();
        var beforeMatchForecast = new ForecastService(stockService, beforeMatchDemand.Object, confidenceStrategy, calculationService);
        var beforeMatchAlerts = new AlertService(context, beforeMatchDemand.Object, strategyMock.Object, calculationService);

        var guinnessBeforeMatch = await beforeMatchForecast.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var alertsBeforeMatch = await beforeMatchAlerts.GetStockAlertsAsync("PUB-001", hoursThreshold: 4);

        Assert.NotNull(guinnessBeforeMatch);
        Assert.False(guinnessBeforeMatch.WillDepleteInWindow);
        Assert.Empty(alertsBeforeMatch);

        var duringMatchDemand = CreateDemandMultiplierMock(2.5);
        var duringMatchForecast = new ForecastService(stockService, duringMatchDemand.Object, confidenceStrategy, calculationService);
        var duringMatchAlerts = new AlertService(context, duringMatchDemand.Object, strategyMock.Object, calculationService);

        var guinnessMatchStart = await duringMatchForecast.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var alertsMatchStart = await duringMatchAlerts.GetStockAlertsAsync("PUB-001", hoursThreshold: 4);

        Assert.NotNull(guinnessMatchStart);
        Assert.Equal(2.5, guinnessMatchStart.DemandMultiplier);
        Assert.Equal(4.0, guinnessMatchStart.HoursRemaining);
        Assert.True(guinnessMatchStart.WillDepleteInWindow);
        Assert.NotEmpty(alertsMatchStart);

        await stockService.RecordConsumptionAsync("PUB-001", "GUINNESS", 40);

        var guinnessHalfTime = await duringMatchForecast.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var alertsHalfTime = await duringMatchAlerts.GetStockAlertsAsync("PUB-001", hoursThreshold: 4);

        Assert.NotNull(guinnessHalfTime);
        Assert.Equal(40, guinnessHalfTime.CurrentLevel);
        Assert.Equal(2.0, guinnessHalfTime.HoursRemaining);
        Assert.Contains(alertsHalfTime, a => a.ProductId == "GUINNESS" && a.Severity == AlertSeverity.HIGH);

        var afterMatchDemand = CreateDemandMultiplierMock(1.0);
        var afterMatchForecast = new ForecastService(stockService, afterMatchDemand.Object, confidenceStrategy, calculationService);
        var afterMatchAlerts = new AlertService(context, afterMatchDemand.Object, strategyMock.Object, calculationService);

        var guinnessAfterMatch = await afterMatchForecast.GetForecastAsync("PUB-001", "GUINNESS", hours: 4);
        var alertsAfterMatch = await afterMatchAlerts.GetStockAlertsAsync("PUB-001", hoursThreshold: 4);

        Assert.NotNull(guinnessAfterMatch);
        Assert.Equal(1.0, guinnessAfterMatch.DemandMultiplier);
        Assert.Equal(5.0, guinnessAfterMatch.HoursRemaining);
        Assert.False(guinnessAfterMatch.WillDepleteInWindow);
        Assert.DoesNotContain(alertsAfterMatch, a => a.ProductId == "GUINNESS");
    }
}
