namespace Pricing.Services.Tests;

public class DiscountServiceTests
{
    private readonly DiscountService _service;

    public DiscountServiceTests()
    {
        _service = new DiscountService();
    }

    #region CalculateDiscount Tests

    [Fact]
    public void CalculateDiscount_WithPercentageDiscount_ReturnsCorrectAmount()
    {
        // Arrange
        var basePrice = 100m;
        var offer = new Offer
        {
            OfferId = "TEST-001",
            DiscountType = DiscountType.PERCENTAGE,
            DiscountValue = 50
        };

        // Act
        var result = _service.CalculateDiscount(basePrice, offer);

        // Assert
        Assert.Equal(50m, result);
    }

    [Fact]
    public void CalculateDiscount_WithFixedAmountDiscount_ReturnsExactAmount()
    {
        // Arrange
        var basePrice = 100m;
        var offer = new Offer
        {
            OfferId = "TEST-002",
            DiscountType = DiscountType.FIXED_AMOUNT,
            DiscountValue = 10m
        };

        // Act
        var result = _service.CalculateDiscount(basePrice, offer);

        // Assert
        Assert.Equal(10m, result);
    }

    [Fact]
    public void CalculateDiscount_WithBuyOneGetOne_DiscountsFullBasePrice()
    {
        // Arrange
        var basePrice = 50m;
        var offer = new Offer
        {
            OfferId = "TEST-003",
            DiscountType = DiscountType.BUY_ONE_GET_ONE,
            DiscountValue = 100
        };

        // Act
        var result = _service.CalculateDiscount(basePrice, offer);

        // Assert
        Assert.Equal(50m, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(0.01)]
    public void CalculateDiscount_WithVariousBasePrices_CalculatesCorrectly(double basePriceDouble)
    {
        // Arrange
        var basePrice = (decimal)basePriceDouble;
        var offer = new Offer
        {
            OfferId = "TEST-004",
            DiscountType = DiscountType.PERCENTAGE,
            DiscountValue = 25
        };

        // Act
        var result = _service.CalculateDiscount(basePrice, offer);

        // Assert
        var expected = basePrice * 0.25m;
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatDiscount Tests

    [Fact]
    public void FormatDiscount_WithPercentage_FormatsAsPercentage()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "TEST-005",
            DiscountType = DiscountType.PERCENTAGE,
            DiscountValue = 50
        };

        // Act
        var result = _service.FormatDiscount(offer);

        // Assert
        Assert.Equal("50% off", result);
    }

    [Fact]
    public void FormatDiscount_WithFixedAmount_FormatsAsPounds()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "TEST-006",
            DiscountType = DiscountType.FIXED_AMOUNT,
            DiscountValue = 5.50m
        };

        // Act
        var result = _service.FormatDiscount(offer);

        // Assert
        Assert.Equal("£5.50 off", result);
    }

    [Fact]
    public void FormatDiscount_WithBuyOneGetOne_FormatsSpecialMessage()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "TEST-007",
            DiscountType = DiscountType.BUY_ONE_GET_ONE
        };

        // Act
        var result = _service.FormatDiscount(offer);

        // Assert
        Assert.Equal("Buy one get one free", result);
    }

    #endregion
}
