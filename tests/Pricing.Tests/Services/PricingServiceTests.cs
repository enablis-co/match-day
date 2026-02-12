namespace Pricing.Services.Tests;

public class PricingServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IOfferEvaluationService> _mockOfferEvaluationService;
    private readonly Mock<IDiscountService> _mockDiscountService;
    private readonly PricingService _service;

    public PricingServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockOfferEvaluationService = new Mock<IOfferEvaluationService>();
        _mockDiscountService = new Mock<IDiscountService>();
        
        _service = new PricingService(
            _mockProductRepository.Object,
            _mockOfferEvaluationService.Object,
            _mockDiscountService.Object);
    }

    [Fact]
    public void GetCurrentPricing_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetProducts(null))
            .Returns(new Dictionary<string, decimal>().AsEnumerable());
        
        _mockOfferEvaluationService.Setup(e => e.EvaluateAllOffers(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<double>(), It.IsAny<DateTime?>()))
            .Returns(new List<OfferEvaluation>());

        // Act
        var result = _service.GetCurrentPricing(null, DateTime.UtcNow, false, 1.0, null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCurrentPricing_NoApplicableOffers_ReturnsBasePrices()
    {
        // Arrange
        var products = new Dictionary<string, decimal>
        {
            { "PINT_LAGER", 5.00m }
        };
        
        _mockProductRepository.Setup(r => r.GetProducts(null))
            .Returns(products.AsEnumerable());
        
        _mockOfferEvaluationService.Setup(e => e.EvaluateAllOffers(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<double>(), It.IsAny<DateTime?>()))
            .Returns(new List<OfferEvaluation>());

        // Act
        var result = _service.GetCurrentPricing(null, DateTime.UtcNow, false, 1.0, null) as List<object>;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void GetCurrentPricing_WithApplicableOffer_AppliesDiscount()
    {
        // Arrange
        var products = new Dictionary<string, decimal>
        {
            { "PINT_LAGER", 5.00m }
        };
        
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Name = "Happy Hour",
            ApplicableProducts = ["PINT_LAGER"],
            DiscountType = DiscountType.PERCENTAGE,
            DiscountValue = 50
        };
        
        var evaluation = new OfferEvaluation(offer, OfferStatus.ACTIVE, null, null);
        
        _mockProductRepository.Setup(r => r.GetProducts(null))
            .Returns(products.AsEnumerable());
        
        _mockOfferEvaluationService.Setup(e => e.EvaluateAllOffers(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<double>(), It.IsAny<DateTime?>()))
            .Returns(new List<OfferEvaluation> { evaluation });
        
        _mockDiscountService.Setup(d => d.CalculateDiscount(5.00m, offer))
            .Returns(2.50m);
        
        _mockDiscountService.Setup(d => d.FormatDiscount(offer))
            .Returns("50% off");

        // Act
        var result = _service.GetCurrentPricing(null, DateTime.UtcNow, false, 1.0, null);

        // Assert
        Assert.NotNull(result);
        _mockDiscountService.Verify(d => d.CalculateDiscount(It.IsAny<decimal>(), It.IsAny<Offer>()), Times.Exactly(2));
    }

    [Fact]
    public void GetCurrentPricing_FiltersByProductId_ReturnsOnlyRequestedProduct()
    {
        // Arrange
        var products = new Dictionary<string, decimal>
        {
            { "PINT_LAGER", 5.00m },
            { "PINT_ALE", 4.80m }
        };
        
        _mockProductRepository.Setup(r => r.GetProducts("PINT_LAGER"))
            .Returns(products.Where(p => p.Key == "PINT_LAGER").AsEnumerable());
        
        _mockOfferEvaluationService.Setup(e => e.EvaluateAllOffers(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<double>(), It.IsAny<DateTime?>()))
            .Returns(new List<OfferEvaluation>());

        // Act
        var result = _service.GetCurrentPricing("PINT_LAGER", DateTime.UtcNow, false, 1.0, null);

        // Assert
        Assert.NotNull(result);
        _mockProductRepository.Verify(r => r.GetProducts("PINT_LAGER"), Times.Once);
    }
}
