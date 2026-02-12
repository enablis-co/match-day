namespace Pricing.Services;

public interface IPricingService
{
    List<object> GetCurrentPricing(string? productId, DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd);
}
