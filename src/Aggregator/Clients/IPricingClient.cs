using Aggregator.Models;

namespace Aggregator.Clients;

public interface IPricingClient
{
    Task<PricingSummary> GetActiveOffersAsync(string pubId, DateTime? time);
}
