namespace Pricing.Data.Tests;

public class ProductRepositoryTests
{
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        _repository = new ProductRepository();
    }

    [Fact]
    public void GetAll_ReturnsAllProducts()
    {
        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void GetAll_ReturnsReadOnlyDictionary()
    {
        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, decimal>>(result);
    }

    [Fact]
    public void GetPrice_WithValidProductId_ReturnsPrice()
    {
        // Act
        var result = _repository.GetPrice("PINT_LAGER");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.Value);
    }

    [Fact]
    public void GetPrice_WithInvalidProductId_ReturnsNull()
    {
        // Act
        var result = _repository.GetPrice("INVALID");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetProducts_WithoutFilter_ReturnsAll()
    {
        // Act
        var result = _repository.GetProducts();

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetProducts_WithProductIdFilter_ReturnsOnlyMatching()
    {
        // Act
        var result = _repository.GetProducts("PINT_LAGER");

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, p => Assert.Equal("PINT_LAGER", p.Key));
    }

    [Fact]
    public void GetProducts_WithInvalidFilter_ReturnsEmpty()
    {
        // Act
        var result = _repository.GetProducts("INVALID");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("PINT_LAGER", 5.00)]
    [InlineData("PINT_ALE", 4.80)]
    [InlineData("SOFT_DRINK", 2.50)]
    public void GetPrice_VerifyProductPrices(string productId, double expectedPrice)
    {
        // Act
        var result = _repository.GetPrice(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((decimal)expectedPrice, result.Value);
    }
}
