namespace Stock.Services;

public interface IStockCommandService
{
    Task<bool> RecordConsumptionAsync(string pubId, string productId, double amount);
}
