namespace Pricing.Data;

public interface IProductRepository
{
    IReadOnlyDictionary<string, decimal> GetAll();
    decimal? GetPrice(string productId);
    IEnumerable<KeyValuePair<string, decimal>> GetProducts(string? productId = null);
}
