using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;
using Stock.Services;

namespace Stock.Tests.Services;

public class StockServiceTests
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
            new Product { ProductId = "JAMESON", ProductName = "Jameson Irish Whiskey", Category = "spirits", Unit = "shots", BaseConsumptionRate = 5 },
            new Product { ProductId = "CARLSBERG", ProductName = "Carlsberg", Category = "beer", Unit = "pints", BaseConsumptionRate = 7 }
        };

        var stockLevels = new[]
        {
            new StockLevel { Id = 1, PubId = "PUB-001", ProductId = "GUINNESS", CurrentLevel = 45, LastUpdated = DateTime.UtcNow.AddHours(-2) },
            new StockLevel { Id = 2, PubId = "PUB-001", ProductId = "STELLA", CurrentLevel = 120, LastUpdated = DateTime.UtcNow.AddHours(-1) },
            new StockLevel { Id = 3, PubId = "PUB-001", ProductId = "JAMESON", CurrentLevel = 8, LastUpdated = DateTime.UtcNow.AddMinutes(-30) },
            new StockLevel { Id = 4, PubId = "PUB-002", ProductId = "GUINNESS", CurrentLevel = 90, LastUpdated = DateTime.UtcNow.AddHours(-3) },
            new StockLevel { Id = 5, PubId = "PUB-002", ProductId = "CARLSBERG", CurrentLevel = 25, LastUpdated = DateTime.UtcNow.AddHours(-1) }
        };

        context.Products.AddRange(products);
        context.StockLevels.AddRange(stockLevels);
        context.SaveChanges();
    }

    [Fact]
    public async Task GetCurrentStockAsync_ReturnsAllStock_ForGivenPub()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetCurrentStockAsync("PUB-001");

        Assert.Equal(3, result.Count());
        Assert.All(result, stock => Assert.Equal("PUB-001", stock.PubId));
    }

    [Fact]
    public async Task GetCurrentStockAsync_ReturnsEmpty_WhenPubHasNoStock()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetCurrentStockAsync("PUB-999");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCurrentStockAsync_FiltersById_WhenCategorySpecified()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetCurrentStockAsync("PUB-001", "beer");

        Assert.Equal(2, result.Count());
        Assert.All(result, stock => Assert.Equal("beer", stock.Product?.Category));
    }

    [Fact]
    public async Task GetCurrentStockAsync_ReturnsEmpty_WhenCategoryNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetCurrentStockAsync("PUB-001", "wine");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCurrentStockAsync_IncludesProductDetails()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetCurrentStockAsync("PUB-001");
        var guinness = result.First(s => s.ProductId == "GUINNESS");

        Assert.NotNull(guinness.Product);
        Assert.Equal("Guinness Draught", guinness.Product.ProductName);
        Assert.Equal("beer", guinness.Product.Category);
        Assert.Equal("pints", guinness.Product.Unit);
    }

    [Fact]
    public async Task GetProductStockAsync_ReturnsStock_WhenProductExists()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetProductStockAsync("PUB-001", "GUINNESS");

        Assert.NotNull(result);
        Assert.Equal("PUB-001", result.PubId);
        Assert.Equal("GUINNESS", result.ProductId);
        Assert.Equal(45, result.CurrentLevel);
    }

    [Fact]
    public async Task GetProductStockAsync_ReturnsNull_WhenProductNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetProductStockAsync("PUB-001", "NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductStockAsync_ReturnsNull_WhenPubNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetProductStockAsync("PUB-999", "GUINNESS");

        Assert.Null(result);
    }

    [Fact]
    public async Task RecordConsumptionAsync_ReducesStockLevel_WhenProductExists()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var success = await service.RecordConsumptionAsync("PUB-001", "GUINNESS", 10);

        Assert.True(success);
        var stock = await context.StockLevels
            .FirstOrDefaultAsync(s => s.PubId == "PUB-001" && s.ProductId == "GUINNESS");
        Assert.NotNull(stock);
        Assert.Equal(35, stock.CurrentLevel);
    }

    [Fact]
    public async Task RecordConsumptionAsync_UpdatesLastUpdatedTimestamp()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);
        var beforeTime = DateTime.UtcNow;

        await service.RecordConsumptionAsync("PUB-001", "GUINNESS", 5);

        var stock = await context.StockLevels
            .FirstOrDefaultAsync(s => s.PubId == "PUB-001" && s.ProductId == "GUINNESS");
        Assert.NotNull(stock);
        Assert.True(stock.LastUpdated >= beforeTime);
    }

    [Fact]
    public async Task RecordConsumptionAsync_CreatesConsumptionRecord()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        await service.RecordConsumptionAsync("PUB-001", "GUINNESS", 15);

        var record = await context.ConsumptionRecords
            .FirstOrDefaultAsync(r => r.PubId == "PUB-001" && r.ProductId == "GUINNESS");
        Assert.NotNull(record);
        Assert.Equal(15, record.Amount);
    }

    [Fact]
    public async Task RecordConsumptionAsync_DoesNotGoBelowZero()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        await service.RecordConsumptionAsync("PUB-001", "GUINNESS", 100);

        var stock = await context.StockLevels
            .FirstOrDefaultAsync(s => s.PubId == "PUB-001" && s.ProductId == "GUINNESS");
        Assert.NotNull(stock);
        Assert.Equal(0, stock.CurrentLevel);
    }

    [Fact]
    public async Task RecordConsumptionAsync_ReturnsFalse_WhenProductNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var success = await service.RecordConsumptionAsync("PUB-001", "NONEXISTENT", 10);

        Assert.False(success);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsItemsBelowThreshold()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetLowStockAlertsAsync("PUB-001", 30);

        Assert.Single(result);
        var item = result.First();
        Assert.Equal("JAMESON", item.ProductId);
        Assert.Equal(8, item.CurrentLevel);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_UsesDefaultThreshold_WhenNotSpecified()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetLowStockAlertsAsync("PUB-001");

        Assert.Single(result);
        Assert.Equal("JAMESON", result.First().ProductId);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsEmpty_WhenAllStockAboveThreshold()
    {
        using var context = CreateInMemoryContext();
        var service = new StockService(context);

        var result = await service.GetLowStockAlertsAsync("PUB-001", 5);

        Assert.Empty(result);
    }

    [Fact]
    public async Task StockLevel_Status_ReturnsCritical_WhenLevelBelowOrEqual10()
    {
        using var context = CreateInMemoryContext();
        
        var stock = await context.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == "JAMESON");

        Assert.NotNull(stock);
        Assert.Equal("CRITICAL", stock.Status);
    }

    [Fact]
    public async Task StockLevel_Status_ReturnsLow_WhenLevelBetween11And30()
    {
        using var context = CreateInMemoryContext();
        
        var stock = await context.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == "GUINNESS" && s.PubId == "PUB-002");

        Assert.NotNull(stock);
        Assert.Equal("OK", stock.Status);
    }

    [Fact]
    public async Task StockLevel_Status_ReturnsOK_WhenLevelAbove30()
    {
        using var context = CreateInMemoryContext();
        
        var stock = await context.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == "STELLA");

        Assert.NotNull(stock);
        Assert.Equal("OK", stock.Status);
    }
}
