using Pricing.Models;

namespace Pricing.Services;

public class DiscountService
{
    public decimal CalculateDiscount(decimal basePrice, Offer offer)
    {
        return offer.DiscountType switch
        {
            DiscountType.PERCENTAGE => basePrice * (offer.DiscountValue / 100m),
            DiscountType.FIXED_AMOUNT => offer.DiscountValue,
            DiscountType.BUY_ONE_GET_ONE => basePrice,
            _ => 0m
        };
    }

    public string FormatDiscount(Offer offer)
    {
        return offer.DiscountType switch
        {
            DiscountType.PERCENTAGE => $"{offer.DiscountValue}% off",
            DiscountType.FIXED_AMOUNT => $"£{offer.DiscountValue} off",
            DiscountType.BUY_ONE_GET_ONE => "Buy one get one free",
            _ => ""
        };
    }
}
