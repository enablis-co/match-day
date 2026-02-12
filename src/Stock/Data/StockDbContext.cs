using Microsoft.EntityFrameworkCore;
using Stock.Models;

namespace Stock.Data;

public class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<StockLevel> StockLevels { get; set; }
    public DbSet<ConsumptionRecord> ConsumptionRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasKey(p => p.ProductId);

        modelBuilder.Entity<StockLevel>()
            .HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId);

        // Seed initial data
        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = "GUINNESS", ProductName = "Guinness Draught", Category = "beer", Unit = "pints", BaseConsumptionRate = 8 },
            new Product { ProductId = "STELLA", ProductName = "Stella Artois", Category = "beer", Unit = "pints", BaseConsumptionRate = 5 },
            new Product { ProductId = "CARLSBERG", ProductName = "Carlsberg", Category = "beer", Unit = "pints", BaseConsumptionRate = 7 },
            new Product { ProductId = "ABBOTS", ProductName = "Abbots Ale", Category = "beer", Unit = "pints", BaseConsumptionRate = 12 },
            new Product { ProductId = "JAMESON", ProductName = "Jameson Irish Whiskey", Category = "spirits", Unit = "shots", BaseConsumptionRate = 5 },
            new Product { ProductId = "GORDONS", ProductName = "Gordon's Gin", Category = "spirits", Unit = "shots", BaseConsumptionRate = 6 }
        );

        modelBuilder.Entity<StockLevel>().HasData(
            new StockLevel { Id = 1, PubId = "PUB-001", ProductId = "GUINNESS", CurrentLevel = 45, LastUpdated = DateTime.UtcNow.AddHours(-2.5) },
            new StockLevel { Id = 2, PubId = "PUB-001", ProductId = "STELLA", CurrentLevel = 120, LastUpdated = DateTime.UtcNow.AddHours(-5.2) },
            new StockLevel { Id = 3, PubId = "PUB-001", ProductId = "CARLSBERG", CurrentLevel = 80, LastUpdated = DateTime.UtcNow.AddHours(-3.8) },
            new StockLevel { Id = 4, PubId = "PUB-001", ProductId = "JAMESON", CurrentLevel = 25, LastUpdated = DateTime.UtcNow.AddHours(-1.3) },
            new StockLevel { Id = 5, PubId = "PUB-001", ProductId = "GORDONS", CurrentLevel = 30, LastUpdated = DateTime.UtcNow.AddHours(-4.7) },
            new StockLevel { Id = 6, PubId = "PUB-002", ProductId = "GUINNESS", CurrentLevel = 120, LastUpdated = DateTime.UtcNow.AddHours(-6.1) },
            new StockLevel { Id = 7, PubId = "PUB-002", ProductId = "STELLA", CurrentLevel = 90, LastUpdated = DateTime.UtcNow.AddHours(-0.9) },
            new StockLevel { Id = 8, PubId = "PUB-002", ProductId = "CARLSBERG", CurrentLevel = 50, LastUpdated = DateTime.UtcNow.AddHours(-3.2) },
            new StockLevel { Id = 9, PubId = "PUB-002", ProductId = "ABBOTS", CurrentLevel = 200, LastUpdated = DateTime.UtcNow.AddHours(-8.4) },
            new StockLevel { Id = 10, PubId = "PUB-002", ProductId = "JAMESON", CurrentLevel = 65, LastUpdated = DateTime.UtcNow.AddHours(-2.1) },
            new StockLevel { Id = 11, PubId = "PUB-002", ProductId = "GORDONS", CurrentLevel = 10, LastUpdated = DateTime.UtcNow.AddHours(-7.3) }
        );
    }
}