namespace Pricing.Data;

public class ProductRepository : IProductRepository
{
    private readonly Dictionary<string, decimal> _baseProducts;

    public ProductRepository()
    {
        _baseProducts = new Dictionary<string, decimal>
        {
            ["PINT_LAGER"] = 5.00m,
            ["PINT_ALE"] = 4.80m,
            ["PINT_STELLA"] = 5.20m,
            ["PINT_GUINNESS"] = 5.50m,
            ["COCKTAIL_MOJITO"] = 8.50m,
            ["COCKTAIL_MARGARITA"] = 8.00m,
            ["COCKTAIL_ESPRESSO_MARTINI"] = 9.00m,
            ["SOFT_DRINK"] = 2.50m,
            ["CRISPS"] = 1.50m
        };
    }

    public IReadOnlyDictionary<string, decimal> GetAll() => _baseProducts.AsReadOnly();

    public decimal? GetPrice(string productId) => 
        _baseProducts.TryGetValue(productId, out var price) ? price : null;

    public IEnumerable<KeyValuePair<string, decimal>> GetProducts(string? productId = null)
    {
        var products = _baseProducts.AsEnumerable();
        if (!string.IsNullOrEmpty(productId))
        {
            products = products.Where(p => p.Key.Equals(productId, StringComparison.OrdinalIgnoreCase));
        }
        return products;
    }
}
