using Stock.Models;

namespace Stock.Services;

public interface IStockService
{
    Task<IEnumerable<StockLevel>> GetCurrentStockAsync(string pubId, string? category = null);
    Task<StockLevel?> GetProductStockAsync(string pubId, string productId);
    Task<bool> RecordConsumptionAsync(string pubId, string productId, double amount);
}