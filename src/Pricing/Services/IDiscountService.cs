using Pricing.Models;

namespace Pricing.Services;

public interface IDiscountService
{
    decimal CalculateDiscount(decimal basePrice, Offer offer);
    string FormatDiscount(Offer offer);
}
