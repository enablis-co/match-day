namespace Pricing.Data.Tests;

public class OfferRepositoryTests
{
    private readonly OfferRepository _repository;

    public OfferRepositoryTests()
    {
        _repository = new OfferRepository();
    }

    [Fact]
    public void GetAll_ReturnsAllOffers()
    {
        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetAll_ReturnsReadOnlyCollection()
    {
        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Offer>>(result);
    }

    [Fact]
    public void GetById_WithValidId_ReturnsOffer()
    {
        // Act
        var result = _repository.GetById("OFFER-001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OFFER-001", result.OfferId);
        Assert.Equal("Happy Hour", result.Name);
    }

    [Fact]
    public void GetById_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = _repository.GetById("OFFER-999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        // Act
        var result = _repository.GetById("offer-001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OFFER-001", result.OfferId);
    }

    [Fact]
    public void GetAll_ContainsExpectedOffers()
    {
        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.Contains(result, o => o.OfferId == "OFFER-001" && o.Name == "Happy Hour");
        Assert.Contains(result, o => o.OfferId == "OFFER-002" && o.Name == "2-for-1 Cocktails");
        Assert.Contains(result, o => o.OfferId == "OFFER-003" && o.Name == "Weekend Special");
    }

    [Fact]
    public void GetById_VerifyOfferDetails()
    {
        // Act
        var result = _repository.GetById("OFFER-001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Happy Hour", result.Name);
        Assert.Equal("50% off selected pints", result.Description);
        Assert.Equal(DiscountType.PERCENTAGE, result.DiscountType);
        Assert.Equal(50, result.DiscountValue);
        Assert.Equal(MatchDayRule.END_EARLY, result.MatchDayRule);
    }
}
