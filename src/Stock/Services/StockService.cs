using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Models;

namespace Stock.Services;

public class StockService : IStockQueryService, IStockCommandService
{
    private readonly StockDbContext _context;

    public StockService(StockDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StockLevel>> GetCurrentStockAsync(string pubId, string? category = null)
    {
        var query = _context.StockLevels
            .Include(s => s.Product)
            .Where(s => s.PubId == pubId);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Product != null && s.Product.Category == category);
        }

        return await query.ToListAsync();
    }

    public async Task<StockLevel?> GetProductStockAsync(string pubId, string productId)
    {
        return await _context.StockLevels
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.PubId == pubId && s.ProductId == productId);
    }

    public async Task<bool> RecordConsumptionAsync(string pubId, string productId, double amount)
    {
        var stockLevel = await GetProductStockAsync(pubId, productId);
        if (stockLevel == null) return false;

        // Update stock level
        stockLevel.CurrentLevel = Math.Max(0, stockLevel.CurrentLevel - amount);
        stockLevel.LastUpdated = DateTime.UtcNow;

        // Record consumption
        _context.ConsumptionRecords.Add(new ConsumptionRecord
        {
            PubId = pubId,
            ProductId = productId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }
}