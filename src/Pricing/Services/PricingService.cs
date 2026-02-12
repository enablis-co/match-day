using Pricing.Data;
using Pricing.Models;

namespace Pricing.Services;

public class PricingService
{
    private readonly ProductRepository _productRepository;
    private readonly OfferEvaluationService _offerEvaluationService;
    private readonly DiscountService _discountService;

    public PricingService(
        ProductRepository productRepository,
        OfferEvaluationService offerEvaluationService,
        DiscountService discountService)
    {
        _productRepository = productRepository;
        _offerEvaluationService = offerEvaluationService;
        _discountService = discountService;
    }

    public List<object> GetCurrentPricing(string? productId, DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd)
    {
        var activeEvaluations = _offerEvaluationService
            .EvaluateAllOffers(now, matchWindowActive, demandMultiplier, matchWindowEnd)
            .Where(e => e.Status == OfferStatus.ACTIVE)
            .ToList();

        var products = _productRepository.GetProducts(productId);

        var prices = products.Select(p =>
        {
            var applicableOffer = activeEvaluations
                .Where(e => e.Offer.ApplicableProducts.Contains(p.Key))
                .OrderByDescending(e => _discountService.CalculateDiscount(p.Value, e.Offer))
                .FirstOrDefault();

            decimal currentPrice = p.Value;
            object? discount = null;

            if (applicableOffer is not null)
            {
                var discountAmount = _discountService.CalculateDiscount(p.Value, applicableOffer.Offer);
                currentPrice = Math.Max(0, p.Value - discountAmount);
                discount = new
                {
                    applicableOffer.Offer.OfferId,
                    Description = $"{applicableOffer.Offer.Name} {_discountService.FormatDiscount(applicableOffer.Offer)}"
                };
            }

            return new
            {
                ProductId = p.Key,
                BasePrice = p.Value,
                CurrentPrice = currentPrice,
                Discount = discount
            };
        }).Cast<object>().ToList();

        return prices;
    }
}
