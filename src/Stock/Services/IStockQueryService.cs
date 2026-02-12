using Stock.Models;

namespace Stock.Services;

public interface IStockQueryService
{
    Task<IEnumerable<StockLevel>> GetCurrentStockAsync(string pubId, string? category = null);
    Task<StockLevel?> GetProductStockAsync(string pubId, string productId);
}
