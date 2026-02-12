using Aggregator.Models;

namespace Aggregator.Clients;

public interface IStockClient
{
    Task<StockSummary> GetStockAlertsAsync(string pubId);
}
