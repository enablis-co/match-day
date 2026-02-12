using Pricing.Models;

namespace Pricing.Data;

public interface IOfferRepository
{
    IReadOnlyList<Offer> GetAll();
    Offer? GetById(string offerId);
}
